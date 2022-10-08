using System.Text.Json;
using CliWrap;
using CliWrap.Buffered;
using SharedLibrary;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TelegramBotConsole;

internal class Runner2
{
    private readonly FileInfo _maker;
    private Violet _currentViolet;
    private Queue<IUnit> _stages = new();
    private readonly TelegramBotClient _client;
    private IUnit _currentStage;

    public Runner2(string apiKey, FileInfo maker)
    {
        _maker = maker;
        
        _client = new(apiKey);
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
        if (message == null)
        {
            return;
        }

        long chatId = message.Chat.Id;

        if (message.Text != null && message.Text.ToLower() == "/start")
        {
            _currentViolet = new Violet(Guid.NewGuid(), "", "", "", new List<string>(),
                DateTime.Now, new List<Image>(), false, new List<VioletColor>());
            _stages = new Queue<IUnit>(new IUnit[]
            {
                new NameUnit(_client),
                new DescriptionUnit(_client),
                new BreederUnit(_client),
                new TagsUnit(_client),
                new ColorsUnit(_client),
                new ChimeraUnit(_client),
                new DateUnit(_client),
                new FirstImageUnit(_client),
                new SecondImageUnit(_client),
                new ThirdImageUnit(_client)
            });
            _currentStage = _stages.Dequeue();
            
            await _client.SendTextMessageAsync(chatId, "Процесс создания фиалки запущен");

            await _currentStage.Question(chatId);
            return;
        }
        
        if (message.Text != null && message.Text.ToLower() == "/stop")
        {
            Reset();
            await _client.SendTextMessageAsync(chatId, "Процесс создания фиалки остановлен");
            
            return;
        }

        if (_stages.Count == 0 && _currentStage == null && _currentViolet != null)
        {
            return;
        }

        (bool, string) validationResult = _currentStage!.Validate(message);
        if (validationResult.Item1 == false)
        {
            await _client.SendTextMessageAsync(chatId, validationResult.Item2);
            return;
        }

        (bool, string) actionResult = await _currentStage.RunAction(_currentViolet, message);
        _currentStage.PrintViolet(_currentViolet);
        if (actionResult.Item1 == false)
        {
            await _client.SendTextMessageAsync(chatId, actionResult.Item2);
            return;
        }

        _currentStage = _stages.Count > 0 ? _stages.Dequeue() : null;
        if (_currentStage != null)
        {
            await _currentStage.Question(chatId);
        }
        
        await PublishNewViolet(chatId);
    }

    private void Reset()
    {
        _currentViolet = null;
        _currentStage = null;
        _stages = new Queue<IUnit>();
    }

    private async Task PublishNewViolet(ChatId chatId)
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
                    $"-n \"{_currentViolet.Name}\" -b \"{_currentViolet.Breeder}\" -d \"{_currentViolet.Description}\" --date \"{_currentViolet.BreedingDate}\" --image1 \"{_currentViolet.Images[0].W300}\" --image2 \"{_currentViolet.Images[1].W300}\" --image3 \"{_currentViolet.Images[2].W300}\" --chimera \"{_currentViolet.IsChimera}\" -t {String.Join(' ', _currentViolet.Tags.Select(tag => $"\"{tag}\""))} --colors {String.Join(' ', _currentViolet.Colors.Select(color => $"\"{color}\""))}")
                .ExecuteBufferedAsync();

            if (result.StandardOutput.Contains("Success"))
            {
                await _client.SendTextMessageAsync(chatId, "Фиалка успешно опубликована");

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

internal interface IUnit
{
    Task Question(ChatId chatId);
    (bool,string) Validate(Message message);
    Task<(bool, string)> RunAction(Violet violet, Message message);
    void PrintViolet(Violet violet)
    {
        Console.WriteLine(JsonSerializer.Serialize(violet));
        Console.WriteLine();
    }
}

internal class NameUnit : IUnit
{
    private readonly ITelegramBotClient _client;

    public NameUnit(ITelegramBotClient client)
    {
        _client = client;
    }

    public async Task Question(ChatId chatId)
    {
        await _client.SendTextMessageAsync(chatId, "Введите имя новой фиалки");
    }

    public (bool, string) Validate(Message message)
    {
        if (message.Type != MessageType.Text)
        {
            return (false, "Ожидалось текстовое сообщение. Повторите ввод имени.");
        }
        
        return message.Text!.Length > 1 ? (true, "") : (false, "Имя должно содержать более 1 символа. Повторите ввод имени.");
    }

    public Task<(bool, string)> RunAction(Violet violet, Message message)
    {
        violet.Name = message.Text;
        return Task.FromResult((true, ""));
    }
}

internal class DescriptionUnit : IUnit
{
    private readonly ITelegramBotClient _client;

    public DescriptionUnit(ITelegramBotClient client)
    {
        _client = client;
    }

    public async Task Question(ChatId chatId)
    {
        await _client.SendTextMessageAsync(chatId, "Введите описание новой фиалки");
    }
    
    public (bool, string) Validate(Message message)
    {
        if (message.Type != MessageType.Text)
        {
            return (false, "Ожидалось текстовое сообщение. Повторите ввод описания.");
        }
        
        return message.Text!.Length > 1 ? (true, "") : (false, "Описание должно содержать более 1 символа. Повторите ввод описания.");
    }

    public Task<(bool, string)> RunAction(Violet violet, Message message)
    {
        violet.Description = message.Text;
        return Task.FromResult((true, ""));
    }
}

internal class FirstImageUnit : IUnit
{
    private readonly ITelegramBotClient _client;

    public FirstImageUnit(ITelegramBotClient client)
    {
        _client = client;
    }

    public async Task Question(ChatId chatId)
    {
        await _client.SendTextMessageAsync(chatId, "Отправьте три фотографии новой фиалки");
    }
    
    public (bool, string) Validate(Message message)
    {
        return message.Type != MessageType.Photo ? (false, "Ожидался файл фотографии. Повторите отправку файла.") : (true, "");
    }

    public async Task<(bool, string)> RunAction(Violet violet, Message message)
    {
        PhotoSize photoSize = message.Photo!.Last();
        string fileId = photoSize.FileId;
        string violetDirInTemp = Path.Combine(Path.GetTempPath(), violet.Id.ToString());
        if (Directory.Exists(violetDirInTemp) == false)
        {
            Directory.CreateDirectory(violetDirInTemp);
        }

        string fullPath = Path.Combine(violetDirInTemp, $"{Guid.NewGuid()}.jpg");
        await using FileStream fileStream = System.IO.File.OpenWrite(fullPath);
        await _client.GetInfoAndDownloadFileAsync(fileId, fileStream);
        violet.Images.Add(new Image(false, fullPath, fullPath, fullPath, fullPath));

        return (true, "");
    }
}

internal class SecondImageUnit : IUnit
{
    private readonly ITelegramBotClient _client;

    public SecondImageUnit(ITelegramBotClient client)
    {
        _client = client;
    }

    public Task Question(ChatId chatId)
    {
        return Task.CompletedTask;
    }
    
    public (bool, string) Validate(Message message)
    {
        return message.Type != MessageType.Photo ? (false, "Ожидался файл фотографии. Повторите отправку файла.") : (true, "");
    }

    public async Task<(bool, string)> RunAction(Violet violet, Message message)
    {
        PhotoSize photoSize = message.Photo!.Last();
        string fileId = photoSize.FileId;
        string violetDirInTemp = Path.Combine(Path.GetTempPath(), violet.Id.ToString());
        if (Directory.Exists(violetDirInTemp) == false)
        {
            Directory.CreateDirectory(violetDirInTemp);
        }

        string fullPath = Path.Combine(violetDirInTemp, $"{Guid.NewGuid()}.jpg");
        await using FileStream fileStream = System.IO.File.OpenWrite(fullPath);
        await _client.GetInfoAndDownloadFileAsync(fileId, fileStream);
        violet.Images.Add(new Image(false, fullPath, fullPath, fullPath, fullPath));

        return (true, "");
    }
}

internal class ThirdImageUnit : IUnit
{
    private readonly ITelegramBotClient _client;

    public ThirdImageUnit(ITelegramBotClient client)
    {
        _client = client;
    }

    public Task Question(ChatId chatId)
    {
        return Task.CompletedTask;
    }
    
    public (bool, string) Validate(Message message)
    {
        return message.Type != MessageType.Photo ? (false, "Ожидался файл фотографии. Повторите отправку файла.") : (true, "");
    }

    public async Task<(bool, string)> RunAction(Violet violet, Message message)
    {
        PhotoSize photoSize = message.Photo!.Last();
        string fileId = photoSize.FileId;
        string violetDirInTemp = Path.Combine(Path.GetTempPath(), violet.Id.ToString());
        if (Directory.Exists(violetDirInTemp) == false)
        {
            Directory.CreateDirectory(violetDirInTemp);
        }

        string fullPath = Path.Combine(violetDirInTemp, $"{Guid.NewGuid()}.jpg");
        await using FileStream fileStream = System.IO.File.OpenWrite(fullPath);
        await _client.GetInfoAndDownloadFileAsync(fileId, fileStream);
        violet.Images.Add(new Image(false, fullPath, fullPath, fullPath, fullPath));

        return (true, "");
    }
}

internal class BreederUnit : IUnit
{
    private readonly ITelegramBotClient _client;

    public BreederUnit(ITelegramBotClient client)
    {
        _client = client;
    }

    public async Task Question(ChatId chatId)
    {
        await _client.SendTextMessageAsync(chatId, "Введите имя селекционера фиалки");
    }
    
    public (bool, string) Validate(Message message)
    {
        if (message.Type != MessageType.Text)
        {
            return (false, "Ожидалось текстовое сообщение. Повторите ввод имени селекционера.");
        }
        
        return message.Text!.Length > 1 ? (true, "") : (false, "Именя селекционера должно содержать более 1 символа. Повторите ввод имени.");
    }

    public Task<(bool, string)> RunAction(Violet violet, Message message)
    {
        violet.Breeder = message.Text;
        return Task.FromResult((true, ""));
    }
}

internal class TagsUnit : IUnit
{
    private readonly ITelegramBotClient _client;

    public TagsUnit(ITelegramBotClient client)
    {
        _client = client;
    }

    public async Task Question(ChatId chatId)
    {
        await _client.SendTextMessageAsync(chatId, "Введите список тэгов (через запятую)");
    }
    
    public (bool, string) Validate(Message message)
    {
        if (message.Type != MessageType.Text)
        {
            return (false, "Ожидалось текстовое сообщение. Повторите ввод списока тэгов.");
        }

        string text = message.Text!.ToLower();
        
        if (text.Split(',').All(String.IsNullOrEmpty))
        {
            return (false, "Список тегов пуст. Повторите ввод списка тэгов.");
        }

        return (true, "");
    }

    public Task<(bool, string)> RunAction(Violet violet, Message message)
    {
        string text = message.Text!.ToLower().Trim();
        List<string> tags = text.Split(',')
            .Where(tag => String.IsNullOrEmpty(tag) == false)
            .Select(tag => tag.Trim())
            .ToList();

        violet.Tags = tags;

        return Task.FromResult((true, ""));
    }
}

internal class ColorsUnit : IUnit
{
    private readonly ITelegramBotClient _client;

    public ColorsUnit(ITelegramBotClient client)
    {
        _client = client;
    }

    public async Task Question(ChatId chatId)
    {
        VioletColor[] colors = Enum.GetValues(typeof(VioletColor))
            .Cast<VioletColor>()
            .ToArray();
        string[] descriptions = colors.Select(color => ExtensionMethods.GetEnumDescription(color)
                .ToLower()).ToArray();
        string resultDescriptions = String.Join(", ", descriptions);

        await _client
            .SendTextMessageAsync(chatId,
            $"Введите список цветов фиалки (через запятую). Список возможных цветов: {resultDescriptions}.");
    }
    
    public (bool, string) Validate(Message message)
    {
        if (message.Type != MessageType.Text)
        {
            return (false, "Ожидалось текстовое сообщение. Повторите ввод списока цветов.");
        }

        string text = message.Text!.ToLower();
        
        if (text.Split(',').All(String.IsNullOrEmpty))
        {
            return (false, "Список цветов пуст. Повторите ввод списка цветов.");
        }

        return (true, "");
    }

    public Task<(bool, string)> RunAction(Violet violet, Message message)
    {
        string text = message.Text!.ToLower().Trim();
        List<string> colors = text.Split(',')
            .Where(color => String.IsNullOrEmpty(color) == false)
            .Select(color => color.Trim().Replace('ё', 'е'))
            .ToList();
        VioletColor[] allColors = Enum.GetValues(typeof(VioletColor)).Cast<VioletColor>().ToArray();
        List<VioletColor> selectedColors = allColors.Where(color =>
            colors.Contains(ExtensionMethods.GetEnumDescription(color).ToLower())).ToList();
        violet.Colors = selectedColors;

        return Task.FromResult((true, ""));
    }
}

internal class ChimeraUnit : IUnit
{
    private readonly ITelegramBotClient _client;

    public ChimeraUnit(ITelegramBotClient client)
    {
        _client = client;
    }

    public async Task Question(ChatId chatId)
    {
        await _client.SendTextMessageAsync(chatId, "Является ли фиалка химерой (\'да\' или \'нет\')?");
    }

    public (bool, string) Validate(Message message)
    {
        if (message.Type != MessageType.Text)
        {
            return (false, "Ожидалось текстовое сообщение. Повторите ввод признака химеры.");
        }

        if (new[]{ "да", "нет" }.Contains(message.Text!.ToLower().Trim()) == false)
        {
            return (false, "Ожидался ответ который содержит либо \"да\" либо \"нет\". Повторите признака химеры.");
        }

        return (true, "");
    }

    public Task<(bool, string)> RunAction(Violet violet, Message message)
    {
        string text = message.Text!.ToLower().Trim();
        if (text.ToLower() == "да")
        {
            violet.IsChimera = true;
            return Task.FromResult((true, ""));
        }

        violet.IsChimera = false;
        return Task.FromResult((true, ""));
    }
}

internal class DateUnit : IUnit
{
    private readonly ITelegramBotClient _client;

    public DateUnit(ITelegramBotClient client)
    {
        _client = client;
    }

    public async Task Question(ChatId chatId)
    {
        await _client.SendTextMessageAsync(chatId, "Введите год селекции фиалки (например: 2022)");
    }

    public (bool, string) Validate(Message message)
    {
        if (message.Type != MessageType.Text)
        {
            return (false, "Ожидалось текстовое сообщение. Повторите ввод года селекции.");
        }

        string text = message.Text!.Trim();
        if (text.Length != 4)
        {
            return (false, "Введите год (четыре цифры)");
        }
        
        bool parseNumberResult = Int32.TryParse(text, out int _);
        if (parseNumberResult == false)
        {
            return (false, "Введите год (четыре цифры)");
        }

        return (true, "");
    }

    public Task<(bool, string)> RunAction(Violet violet, Message message)
    {
        string text = message.Text!.Trim();
        Int32.TryParse(text, out int parsedNumber);
        violet.BreedingDate = new DateTime(parsedNumber, 1, 1);
        return Task.FromResult((true, ""));
    }
}