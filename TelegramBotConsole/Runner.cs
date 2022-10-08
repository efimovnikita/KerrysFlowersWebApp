using CliWrap;
using CliWrap.Buffered;
using SharedLibrary;
using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramBotConsole.Units;

namespace TelegramBotConsole;

internal class Runner
{
    private readonly FileInfo _maker;
    private Violet _currentViolet;
    private Queue<IUnit> _stages = new();
    private readonly TelegramBotClient _client;
    private IUnit _currentStage;

    public Runner(string apiKey, FileInfo maker)
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

        if (_currentStage == null)
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