using System.CommandLine;
using System.Globalization;
using System.Text;
using System.Text.Json;
using CliWrap;
using CliWrap.Buffered;
using Microsoft.VisualBasic;
using SharedLibrary;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TelegramBotConsole;

internal class Program
{
    private static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        
        Option<string> apiOption = new("--api", "API key for telegram bot") {IsRequired = true};
        apiOption.AddAlias("-a");

        Option<FileInfo> makerOption = new("--maker", "Path to violet maker utility") {IsRequired = true};
        makerOption.AddAlias("-m");

        RootCommand rootCommand = new("Telegram interface for KerrysFlowersWebApp");
        rootCommand.AddOption(apiOption);
        rootCommand.AddOption(makerOption);
            
        rootCommand.SetHandler(new Runner().RunBot, apiOption, makerOption);

        return await rootCommand.InvokeAsync(args);
    }
}

internal enum Mode
{
    Unknown, Adding, Idle
}

internal enum Stage
{
    Name, Description, Breeder, Chimera, Tags, Colors, Date, Images
}

internal class Runner
{
    private FileInfo _maker;
    public Violet CurrentViolet { get; set; }

    public Queue<Stage> Stages { get; set; } = ReloadStages();

    private static Queue<Stage> ReloadStages() => new(Enum.GetValues(typeof(Stage)).Cast<Stage>().ToArray());

    public Mode CurrentMode { get; set; } = Mode.Idle;

    public void RunBot(string api, FileInfo maker)
    {
        _maker = maker;
        TelegramBotClient client = new(api);
        client.StartReceiving(HandleUpdateAsync, HandleErrorAsync);

        Console.ReadLine();
    }
    private async Task HandleErrorAsync(ITelegramBotClient arg1, Exception arg2, CancellationToken arg3)
    {
        Console.WriteLine(arg2);
    }

    private async Task HandleUpdateAsync(ITelegramBotClient client, Update update, CancellationToken token)
    {
        Console.WriteLine($"Current mode: \"{CurrentMode}\"");
        Console.WriteLine($"Current stage: \"{Stages.Peek()}\"");
        
        Message message = update.Message;
        if (message == null)
        {
            return;
        }

        long chatId = message.Chat.Id;
        
        switch (CurrentMode)
        {
            case Mode.Unknown:
            {
                break;
            }
            case Mode.Adding:
            {
                if (message.Type == MessageType.Text)
                {
                    string text = message.Text;
                    Console.WriteLine($"Message from user: \"{text}\"");

                    if (text == "/stop")
                    {
                        Reset();
                        await SendTextMsg(client, token, chatId, "Процесс создания фиалки остановлен");
                        break;
                    }
                    
                    if (text == "/start")
                    {
                        await SendTextMsg(client, token, chatId, "Процесс создания фиалки уже запущен");

                        break;
                    }
                    
                    switch (Stages.Peek())
                    {
                        case Stage.Name:
                        {
                            CurrentViolet.Name = text;
                            Stages.Dequeue();
                            PrintCurrentViolet();
                            
                            await SendTextMsg(client, token, chatId, "Введите описание фиалки");

                            break;
                        }
                        case Stage.Description:
                        {
                            CurrentViolet.Description = text;
                            Stages.Dequeue();
                            PrintCurrentViolet();
                            
                            await SendTextMsg(client, token, chatId, "Введите имя селекционера");

                            break;
                        }
                        case Stage.Breeder:
                        {
                            CurrentViolet.Breeder = text;
                            Stages.Dequeue();
                            PrintCurrentViolet();
                            
                            await SendTextMsg(client, token, chatId, "Является ли фиалка химерой (\'true\' или \'false\')?");

                            break;
                        }
                        case Stage.Chimera:
                        {
                            CurrentViolet.IsChimera = Boolean.Parse(text.ToLower());
                            Stages.Dequeue();
                            PrintCurrentViolet();
                            
                            await SendTextMsg(client, token, chatId, "Введите список тэгов (через запятую)");

                            break;
                        }
                        case Stage.Tags:
                        {
                            string[] tags = text.ToLower().Split(',');
                            List<string> trimmedTags = tags.Select(tag => tag.Trim()).ToList();
                            CurrentViolet.Tags = trimmedTags;
                            Stages.Dequeue();
                            PrintCurrentViolet();

                            VioletColor[] colors = Enum.GetValues(typeof(VioletColor)).Cast<VioletColor>().ToArray();
                            string[] descriptions = colors.Select(color => ExtensionMethods.GetEnumDescription(color).ToLower()).ToArray();
                            string resultDescriptions = String.Join(", ", descriptions);
                            await SendTextMsg(client, token, chatId, $"Введите список цветов фиалки (через запятую). Список возможных цветов: {resultDescriptions}.");

                            break;
                        }
                        case Stage.Colors:
                        {
                            string[] colors = text.ToLower().Split(',');
                            List<string> trimmedColors = colors.Select(tag => tag.Trim()).ToList();
                            VioletColor[] allColors = Enum.GetValues(typeof(VioletColor)).Cast<VioletColor>().ToArray();
                            List<VioletColor> selectedColors = allColors.Where(color =>
                                trimmedColors.Contains(ExtensionMethods.GetEnumDescription(color).ToLower())).ToList();
                            CurrentViolet.Colors = selectedColors;
                            Stages.Dequeue();
                            PrintCurrentViolet();

                            await SendTextMsg(client, token, chatId, "Введите дату селекции (например: 15.12.2022)");

                            break;
                        }
                        case Stage.Date:
                        {
                            CurrentViolet.BreedingDate = DateTime
                                .ParseExact(text, "dd.MM.yyyy", CultureInfo.InvariantCulture);
                            Stages.Dequeue();
                            PrintCurrentViolet();
                            
                            await SendTextMsg(client, token, chatId, "Отправьте три изображения новой фиалки в этот чат");

                            break;
                        }
                        case Stage.Images:
                        {
                            await SendTextMsg(client, token, chatId, "Отправьте изображение фиалки");
                            PrintCurrentViolet();

                            break;
                        }
                        default:
                        {
                            throw new ArgumentOutOfRangeException();
                        }
                    }
                }

                if (message.Type == MessageType.Document)
                {
                    Document messageDocument = message.Document;
                    if (messageDocument == null)
                    {
                        return;
                    }

                    if (messageDocument.FileName.EndsWith("jpg") == false)
                    {
                        await SendTextMsg(client, token, chatId, "Отправьте изображение в формате .jpg");
                        PrintCurrentViolet();
                        return;
                    }

                    if (CurrentMode == Mode.Adding && Stages.Peek() == Stage.Images)
                    {
                        string fileId = messageDocument.FileId;

                        string violetDirInTemp = Path.Combine(Path.GetTempPath(), CurrentViolet.Id.ToString());
                        if (Directory.Exists(violetDirInTemp) == false)
                        {
                            Directory.CreateDirectory(violetDirInTemp);
                        }

                        string fullPath = Path.Combine(violetDirInTemp, messageDocument.FileName);
                        await using FileStream fileStream = System.IO.File.OpenWrite(fullPath);
                        await client.GetInfoAndDownloadFileAsync(fileId, fileStream, token);
                        
                        CurrentViolet.Images.Add(new Image(false, fullPath, fullPath, fullPath, fullPath));
                        
                        PrintCurrentViolet();
                    }
                }
                
                // check current violet
                if (new[]
                    {
                        CurrentViolet.Name,
                        CurrentViolet.Breeder,
                        CurrentViolet.Description
                    }.All(s => String.IsNullOrEmpty(s) == false) &&
                    CurrentViolet.Tags.Count > 0 &&
                    CurrentViolet.Images.Count == 3 &&
                    CurrentViolet.Colors.Count > 0)
                {
                    Console.WriteLine("Publish violet");

                    BufferedCommandResult result = await Cli.Wrap(_maker.FullName)
                        .WithArguments(
                            $"-n \"{CurrentViolet.Name}\" -b \"{CurrentViolet.Breeder}\" -d \"{CurrentViolet.Description}\" --date \"{CurrentViolet.BreedingDate}\" --image1 \"{CurrentViolet.Images[0].W300}\" --image2 \"{CurrentViolet.Images[1].W300}\" --image3 \"{CurrentViolet.Images[2].W300}\" --chimera \"{CurrentViolet.IsChimera}\" -t {String.Join(' ', CurrentViolet.Tags.Select(tag => $"\"{tag}\""))} --colors {String.Join(' ', CurrentViolet.Colors.Select(color => $"\"{color}\""))}")
                        .ExecuteBufferedAsync();

                    if (result.StandardOutput.Contains("Success"))
                    {
                        await SendTextMsg(client, token, chatId, "Фиалка успешно опубликована");
                        Console.WriteLine(result.StandardOutput);
                    }
                    else
                    {
                        Console.WriteLine(result.StandardError);
                        Console.WriteLine(result.StandardOutput);
                    }
                    
                    Reset();
                }
                else
                {
                    Console.WriteLine("Violet not ready for publish");
                }

                break;
            }
            case Mode.Idle:
            {
                if (message.Type == MessageType.Text)
                {
                    string text = message.Text;
                    Console.WriteLine($"Message from user: \"{text}\"");

                    if (text == "/start")
                    {
                        CurrentMode = Mode.Adding;
                        CurrentViolet = new Violet(Guid.NewGuid(), "", "", "", new List<string>(),
                            DateTime.Now, new List<Image>(), false, new List<VioletColor>());

                        await SendTextMsg(client, token, chatId, "Введите имя новой фиалки");
                    }
                    else
                    {
                        await SendTextMsg(client, token, chatId,
                            "Чтобы начать публикацию новой фиалки наберите \"/start\"");
                    }
                }

                if (message.Type == MessageType.Document)
                {
                    await SendTextMsg(client, token, chatId,
                        "Чтобы начать публикацию новой фиалки наберите \"/start\"");
                }

                break;
            }
            default:
            {
                throw new ArgumentOutOfRangeException();
            }
        }
    }

    private void Reset()
    {
        CurrentViolet = null;
        CurrentMode = Mode.Idle;
        Stages = ReloadStages();
    }

    private void PrintCurrentViolet()
    {
        string serialize = GetSerializedViolet();
        Console.WriteLine(serialize);
    }

    private string GetSerializedViolet()
    {
        string serialize = JsonSerializer.Serialize(CurrentViolet);
        return serialize;
    }

    private static async Task SendTextMsg(ITelegramBotClient client, CancellationToken token, long chatId, string msg)
    {
        await client.SendTextMessageAsync(chatId,
            msg,
            cancellationToken: token);
    }
}