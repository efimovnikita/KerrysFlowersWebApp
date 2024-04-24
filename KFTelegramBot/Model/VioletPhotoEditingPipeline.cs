using SharedLibrary;
using SharedLibrary.Providers;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace KFTelegramBot.Model;

public class VioletPhotoEditingPipeline : IPipeline
{
    private readonly Violet _editedViolet;
    private readonly Queue<IPipelineItem> _items;
    private readonly IVioletRepository _violetRepository;

    public VioletPhotoEditingPipeline(Violet editedViolet, IPipelineItem[] items, IVioletRepository violetRepository)
    {
        _editedViolet = editedViolet;
        _editedViolet.Images.Clear();

        _items = new Queue<IPipelineItem>(items);
        _violetRepository = violetRepository;
    }

    public Task<Message> AskAQuestionForNextItem(Message message, ITelegramBotClient botClient)
    {
        var pipelineItem = _items.Peek();
        return pipelineItem.AskAQuestion(message, botClient);
    }

    public Task<Message> ProcessCurrentItem(Message message, ITelegramBotClient botClient)
    {
        var pipelineItem = _items.Peek();
        var (validationResult, validationMsg) = pipelineItem.ValidateInput(message, botClient);
        if (validationResult == false)
        {
            return validationMsg!;
        }

        var (jobResult, jobMsg) = pipelineItem.DoTheJob(message, _editedViolet, botClient);
        if (jobResult == false)
        {
            return jobMsg!;
        }

        _items.Dequeue();

        var peekResult = _items.TryPeek(out var nextItem);
        if (peekResult)
        {
            return nextItem!.AskAQuestion(message, botClient);
        }

        // queue is empty
        var sentResult = SendUpdatedVioletToTheDatabase(_editedViolet, botClient, message.Chat.Id);

        return botClient.SendTextMessageAsync(message.Chat.Id,
            sentResult
                ? "Фотографии обновлены."
                : "Не удалось обновить фотографии в базе данных.",
            disableWebPagePreview: true);
    }

    private bool SendUpdatedVioletToTheDatabase(Violet violet, ITelegramBotClient botClient, long chatId)
    {
        try
        {
            _violetRepository.UpdateViolet(violet);
            return true;
        }
        catch (Exception e)
        {
            _ = botClient.SendTextMessageAsync(chatId, e.Message).Result;
            return false;
        }
    }

    public bool IsPipelineQueueEmpty() => _items.Count == 0;
}