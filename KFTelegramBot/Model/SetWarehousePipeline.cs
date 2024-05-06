using SharedLibrary;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace KFTelegramBot.Model;

public class SetWarehousePipeline(IPipelineItem[] items) : IPipeline
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

        // TODO
        // queue is empty
        //var sentResult = SendDataToTheDatabase(GrowingViolet, botClient, message.Chat.Id);
        var sentResult = false;

        return botClient.SendTextMessageAsync(message.Chat.Id,
            sentResult
                ? "Данные о складких остатках и ценах добавлены в базу данных."
                : "Не удалось добавить данные в базу данных.",
            disableWebPagePreview: true);
    }

    public bool IsPipelineQueueEmpty() => PipelineItems.Count == 0;
}