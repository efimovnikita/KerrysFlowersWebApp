using System.Text.Json;
using KFTelegramBot.Model;
using KFTelegramBot.Providers;
using Microsoft.Extensions.Logging;
using NPOI.HSSF.UserModel;
using SharedLibrary.Providers;
using SharedLibrary;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.ReplyMarkups;
using File = System.IO.File;

namespace KFTelegramBot.Services;

public class UpdateHandler : IUpdateHandler
{
    private const string DeleteVioletButtonText = "Удалить фиалку";
    private const string EditVioletPhotoButtonText = "Заменить фотографии";
    private readonly ITelegramBotClient _botClient;
    private readonly ILogger<UpdateHandler> _logger;
    private readonly IMemoryStateProvider _memoryStateProvider;
    private readonly IVioletRepository _violetRepository;
    private readonly IRecommendationsProvider _recommendationsProvider;

    public UpdateHandler(ITelegramBotClient botClient, ILogger<UpdateHandler> logger,
        IMemoryStateProvider memoryStateProvider, IVioletRepository violetRepository,
        IRecommendationsProvider recommendationsProvider)
    {
        _botClient = botClient;
        _logger = logger;
        _memoryStateProvider = memoryStateProvider;
        _violetRepository = violetRepository;
        _recommendationsProvider = recommendationsProvider;
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
            _                                              => UnknownUpdateHandlerAsync(update)
        };

        await handler;
    }

    private async Task BotOnMessageReceived(Message message, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Receive message type: {MessageType}", message.Type);
        if (message.Type == MessageType.Document)
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
            "/start"           => AddCommand(_botClient, message),
            "/add"             => AddCommand(_botClient, message),
            "/delete"          => DeleteCommand(_botClient, message,cancellationToken),
            "/reset"           => ResetCommand(_botClient, message,cancellationToken),
            "/list"            => ListCommand(_botClient, message,cancellationToken),
            "/usage"           => UsageCommand(_botClient, message,cancellationToken),
            "/site"            => SiteCommand(_botClient, message,cancellationToken),
            "/backup"          => BackupCommand(_botClient, message,cancellationToken),
            "/edit_photos"     => EditPhotosCommand(_botClient, message,cancellationToken),
            "/get_warehouse"   => GetWarehouseCommand(_botClient, message,cancellationToken),
            "/set_warehouse"   => SetWarehouseCommand(_botClient, message),
            _                  => ProcessCommand(_botClient, message, cancellationToken)
        };

        Message sentMessage = await action;
        _logger.LogInformation("The message was sent with userId: {SentMessageId}", sentMessage.MessageId);
    }

    private Task<Message> SetWarehouseCommand(ITelegramBotClient botClient, Message message)
    {
        var violetAddingPipeline = new SetWarehousePipeline([
            new DigestWarehouseXlsPipelineItem()
        ],
        _violetRepository);

        _memoryStateProvider.SetCurrentPipeline(violetAddingPipeline, message.Chat.Id);
        return violetAddingPipeline.AskAQuestionForNextItem(message, botClient);
    }

    private async Task<Message> GetWarehouseCommand(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        var allViolets = _violetRepository.GetAllViolets();
        if (allViolets.Count == 0)
        {
            return await botClient.SendTextMessageAsync(message.Chat.Id,
                "Список пуст.",
                cancellationToken: cancellationToken);
        }

        // Create a new workbook and a sheet
        var workbook = new HSSFWorkbook();
        var sheet = workbook.CreateSheet("Sheet1");

        // Create a bold font
        var boldFont = (HSSFFont)workbook.CreateFont();
        boldFont.Boldweight = (short)NPOI.SS.UserModel.FontBoldWeight.Bold;

        // Create a cell style with the bold font
        var boldStyle = (HSSFCellStyle)workbook.CreateCellStyle();
        boldStyle.SetFont(boldFont);

        // Create a header row
        var headerRow = sheet.CreateRow(0);
        
        var cell0 = headerRow.CreateCell(0);
        cell0.SetCellValue("Id");
        cell0.CellStyle = boldStyle;
        
        var cell1 = headerRow.CreateCell(1);
        cell1.SetCellValue("Название");
        cell1.CellStyle = boldStyle;

        var cell2 = headerRow.CreateCell(2);
        cell2.SetCellValue("Лист (кол-во)");
        cell2.CellStyle = boldStyle;

        var cell3 = headerRow.CreateCell(3);
        cell3.SetCellValue("Лист (цена)");
        cell3.CellStyle = boldStyle;
        
        var cell4 = headerRow.CreateCell(4);
        cell4.SetCellValue("Пасынок/детка (кол-во)");
        cell4.CellStyle = boldStyle;

        var cell5 = headerRow.CreateCell(5);
        cell5.SetCellValue("Пасынок/детка (цена)");
        cell5.CellStyle = boldStyle;

        var cell6 = headerRow.CreateCell(6);
        cell6.SetCellValue("Целое растение (кол-во)");
        cell6.CellStyle = boldStyle;

        var cell7 = headerRow.CreateCell(7);
        cell7.SetCellValue("Целое растение (цена)");
        cell7.CellStyle = boldStyle;
        
        sheet.SetColumnHidden(0, true);

        var violets = allViolets.OrderBy(violet => violet.PublishDate).ToArray();
        
        for (var i = 0; i < violets.Length; i++)
        {
            var violet = violets[i];
            var row = sheet.CreateRow(i + 1);
            row.CreateCell(0).SetCellValue(violet.Id.ToString());
            row.CreateCell(1).SetCellValue(violet.Name);
            row.CreateCell(2).SetCellValue(0);
            row.CreateCell(3).SetCellValue(0);
            row.CreateCell(4).SetCellValue(0);
            row.CreateCell(5).SetCellValue(0);
            row.CreateCell(6).SetCellValue(0);   
            row.CreateCell(7).SetCellValue(0);   
        }

        // Auto-size columns
        for (var columnIndex = 0; columnIndex < 6; columnIndex++)
        {
            sheet.AutoSizeColumn(columnIndex);
        }

        // Create a new file name with a .xls extension
        var tempXlsFilePath = Path.Combine(Path.GetTempPath(), 
            "Складские остатки и цены.xls");

        // If a file with the .xls extension already exists (rare case), delete it
        if (File.Exists(tempXlsFilePath))
        {
            File.Delete(tempXlsFilePath);
        }

        // Now save the Excel workbook in the newly named .xls file
        using (var createStream = new FileStream(tempXlsFilePath, FileMode.OpenOrCreate, FileAccess.Write))
        {
            workbook.Write(createStream);
        }
        
        // Create a FileStream to your text file
        await using FileStream fileStream = new(tempXlsFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        // Create a new InputOnlineFile from the FileStream
        InputFileStream inputFile = new(fileStream, Path.GetFileName(tempXlsFilePath));
        
        return await botClient.SendDocumentAsync(
            chatId: message.Chat.Id,
            document: inputFile,
            cancellationToken: cancellationToken);
    }

    private async Task<Message> EditPhotosCommand(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        var strings = message.Text!.Split(' ');
        if (strings.Length < 2)
        {
            return await botClient.SendTextMessageAsync(message.Chat.Id,
                "Формат команды: `/edit_photos <GUID>`. Попробуйте снова.",
                parseMode: ParseMode.Markdown,
                cancellationToken: cancellationToken);
        }

        var guidStr = strings[1];
        if (string.IsNullOrWhiteSpace(guidStr))
        {
            return await botClient.SendTextMessageAsync(message.Chat.Id,
                "Формат команды: `/edit_photos <GUID>`. Попробуйте снова.",
                cancellationToken: cancellationToken);
        }

        var parseStatus = Guid.TryParse(guidStr, out var guid);
        if (parseStatus == false)
        {
            return await botClient.SendTextMessageAsync(message.Chat.Id,
                "Не верный формат идентификатора. Попробуйте снова.",
                cancellationToken: cancellationToken);
        }

        var violet = _violetRepository.GetVioletById(guid);
        if (violet == null)
        {
            return await botClient.SendTextMessageAsync(message.Chat.Id,
                "Фиалки с таким id не найдено. Попробуйте снова.",
                cancellationToken: cancellationToken);
        }

        var violetPhotoEditingPipeline = new VioletPhotoEditingPipeline(violet, [
            new VioletDocumentsPipelineItem()
        ], _violetRepository);

        _memoryStateProvider.SetCurrentPipeline(violetPhotoEditingPipeline, message.Chat.Id);
        return await violetPhotoEditingPipeline.AskAQuestionForNextItem(message, botClient);
    }

    private async Task<Message> BackupCommand(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        var allViolets = _violetRepository.GetAllViolets();
        if (allViolets.Count == 0)
        {
            return await botClient.SendTextMessageAsync(message.Chat.Id,
                "Список пуст.",
                cancellationToken: cancellationToken);
        }

        var violets = allViolets.OrderBy(violet => violet.PublishDate).ToArray();

        var path = SerializeVioletsToJsonFile(violets);

        await using FileStream fileStream = new(path, FileMode.Open, FileAccess.Read, FileShare.Read);

        InputFileStream inputFile = new(fileStream, Path.GetFileName(path));

        return await botClient.SendDocumentAsync(message.Chat.Id, inputFile, caption: "Ваш бэкап", cancellationToken: cancellationToken);
    }

    public static string SerializeVioletsToJsonFile(Violet[] violets)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        var jsonString = JsonSerializer.Serialize(violets, options);

        var tempDirectory = Path.GetTempPath();
        const string fileName = "Violets.json";
        var filePath = Path.Combine(tempDirectory, fileName);

        File.WriteAllText(filePath, jsonString);

        return filePath;
    }

    private async Task<Message> SiteCommand(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken) =>
        await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "https://kerrisflowers.ru",
            cancellationToken: cancellationToken);

    private async Task<Message> ListCommand(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        var allViolets = _violetRepository.GetAllViolets();
        if (allViolets.Count == 0)
        {
            return await botClient.SendTextMessageAsync(message.Chat.Id,
                "Список пуст.",
                cancellationToken: cancellationToken);
        }

        var violets = allViolets.OrderBy(violet => violet.PublishDate).ToArray();
        foreach (var violet in violets.Take(violets.Length - 1))
        {
            var inlineKeyboard = GetInlineKeyboardMarkupForViolet(violet);

            if (violet.Images.Count != 0)
            {
                await botClient.SendPhotoAsync(message.Chat.Id,
                    InputFile.FromStream(await GetImageStreamFromBase64(violet)),
                    caption: GetCaption(violet),
                    replyMarkup: inlineKeyboard,
                    parseMode: ParseMode.Markdown,
                    cancellationToken: cancellationToken);
            }
            else
            {
                await botClient.SendTextMessageAsync(message.Chat.Id,
                    GetCaption(violet),
                    replyMarkup: inlineKeyboard,
                    parseMode: ParseMode.Markdown,
                    cancellationToken: cancellationToken);
            }
        }

        var lastViolet = violets.Last();

        var inlineKeyboardForLastViolet = GetInlineKeyboardMarkupForViolet(lastViolet);

        if (lastViolet.Images.Count != 0)
        {
            return await botClient.SendPhotoAsync(message.Chat.Id,
                InputFile.FromStream(await GetImageStreamFromBase64(lastViolet)),
                caption: GetCaption(lastViolet),
                replyMarkup: inlineKeyboardForLastViolet,
                parseMode: ParseMode.Markdown,
                cancellationToken: cancellationToken);
        }

        return await botClient.SendTextMessageAsync(message.Chat.Id,
            GetCaption(lastViolet),
            replyMarkup: inlineKeyboardForLastViolet,
            parseMode: ParseMode.Markdown,
            cancellationToken: cancellationToken);

        string GetCaption(Violet violet) =>
            violet.ToString("M");

        async Task<FileStream> GetImageStreamFromBase64(Violet violet)
        {
            FileStream? imageStream = null;
            try
            {
                var imagePath = ConvertBase64StringToJpgFile(violet.Images.First().W330);
                imageStream = File.OpenRead(imagePath);
                return imageStream;
            }
            catch
            {
                if (imageStream != null) await imageStream.DisposeAsync();
                throw;
            }
        }
    }

    private static InlineKeyboardMarkup GetInlineKeyboardMarkupForViolet(Violet violet)
    {
        var deleteButton = new InlineKeyboardButton(DeleteVioletButtonText)
            { CallbackData = $"/delete {violet.Id}" };
        var editPhotosButton = new InlineKeyboardButton(EditVioletPhotoButtonText)
            { CallbackData = $"/edit_photos {violet.Id}" };
        var buttons = new[] { editPhotosButton, deleteButton }
            .Select(button => (InlineKeyboardButton[]) [button])
            .ToArray();
        InlineKeyboardMarkup inlineKeyboard = new(buttons);
        return inlineKeyboard;
    }

    public static string ConvertBase64StringToJpgFile(string base64String)
    {
        var tempFilePath = Path.GetTempFileName();
        var jpgFilePath = Path.ChangeExtension(tempFilePath, ".jpg");

        if (File.Exists(jpgFilePath))
        {
            File.Delete(jpgFilePath);
        }

        var fileBytes = Convert.FromBase64String(base64String);

        File.WriteAllBytes(jpgFilePath, fileBytes);

        return jpgFilePath;
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

        var msg = await pipeline.ProcessCurrentItem(message, botClient);

        if (pipeline.IsPipelineQueueEmpty())
        {
            _memoryStateProvider.ResetCurrentPipeline(chatId);
        }

        return msg;
    }

    private async Task<Message> ResetCommand(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        _memoryStateProvider.ResetCurrentPipeline(message.Chat.Id);
        return await botClient.SendTextMessageAsync(message.Chat.Id,
            "Текущая операция отменена.",
            cancellationToken: cancellationToken);
    }

    private static async Task<Message> UsageCommand(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken) =>
        await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: GetUsageText(),
            cancellationToken: cancellationToken);

    private static string GetUsageText() =>
        """
        Список команд:
        
        /start - добавить фиалку
        /add - добавить фиалку
        /edit_photos - заменить фотографии
        /delete - удалить фиалку
        /reset - отменить текущую команду
        /list - показать список фиалок
        /usage - показать список команд
        /site - показать ссылку на сайт
        """;

    private async Task<Message> DeleteCommand(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        var strings = message.Text!.Split(' ');
        if (strings.Length < 2)
        {
            return await botClient.SendTextMessageAsync(message.Chat.Id,
                "Формат команды: `/delete <GUID>`. Попробуйте снова.",
                parseMode: ParseMode.Markdown,
                cancellationToken: cancellationToken);
        }

        var guidStr = strings[1];
        if (string.IsNullOrWhiteSpace(guidStr))
        {
            return await botClient.SendTextMessageAsync(message.Chat.Id,
                "Формат команды: `/delete <GUID>`. Попробуйте снова.",
                cancellationToken: cancellationToken);
        }

        var parseStatus = Guid.TryParse(guidStr, out var guid);
        if (parseStatus == false)
        {
            return await botClient.SendTextMessageAsync(message.Chat.Id,
                "Не верный формат идентификатора. Попробуйте снова.",
                cancellationToken: cancellationToken);
        }

        var deleteVioletStatus = _violetRepository.DeleteViolet(guid);
        if (deleteVioletStatus)
        {
            return await botClient.SendTextMessageAsync(message.Chat.Id,
                "Фиалка успешно удалена.",
                cancellationToken: cancellationToken);
        }

        return await botClient.SendTextMessageAsync(message.Chat.Id,
            "Не удалось удалить фиалку.",
            cancellationToken: cancellationToken);
    }

    private Task<Message> AddCommand(ITelegramBotClient botClient, Message message)
    {
        var violetAddingPipeline = new VioletAddingPipeline([
            new VioletNamePipelineItem(_violetRepository),
            new VioletBreederPipelineItem(_violetRepository),
            new VioletDescriptionPipelineItem(_recommendationsProvider),
            new VioletTagsPipelineItem(_violetRepository),
            new VioletBreedingDatePipelineItem(),
            new VioletChimeraPipelineItem(),
            new VioletDocumentsPipelineItem(),
            new VioletColorsPipelineItem(_recommendationsProvider),
            new VioletSizePipelineItem(),
        ], _violetRepository);

        _memoryStateProvider.SetCurrentPipeline(violetAddingPipeline, message.Chat.Id);
        return violetAddingPipeline.AskAQuestionForNextItem(message, botClient);
    }

    private async Task BotOnCallbackQueryReceived(CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received inline keyboard callback from: {CallbackQueryId}", callbackQuery.Id);

        var data = callbackQuery.Data;
        if (string.IsNullOrWhiteSpace(data))
        {
            await _botClient.SendTextMessageAsync(callbackQuery.Message!.Chat.Id, "Callback query data is empty.",
                cancellationToken: cancellationToken);
            return;
        }

        callbackQuery.Message!.Text = data;

        await _botClient.AnswerCallbackQueryAsync(
            callbackQueryId: callbackQuery.Id,
            text: $"Received {callbackQuery.Data}",
            cancellationToken: cancellationToken);

        var inlineKeyboardButtons = callbackQuery.Message.ReplyMarkup!.InlineKeyboard
            .Select(buttons => buttons.First())
            .ToArray();

        var button = inlineKeyboardButtons.FirstOrDefault(b => b.CallbackData!.Equals(data, StringComparison.OrdinalIgnoreCase));

        if (button != null && button.Text.Contains(DeleteVioletButtonText, StringComparison.OrdinalIgnoreCase))
        {
            await DeleteCommand(_botClient, callbackQuery.Message, cancellationToken);
        }
        else
        {
            await EditPhotosCommand(_botClient, callbackQuery.Message, cancellationToken);
        }
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
    private Task UnknownUpdateHandlerAsync(Update update)
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