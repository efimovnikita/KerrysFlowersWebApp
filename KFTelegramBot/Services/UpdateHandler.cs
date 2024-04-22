using KFTelegramBot.Model;
using KFTelegramBot.Providers;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.ReplyMarkups;

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
        if (allViolets.Count == 0)
        {
            return await botClient.SendTextMessageAsync(message.Chat.Id,
                "Список пуст.",
                cancellationToken: cancellationToken);
        }

        foreach (var violet in allViolets)
        {
            await botClient.SendTextMessageAsync(message.Chat.Id,
                $"{violet.Id}\n{violet.Name}",
                cancellationToken: cancellationToken);
        }

        return await botClient.SendTextMessageAsync(message.Chat.Id,
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

    private async Task<Message> ResetCommand(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        _memoryStateProvider.ResetCurrentPipeline(message.Chat.Id);
        return await botClient.SendTextMessageAsync(message.Chat.Id,
            "Текущая операция отменена.",
            cancellationToken: cancellationToken);
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