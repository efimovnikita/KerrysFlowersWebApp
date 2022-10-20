using CliWrap;
using CliWrap.Buffered;
using SharedLibrary;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBotConsole.Units;

namespace TelegramBotConsole;

internal class Runner
{
    private readonly FileInfo _maker;
    private readonly FileInfo _root;
    private Violet _currentViolet;
    private Queue<IUnit> _stages = new();
    private readonly TelegramBotClient _client;
    private IUnit _currentStage;
    private long? _chatId;

    public Runner(string apiKey, FileInfo maker, FileInfo root)
    {
        _maker = maker;
        _root = root;

        _client = new TelegramBotClient(apiKey);
        _client.StartReceiving(UpdateHandler, PollingErrorHandler);

        Console.ReadLine();
    }

    private Task PollingErrorHandler(ITelegramBotClient arg1, Exception arg2, CancellationToken arg3)
    {
        Console.WriteLine(arg2);
        return Task.CompletedTask;
    }

    private async Task UpdateHandler(ITelegramBotClient client, Update update, CancellationToken token)
    {
        Message message = update.Message;

        if (update.Type == UpdateType.Message && message!.Text != null && message.Text.ToLower() == "/start")
        {
            _chatId = update.Message!.Chat.Id;
            _currentViolet = new Violet(Guid.NewGuid(), "", "", "", new List<string>(),
                DateTime.Now, new List<Image>(), false, new List<VioletColor>());
            _stages = new Queue<IUnit>(new IUnit[]
            {
                new NameUnit(client, _root),
                new DescriptionUnit(client),
                new BreederUnit(client),
                new TagsUnit(client),
                new ColorsUnit(client),
                new ChimeraUnit(client),
                new DateUnit(client),
                new FirstImageUnit(client),
                new SecondImageUnit(client),
                new ThirdImageUnit(client)
            });
            _currentStage = _stages.Dequeue();
            
            await _client.SendTextMessageAsync(_chatId, "Процесс создания фиалки запущен", cancellationToken: token);

            await _currentStage.Question(_chatId);
            return;
        }
        
        if (update.Type == UpdateType.Message && message!.Text != null && message.Text.ToLower() == "/stop")
        {
            _chatId = update.Message!.Chat.Id;
            await _client.SendTextMessageAsync(_chatId!, "Процесс создания фиалки остановлен", cancellationToken: token);
            Reset();
            
            return;
        }

        if (_currentStage == null)
        {
            return;
        }

        (bool, string) validationResult = _currentStage!.Validate(update);
        if (validationResult.Item1 == false)
        {
            await _client.SendTextMessageAsync(_chatId!, validationResult.Item2, cancellationToken: token);
            return;
        }

        (bool, string) actionResult = await _currentStage.RunAction(_currentViolet, update);
        _currentStage.PrintViolet(_currentViolet);
        if (actionResult.Item1 == false)
        {
            await _client.SendTextMessageAsync(_chatId!, actionResult.Item2, cancellationToken: token);
            return;
        }

        _currentStage = _stages.Count > 0 ? _stages.Dequeue() : null;
        if (_currentStage != null)
        {
            await _currentStage.Question(_chatId);
        }
        
        await PublishNewViolet();
    }

    private void Reset()
    {
        _chatId = null;
        _currentViolet = null;
        _currentStage = null;
        _stages = new Queue<IUnit>();
    }

    private async Task PublishNewViolet()
    {
        if (new[]
            {
                _currentViolet.Name,
                _currentViolet.Breeder,
                _currentViolet.Description
            }.All(s => String.IsNullOrEmpty(s) == false) &&
            _currentViolet.Tags.Count > 0 &&
            _currentViolet.Images.Count == 3 &&
            _currentViolet.Colors.Count > 0)
        {
            Console.WriteLine("Publish violet");

            BufferedCommandResult result = await Cli.Wrap(_maker.FullName)
                .WithArguments(
                    $"-n \"{_currentViolet.Name}\" -b \"{_currentViolet.Breeder}\" -d \"{_currentViolet.Description}\" --date \"{_currentViolet.BreedingDate}\" --image1 \"{_currentViolet.Images[0].W300}\" --image2 \"{_currentViolet.Images[1].W300}\" --image3 \"{_currentViolet.Images[2].W300}\" --chimera \"{_currentViolet.IsChimera}\" -t {String.Join(' ', _currentViolet.Tags.Select(tag => $"\"{tag}\""))} --colors {String.Join(' ', _currentViolet.Colors.Select(color => $"\"{color}\""))} --root \"{_root}\"")
                .ExecuteBufferedAsync();

            if (result.StandardOutput.Contains("Success"))
            {
                InlineKeyboardMarkup inlineKeyboard = new(new []
                    {
                        InlineKeyboardButton.WithUrl(
                            text: "Страница фиалки",
                            url: $@"https://www.kerrisflowers.ru/details/{_currentViolet.TransliteratedName}"
                        )
                    }
                );
                await _client.SendTextMessageAsync(_chatId!,
                    "Фиалка успешно опубликована.\nФиалка будет доступна на сайте через минуту.",
                    replyMarkup: inlineKeyboard);

                Console.WriteLine(result.StandardOutput);
            }
            else
            {
                Console.WriteLine(result.StandardError);
                Console.WriteLine(result.StandardOutput);
            }
                    
            Reset();
        }
    }
}