using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CliWrap;
using CliWrap.Buffered;
using SharedLibrary;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using File = System.IO.File;

namespace TelegramBotRemoverConsole;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        
        Option<string> apiOption = new("--api", "API key for telegram bot") {IsRequired = true};
        apiOption.AddAlias("-a");

        Option<FileInfo> removerOption = new("--remover", "Path to violet remover utility") {IsRequired = true};
        removerOption.AddAlias("-r");

        Option<FileInfo> rootOption = new("--root", "Violets root dir") {IsRequired = true};

        RootCommand rootCommand = new("Telegram interface for KerrysFlowersWebApp");
        rootCommand.AddOption(apiOption);
        rootCommand.AddOption(removerOption);
        rootCommand.AddOption(rootOption);
            
        // ReSharper disable once ObjectCreationAsStatement
        rootCommand.SetHandler((api, remover, root) =>
        {
            if (File.Exists(remover.FullName) == false)
            {
                Console.WriteLine("Remover tool not found");
                return;
            }

            if (Directory.Exists(root.FullName) == false)
            {
                Console.WriteLine("Root dir not exist");
                return;
            }

            new Remover(api, remover, root);
        }, apiOption, removerOption, rootOption);

        return await rootCommand.InvokeAsync(args);
    }
}

internal class Remover
{
    private readonly FileInfo _remover;
    private readonly FileInfo _root;
    private State _currentState = State.Idle;
    private List<Violet> _violets = new();
    
    public Remover(string apiKey, FileInfo remover, FileInfo root)
    {
        _remover = remover;
        _root = root;

        TelegramBotClient client = new(apiKey);
        client.StartReceiving(UpdateHandler, PollingErrorHandler);
            
        Console.ReadLine();
    }
    private static Task PollingErrorHandler(ITelegramBotClient arg1, Exception arg2, CancellationToken arg3)
    {
        Console.WriteLine(arg2);
        return Task.CompletedTask;
    }

    private async Task UpdateHandler(ITelegramBotClient client, Update update, CancellationToken token)
    {
        Message message = update.Message;
        if (message == null)
        {
            return;
        }

        long chatId = message.Chat.Id;

        if (message.Text != null && _currentState != State.Idle && message.Text.ToLower() == "/stop")
        {
            await RunStopProcedure(client, chatId);
            return;
        }

        if (message.Text != null && _currentState != State.Working && message.Text.ToLower() == "/start")
        {
            await RunStartProcedure(client, chatId);
            return;
        }

        if (message.Text != null && _currentState == State.Working && _violets.Count > 0)
        {
            await SelectAndRemoveViolet(client, message, chatId);
        }
    }

    private async Task SelectAndRemoveViolet(ITelegramBotClient client, Message message, long chatId)
    {
        bool parse = Guid.TryParse(message.Text, out Guid result);
        if (parse == false)
        {
            await client.SendTextMessageAsync(chatId, "Ожидался объект типа \"идентификатор\". Повторите ввод.");
            return;
        }

        Violet selectedViolet = _violets.FirstOrDefault(violet => violet.Id.Equals(result));
        if (selectedViolet == null)
        {
            await client.SendTextMessageAsync(chatId, "Фиалка с таким идентификатором не найдена. Повторите ввод.");
            return;
        }

        BufferedCommandResult bufferedCommandResult = await Cli.Wrap(_remover.FullName)
            .WithArguments($"-g \"{selectedViolet.Id}\" -r \"{_root}\"")
            .ExecuteBufferedAsync();

        if (bufferedCommandResult.StandardOutput.Contains("Success"))
        {
            await client.SendTextMessageAsync(chatId, "Фиалка успешно удалена.");
            Console.WriteLine(bufferedCommandResult.StandardOutput);
        }
        else
        {
            Console.WriteLine(bufferedCommandResult.StandardError);
            Console.WriteLine(bufferedCommandResult.StandardOutput);
        }

        await RunStopProcedure(client, chatId);
    }

    private async Task RunStopProcedure(ITelegramBotClient client, long chatId)
    {
        await client.SendTextMessageAsync(chatId, "Exit");
        _violets = Array.Empty<Violet>().ToList();
        _currentState = State.Idle;
    }

    private async Task RunStartProcedure(ITelegramBotClient client, long chatId)
    {
        _currentState = State.Working;
        string[] jsonFiles = Directory.GetFiles(_root.FullName, "*.json", SearchOption.AllDirectories);
        if (jsonFiles.Length == 0)
        {
            await client.SendTextMessageAsync(chatId, "Не найдено ни одной фиалки");
            _currentState = State.Idle;
            return;
        }

        List<string> filesContents = jsonFiles.Select(File.ReadAllText).ToList();
        try
        {
            _violets = filesContents
                .Select(content => JsonSerializer.Deserialize<Violet>(content))
                .ToList();
        }
        catch
        {
            await client.SendTextMessageAsync(chatId, "Во время загрузки списка фиалок с сайта возникла ошибка");
            _currentState = State.Idle;
            return;
        }

        InlineKeyboardMarkup inlineKeyboard = new(new []
            {
                InlineKeyboardButton.WithUrl(
                    text: "Каталог фиалок",
                    url: $@"https://www.kerrisflowers.ru"
                )
            }
        );
        await client.SendTextMessageAsync(chatId, 
            "Введите id фиалки, которую нужно удалить.",
            replyMarkup: inlineKeyboard);
    }
}

internal enum State
{
    Working, Idle
}