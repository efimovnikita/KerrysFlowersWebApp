using SharedLibrary;
using SharedLibrary.Providers;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace KFTelegramBot.Model;

public class SetWarehousePipeline(IPipelineItem[] items, IVioletRepository violetRepository) : IPipeline
{
    private List<WarehouseVioletItem> WarehouseVioletItems { get; } = [];
    private Queue<IPipelineItem> PipelineItems { get; } = new(items);

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

        var (jobResult, jobMsg) = pipelineItem.DoTheJob(message, WarehouseVioletItems, botClient);
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
        
        if (WarehouseVioletItems.Count == 0)
        {
            return botClient.SendTextMessageAsync(message.Chat.Id,
                "Не удалось добавить данные о складских остатках и ценах...");
        }

        var clearAllWarehouseVioletItems = violetRepository.ClearAllWarehouseVioletItems();
        if (clearAllWarehouseVioletItems == false)
        {
            return botClient.SendTextMessageAsync(message.Chat.Id,
                "Не удалось удалить предыдущие данные о складских остатках и ценах...");
        }

        var sentResult = violetRepository.InsertWarehouseVioletItems(WarehouseVioletItems);

        return botClient.SendTextMessageAsync(message.Chat.Id,
            sentResult
                ? "Данные о складких остатках и ценах добавлены в базу данных."
                : "Не удалось добавить данные в базу данных.",
            disableWebPagePreview: true);
    }

    public bool IsPipelineQueueEmpty() => PipelineItems.Count == 0;
}