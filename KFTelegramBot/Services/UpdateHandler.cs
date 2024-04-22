using Microsoft.Extensions.Logging;
using SharedLibrary;
using System.Globalization;
using MongoDB.Driver;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.ReplyMarkups;
using Image = SharedLibrary.Image;

namespace KFTelegramBot.Services;

public class UpdateHandler : IUpdateHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly ILogger<UpdateHandler> _logger;
    private readonly IMemoryStateProvider _memoryStateProvider;
    private readonly IVioletRepository _violetRepository;

    public UpdateHandler(ITelegramBotClient botClient, ILogger<UpdateHandler> logger, IMemoryStateProvider memoryStateProvider, IVioletRepository violetRepository)
    {
        _botClient = botClient;
        _logger = logger;
        _memoryStateProvider = memoryStateProvider;
        _violetRepository = violetRepository;
    }

    public async Task HandleUpdateAsync(ITelegramBotClient _, Update update, CancellationToken cancellationToken)
    {
        var handler = update switch
        {
            { Message: { } message }                       => BotOnMessageReceived(message, cancellationToken),
            { EditedMessage: { } message }                 => BotOnMessageReceived(message, cancellationToken),
            { CallbackQuery: { } callbackQuery }           => BotOnCallbackQueryReceived(callbackQuery, cancellationToken),
            { InlineQuery: { } inlineQuery }               => BotOnInlineQueryReceived(inlineQuery, cancellationToken),
            { ChosenInlineResult: { } chosenInlineResult } => BotOnChosenInlineResultReceived(chosenInlineResult, cancellationToken),
            _                                              => UnknownUpdateHandlerAsync(update, cancellationToken)
        };

        await handler;
    }

    private async Task BotOnMessageReceived(Message message, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Receive message type: {MessageType}", message.Type);
        if (message.Type == MessageType.Photo)
        {
            await ProcessCommand(_botClient, message, cancellationToken);
        }

        if (message.Text is not { } messageText)
            return;

        if (_memoryStateProvider.IsContainUserId(message.Chat.Id) == false)
        {
            await _botClient.SendTextMessageAsync(message.Chat.Id,
                "У вас нет прав на использование этого бота.",
                cancellationToken: cancellationToken);
            return;
        }

        var action = messageText.Split(' ')[0] switch
        {
            "/add"             => AddCommand(_botClient, message, cancellationToken),
            "/edit"            => EditCommand(_botClient, message, cancellationToken),
            "/delete"          => DeleteCommand(_botClient, message,cancellationToken),
            "/reset"           => ResetCommand(_botClient, message,cancellationToken),
            "/test"            => TestCommand(_botClient, message,cancellationToken),
            _                  => ProcessCommand(_botClient, message, cancellationToken)
        };

        Message sentMessage = await action;
        _logger.LogInformation("The message was sent with userId: {SentMessageId}", sentMessage.MessageId);
    }

    private async Task<Message> TestCommand(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        var allViolets = _violetRepository.GetAllViolets();
        foreach (var violet in allViolets)
        {
            await _botClient.SendTextMessageAsync(message.Chat.Id,
                $"{violet.Id}\n{violet.Name}",
                cancellationToken: cancellationToken);
        }

        return await _botClient.SendTextMessageAsync(message.Chat.Id,
            "Done!",
            cancellationToken: cancellationToken);
    }

    private async Task<Message> ProcessCommand(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        var chatId = message.Chat.Id;
        var pipeline = _memoryStateProvider.GetCurrentPipeline(chatId);
        if (pipeline == null)
        {
            return await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: GetUsageText(),
                cancellationToken: cancellationToken);
        }

        return await pipeline.ProcessCurrentItem(message, botClient);
    }

    private Task<Message> ResetCommand(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    private static async Task<Message> Usage(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        return await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: GetUsageText(),
            replyMarkup: new ReplyKeyboardRemove(),
            cancellationToken: cancellationToken);
    }

    private static string GetUsageText()
    {
        const string usage = "Usage:\n" +
                             "/inline_keyboard - send inline keyboard\n" +
                             "/keyboard    - send custom keyboard\n" +
                             "/remove      - remove custom keyboard\n" +
                             "/photo       - send a photo\n" +
                             "/request     - request location or contact\n" +
                             "/inline_mode - send keyboard with Inline Query";
        return usage;
    }

    private Task<Message> DeleteCommand(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    private Task<Message> EditCommand(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    private Task<Message> AddCommand(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        var violetAddingPipeline = new VioletAddingPipeline([
            new VioletNamePipelineItem(),
            new VioletBreederPipelineItem(),
            new VioletDescriptionPipelineItem(),
            new VioletTagsPipelineItem(),
            new VioletBreedingDatePipelineItem(),
            new VioletChimeraPipelineItem(),
            new VioletColorsPipelineItem(),
            new VioletImagesPipelineItem()
        ], _violetRepository);

        _memoryStateProvider.SetCurrentPipeline(violetAddingPipeline, message.Chat.Id);
        return violetAddingPipeline.AskAQuestionForNextItem(message, botClient);
    }

    private async Task BotOnCallbackQueryReceived(CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received inline keyboard callback from: {CallbackQueryId}", callbackQuery.Id);

        await _botClient.AnswerCallbackQueryAsync(
            callbackQueryId: callbackQuery.Id,
            text: $"Received {callbackQuery.Data}",
            cancellationToken: cancellationToken);

        await _botClient.SendTextMessageAsync(
            chatId: callbackQuery.Message!.Chat.Id,
            text: $"Received {callbackQuery.Data}",
            cancellationToken: cancellationToken);
    }

    #region Inline Mode

    private async Task BotOnInlineQueryReceived(InlineQuery inlineQuery, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received inline query from: {InlineQueryFromId}", inlineQuery.From.Id);

        InlineQueryResult[] results = {
            // displayed result
            new InlineQueryResultArticle(
                id: "1",
                title: "TgBots",
                inputMessageContent: new InputTextMessageContent("hello"))
        };

        await _botClient.AnswerInlineQueryAsync(
            inlineQueryId: inlineQuery.Id,
            results: results,
            cacheTime: 0,
            isPersonal: true,
            cancellationToken: cancellationToken);
    }

    private async Task BotOnChosenInlineResultReceived(ChosenInlineResult chosenInlineResult, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received inline result: {ChosenInlineResultId}", chosenInlineResult.ResultId);

        await _botClient.SendTextMessageAsync(
            chatId: chosenInlineResult.From.Id,
            text: $"You chose result with Id: {chosenInlineResult.ResultId}",
            cancellationToken: cancellationToken);
    }

    #endregion

#pragma warning disable IDE0060 // Remove unused parameter
#pragma warning disable RCS1163 // Unused parameter.
    private Task UnknownUpdateHandlerAsync(Update update, CancellationToken cancellationToken)
#pragma warning restore RCS1163 // Unused parameter.
#pragma warning restore IDE0060 // Remove unused parameter
    {
        _logger.LogInformation("Unknown update type: {UpdateType}", update.Type);
        return Task.CompletedTask;
    }

    public async Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        var errorMessage = exception switch
        {
            ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        _logger.LogInformation("HandleError: {ErrorMessage}", errorMessage);

        // Cooldown in case of network connection error
        if (exception is RequestException)
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
    }
}

public class MemoryStateProvider : IMemoryStateProvider
{
    public MemoryStateProvider(long[] ids)
    {
        State = new Dictionary<long, IPipeline?>(ids.Length);
        foreach (var id in ids) State.Add(id, null);
    }

    public Dictionary<long, IPipeline?> State { get; set; }
    public Dictionary<long, IPipeline?> GetState() => State;
    public bool IsContainUserId(long userId) => State.ContainsKey(userId);
    public IPipeline? GetCurrentPipeline(long userId) => IsContainUserId(userId) ? State[userId] : null;
    public void SetCurrentPipeline(IPipeline pipeline, long userId)
    {
        if (IsContainUserId(userId) == false)
        {
            return;
        }

        State[userId] = pipeline;
    }
}

public interface IPipeline
{
    Task<Message> AskAQuestionForNextItem(Message message, ITelegramBotClient botClient);
    Task<Message> ProcessCurrentItem(Message message, ITelegramBotClient botClient);
}

public interface IMemoryStateProvider
{
    Dictionary<long, IPipeline?> GetState();
    bool IsContainUserId(long userId);
    IPipeline? GetCurrentPipeline(long userId);
    void SetCurrentPipeline(IPipeline pipeline, long userId);
}

public class VioletAddingPipeline : IPipeline
{
    private readonly IVioletRepository _violetRepository;

    public VioletAddingPipeline(IPipelineItem[] items, IVioletRepository violetRepository)
    {
        PipelineItems = new Queue<IPipelineItem>(items);
        GrowingViolet = new Violet(Guid.NewGuid(), "", "", "", [], DateTime.Now, [], false, []);
        _violetRepository = violetRepository;
    }

    public Queue<IPipelineItem> PipelineItems { get; set; }
    public Violet GrowingViolet { get; set; }
    public Task<Message> AskAQuestionForNextItem(Message message, ITelegramBotClient botClient)
    {
        var pipelineItem = PipelineItems.Peek();
        return pipelineItem.AskAQuestion(message, botClient);
    }

    public Task<Message> ProcessCurrentItem(Message message, ITelegramBotClient botClient)
    {
        var pipelineItem = PipelineItems.Peek();
        var (validationResult, validationMsg) = pipelineItem.ValidateInput(message, botClient);
        if (validationResult == false)
        {
            return validationMsg!;
        }

        var (jobResult, jobMsg) = pipelineItem.DoTheJob(message, GrowingViolet, botClient);
        if (jobResult == false)
        {
            return jobMsg!;
        }

        PipelineItems.Dequeue();

        var peekResult = PipelineItems.TryPeek(out var nextItem);
        if (peekResult)
        {
            return nextItem!.AskAQuestion(message, botClient);
        }

        // queue is empty
        var sentResult = SendDataToTheDatabase(GrowingViolet, botClient, message.Chat.Id);

        return botClient.SendTextMessageAsync(message.Chat.Id, sentResult ? "Данные добавлены в базу данных." : "Не удалось добавить данные в базу данных.");
    }

    private bool SendDataToTheDatabase(Violet violet, ITelegramBotClient botClient, long chatId)
    {
        try
        {
            _violetRepository.GetOrCreateViolet(violet.Id,
                violet.Name,
                violet.Breeder,
                violet.Description,
                violet.Tags,
                violet.BreedingDate,
                violet.Images,
                violet.IsChimera,
                violet.Colors);

            return true;
        }
        catch (Exception e)
        {
            _ = botClient.SendTextMessageAsync(chatId, e.Message).Result;
            return false;
        }
    }
}

public class VioletNamePipelineItem : IPipelineItem
{
    public Task<Message> AskAQuestion(Message message, ITelegramBotClient botClient) =>
        botClient.SendTextMessageAsync(message.Chat.Id, "Введите имя новой фиалки");

    public (bool, Task<Message>?) ValidateInput(Message message, ITelegramBotClient botClient)
    {
        if (message.Type != MessageType.Text)
        {
            return (false,
                botClient.SendTextMessageAsync(message.Chat.Id, "Ожидалось текстовое сообщение. Повторите ввод."));
        }

        return (true, null);
    }

    public (bool, Task<Message>?) DoTheJob(Message message, object affectedObject, ITelegramBotClient botClient)
    {
        if (affectedObject is not Violet violet)
        {
            return (false,
                botClient.SendTextMessageAsync(message.Chat.Id, "Редактируемый объект неверного типа. Повторите ввод."));
        }

        violet.Name = message.Text!.Trim();
        return (true, null);
    }
}

public class VioletBreederPipelineItem : IPipelineItem
{
    public Task<Message> AskAQuestion(Message message, ITelegramBotClient botClient) =>
        botClient.SendTextMessageAsync(message.Chat.Id, "Введите имя селекционера фиалки");

    public (bool, Task<Message>?) ValidateInput(Message message, ITelegramBotClient botClient)
    {
        if (message.Type != MessageType.Text)
        {
            return (false,
                botClient.SendTextMessageAsync(message.Chat.Id, "Ожидалось текстовое сообщение. Повторите ввод."));
        }

        return (true, null);
    }

    public (bool, Task<Message>?) DoTheJob(Message message, object affectedObject, ITelegramBotClient botClient)
    {
        if (affectedObject is not Violet violet)
        {
            return (false,
                botClient.SendTextMessageAsync(message.Chat.Id, "Редактируемый объект неверного типа. Повторите ввод."));
        }

        violet.Breeder = message.Text!.Trim();
        return (true, null);
    }
}

public class VioletDescriptionPipelineItem : IPipelineItem
{
    public Task<Message> AskAQuestion(Message message, ITelegramBotClient botClient) =>
        botClient.SendTextMessageAsync(message.Chat.Id, "Введите описание фиалки");

    public (bool, Task<Message>?) ValidateInput(Message message, ITelegramBotClient botClient)
    {
        if (message.Type != MessageType.Text)
        {
            return (false,
                botClient.SendTextMessageAsync(message.Chat.Id, "Ожидалось текстовое сообщение. Повторите ввод."));
        }

        return (true, null);
    }

    public (bool, Task<Message>?) DoTheJob(Message message, object affectedObject, ITelegramBotClient botClient)
    {
        if (affectedObject is not Violet violet)
        {
            return (false,
                botClient.SendTextMessageAsync(message.Chat.Id, "Редактируемый объект неверного типа. Повторите ввод."));
        }

        violet.Description = message.Text!.Trim();
        return (true, null);
    }
}

public class VioletTagsPipelineItem : IPipelineItem
{
    public Task<Message> AskAQuestion(Message message, ITelegramBotClient botClient) =>
        botClient.SendTextMessageAsync(message.Chat.Id, "Введите теги (через запятую) для новой фиалки");

    public (bool, Task<Message>?) ValidateInput(Message message, ITelegramBotClient botClient)
    {
        if (message.Type != MessageType.Text)
        {
            return (false,
                botClient.SendTextMessageAsync(message.Chat.Id, "Ожидалось текстовое сообщение. Повторите ввод."));
        }

        return (true, null);
    }

    public (bool, Task<Message>?) DoTheJob(Message message, object affectedObject, ITelegramBotClient botClient)
    {
        if (affectedObject is not Violet violet)
        {
            return (false,
                botClient.SendTextMessageAsync(message.Chat.Id, "Редактируемый объект неверного типа. Повторите ввод."));
        }

        var tags = message.Text!.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray();

        violet.Tags.AddRange(tags);
        return (true, null);
    }
}

public class VioletBreedingDatePipelineItem : IPipelineItem
{
    private const string DateFormat = "dd.MM.yyyy";

    public Task<Message> AskAQuestion(Message message, ITelegramBotClient botClient) =>
        botClient.SendTextMessageAsync(message.Chat.Id, "Введите дату селекции фиалки (в формате \"dd.MM.yyyy\")");

    public (bool, Task<Message>?) ValidateInput(Message message, ITelegramBotClient botClient)
    {
        if (message.Type != MessageType.Text)
        {
            return (false,
                botClient.SendTextMessageAsync(message.Chat.Id, "Ожидалось текстовое сообщение. Повторите ввод."));
        }

        var text = message.Text;

        if (DateTime.TryParseExact(text, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out _) == false)
        {
            return (false,
                botClient.SendTextMessageAsync(message.Chat.Id, "Неверный формат даты. Повторите ввод."));
        }

        return (true, null);
    }

    public (bool, Task<Message>?) DoTheJob(Message message, object affectedObject, ITelegramBotClient botClient)
    {
        if (affectedObject is not Violet violet)
        {
            return (false,
                botClient.SendTextMessageAsync(message.Chat.Id, "Редактируемый объект неверного типа. Повторите ввод."));
        }

        var text = message.Text;
        var dateTime = DateTime.ParseExact(text!, DateFormat, CultureInfo.InvariantCulture);

        violet.BreedingDate = dateTime;
        return (true, null);
    }
}

public class VioletChimeraPipelineItem : IPipelineItem
{
    public Task<Message> AskAQuestion(Message message, ITelegramBotClient botClient) =>
        botClient.SendTextMessageAsync(message.Chat.Id, "Фиалка химера или нет (да или нет)?");

    public (bool, Task<Message>?) ValidateInput(Message message, ITelegramBotClient botClient)
    {
        if (message.Type != MessageType.Text)
        {
            return (false,
                botClient.SendTextMessageAsync(message.Chat.Id, "Ожидалось текстовое сообщение. Повторите ввод."));
        }

        return (true, null);
    }

    public (bool, Task<Message>?) DoTheJob(Message message, object affectedObject, ITelegramBotClient botClient)
    {
        if (affectedObject is not Violet violet)
        {
            return (false,
                botClient.SendTextMessageAsync(message.Chat.Id, "Редактируемый объект неверного типа. Повторите ввод."));
        }

        var text = message.Text!;

        if ((text.Equals("да", StringComparison.OrdinalIgnoreCase) || text.Equals("нет", StringComparison.OrdinalIgnoreCase)) == false)
        {
            return (false,
                botClient.SendTextMessageAsync(message.Chat.Id, "Дайте ответ в формате \"да\" или \"нет\". Повторите ввод."));
        }

        if (!text.Equals("да", StringComparison.OrdinalIgnoreCase))
        {
            violet.IsChimera = false;
            return (true, null);
        }

        violet.IsChimera = true;
        return (true, null);
    }
}

public class VioletColorsPipelineItem : IPipelineItem
{
    public Task<Message> AskAQuestion(Message message, ITelegramBotClient botClient)
    {
        var possibleVariants = GetPossibleVariants();

        var text = $"Введите, через запятую, цвета новой фиалки.\n{possibleVariants}";
        return botClient.SendTextMessageAsync(message.Chat.Id, text, parseMode:ParseMode.Markdown);
    }

    private static string GetPossibleVariants()
    {
        var values = Enum.GetValues(typeof(VioletColor));
        var colors = values
            .OfType<VioletColor>()
            .Select(color => $"`{ExtensionMethods.GetEnumDescription(color)}`")
            .ToArray();

        var possibleVariants = $"Возможные варианты:\n{string.Join(',', colors)}";
        return possibleVariants;
    }

    public (bool, Task<Message>?) ValidateInput(Message message, ITelegramBotClient botClient)
    {
        if (message.Type != MessageType.Text)
        {
            return (false,
                botClient.SendTextMessageAsync(message.Chat.Id, "Ожидалось текстовое сообщение. Повторите ввод."));
        }

        var values = Enum.GetValues(typeof(VioletColor));
        var possibleColors = values
            .OfType<VioletColor>()
            .Select(color => ExtensionMethods.GetEnumDescription(color))
            .ToArray();

        var colors = message.Text!
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .ToArray();

        var allColorsExist = colors.All(c => possibleColors.Contains(c, StringComparer.OrdinalIgnoreCase));
        if (allColorsExist == false)
        {
            return (false,
                botClient.SendTextMessageAsync(message.Chat.Id, $"Выбраны не корректные цвета.\n{GetPossibleVariants()}.\nПовторите ввод.", parseMode:ParseMode.Markdown));
        }

        return (true, null);
    }

    public (bool, Task<Message>?) DoTheJob(Message message, object affectedObject, ITelegramBotClient botClient)
    {
        if (affectedObject is not Violet violet)
        {
            return (false,
                botClient.SendTextMessageAsync(message.Chat.Id, "Редактируемый объект неверного типа. Повторите ввод."));
        }

        var values = Enum.GetValues(typeof(VioletColor));
        var possibleColors = values
            .OfType<VioletColor>()
            .ToDictionary(color => ExtensionMethods.GetEnumDescription(color), color => color);

        var colors = message.Text!
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Select(s => possibleColors[s])
            .ToArray();

        violet.Colors.AddRange(colors);
        return (true, null);
    }
}

public class VioletImagesPipelineItem : IPipelineItem
{
    public Task<Message> AskAQuestion(Message message, ITelegramBotClient botClient) =>
        botClient.SendTextMessageAsync(message.Chat.Id, "Отправьте изображения фиалки (3 шт., минимальный размер по ширине 700 px)");

    public (bool, Task<Message>?) ValidateInput(Message message, ITelegramBotClient botClient)
    {
        if (message.Type != MessageType.Photo)
        {
            return (false,
                botClient.SendTextMessageAsync(message.Chat.Id, "Ожидалось сообщение в формате фотографии. Повторите ввод."));
        }

        return (true, null);
    }

    public (bool, Task<Message>?) DoTheJob(Message message, object affectedObject, ITelegramBotClient botClient)
    {
        if (affectedObject is not Violet violet)
        {
            return (false,
                botClient.SendTextMessageAsync(message.Chat.Id, "Редактируемый объект неверного типа. Повторите ввод."));
        }

        var photoSize = message.Photo![^1];
        
        var file = botClient.GetFileAsync(photoSize.FileId).Result;

        var path = Path.GetTempPath();
        var fileName = Path.Combine(path, file.FileId + ".jpg");
        using (var stream = System.IO.File.OpenWrite(fileName))
        {
            botClient.DownloadFileAsync(file.FilePath!, stream).GetAwaiter().GetResult();
        }

        var resizer = new ImageResizer();
        var hasMinimumWidth = resizer.HasMinimumWidth(fileName, 700);
        if (hasMinimumWidth == false)
        {
            return (false,
                botClient.SendTextMessageAsync(message.Chat.Id, "Изображение должноб быть не менее чем 700 px в ширину. Повторите ввод."));
        }

        var name300 = $"{Path.GetFileNameWithoutExtension(fileName)}_300.jpg";
        var path300 = Path.Combine(path, name300);
        resizer.ResizeImage(fileName, path300, 300);
        var imageBytes300 = System.IO.File.ReadAllBytes(path300);
        var base64300 = Convert.ToBase64String(imageBytes300);

        var name330 = $"{Path.GetFileNameWithoutExtension(fileName)}_330.jpg";
        var path330 = Path.Combine(path, name330);
        resizer.ResizeImage(fileName, path330, 330);
        var imageBytes330 = System.IO.File.ReadAllBytes(path330);
        var base64330 = Convert.ToBase64String(imageBytes330);

        var name500 = $"{Path.GetFileNameWithoutExtension(fileName)}_500.jpg";
        var path500 = Path.Combine(path, name500);
        resizer.ResizeImage(fileName, path500, 500);
        var imageBytes500 = System.IO.File.ReadAllBytes(path500);
        var base64500 = Convert.ToBase64String(imageBytes500);

        var name700 = $"{Path.GetFileNameWithoutExtension(fileName)}_700.jpg";
        var path700 = Path.Combine(path, name700);
        resizer.ResizeImage(fileName, path700, 700);
        var imageBytes700 = System.IO.File.ReadAllBytes(path700);
        var base64700 = Convert.ToBase64String(imageBytes700);

        var image = new Image(violet.Images.Count == 0, base64300, base64330, base64500, base64700);

        violet.Images.Add(image);

        if (violet.Images.Count == 3)
        {
            _ = botClient.SendTextMessageAsync(message.Chat.Id, $"Загружено {violet.Images.Count} из 3 изображений.")
                .Result;
            return (true, null);
        }

        return (false,
            botClient.SendTextMessageAsync(message.Chat.Id, $"Загружено {violet.Images.Count} из 3 изображений."));
    }

    public class ImageResizer
    {
        public void ResizeImage(string inputFile, string outputFile, int newWidth)
        {
            using var image = SixLabors.ImageSharp.Image.Load(inputFile);
            var newHeight = (int)(image.Height / ((float)image.Width / newWidth));

            image.Mutate(ctx => ctx.Resize(newWidth, newHeight));

            image.SaveAsJpeg(outputFile);
        }

        public bool HasMinimumWidth(string imagePath, int minWidth)
        {
            using var image = SixLabors.ImageSharp.Image.Load(imagePath);
            return image.Width >= minWidth;
        }
    }
}

public interface IPipelineItem
{
    Task<Message> AskAQuestion(Message message, ITelegramBotClient botClient);
    (bool, Task<Message>?) ValidateInput(Message message, ITelegramBotClient botClient);
    (bool, Task<Message>?) DoTheJob(Message message, object affectedObject, ITelegramBotClient botClient);
}

public interface IVioletRepository
{
    Violet GetOrCreateViolet(Guid id, string name, string breeder, string description, List<string> tags,
        DateTime breedingDate, List<Image> images, bool isChimera, List<VioletColor> colors);
    bool UpdateViolet(Violet updatedViolet);
    List<Violet> GetAllViolets();
    Violet GetVioletById(Guid id);
    bool DeleteViolet(Guid id);
}

public class VioletRepository : IVioletRepository
{
    private readonly IMongoDatabase _database;

    public VioletRepository(string connectionString, string dbDatabase)
    {
        MongoClient client = new MongoClient(connectionString);
        _database = client.GetDatabase(dbDatabase);
    }

    public Violet GetOrCreateViolet(Guid id, string name, string breeder, string description, List<string> tags,
        DateTime breedingDate, List<Image> images, bool isChimera, List<VioletColor> colors)
    {
        var violetsCollection = _database.GetCollection<Violet>("Violets");

        var filter = Builders<Violet>.Filter.Eq("Id", id);

        var violet = violetsCollection.Find(filter).FirstOrDefault();
        if (violet != null)
        {
            return violet;
        }

        violet = new Violet(id, name, breeder, description, tags, breedingDate, images, isChimera, colors);
        violetsCollection.InsertOne(violet);

        return violet;
    }

    public bool UpdateViolet(Violet updatedViolet)
    {
        var violetsCollection = _database.GetCollection<Violet>("Violets");
        var filter = Builders<Violet>.Filter.Eq("Id", updatedViolet.Id);

        var updateDefinition = Builders<Violet>.Update
            .Set(v => v.Name, updatedViolet.Name)
            .Set(v => v.Breeder, updatedViolet.Breeder)
            .Set(v => v.Description, updatedViolet.Description)
            .Set(v => v.Tags, updatedViolet.Tags)
            .Set(v => v.Images, updatedViolet.Images)
            .Set(v => v.BreedingDate, updatedViolet.BreedingDate)
            .Set(v => v.IsChimera, updatedViolet.IsChimera)
            .Set(v => v.Colors, updatedViolet.Colors)
            .CurrentDate(v => v.PublishDate); // Assuming we want to update the publish date as the current date
        
        var updateResult = violetsCollection.UpdateOne(filter, updateDefinition);

        return updateResult.ModifiedCount > 0;
    }

    public List<Violet> GetAllViolets()
    {
        var violetsCollection = _database.GetCollection<Violet>("Violets");
        return violetsCollection.Find(Builders<Violet>.Filter.Empty).ToList();
    }

    public Violet GetVioletById(Guid id)
    {
        var violetsCollection = _database.GetCollection<Violet>("Violets");
        var filter = Builders<Violet>.Filter.Eq("Id", id);
        return violetsCollection.Find(filter).FirstOrDefault();
    }

    public bool DeleteViolet(Guid id)
    {
        var violetsCollection = _database.GetCollection<Violet>("Violets");
        var filter = Builders<Violet>.Filter.Eq(v => v.Id, id);
    
        var deleteResult = violetsCollection.DeleteOne(filter);

        return deleteResult.DeletedCount > 0;
    }
}