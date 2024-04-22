using KFTelegramBot.Providers;
using SharedLibrary;
using SharedLibrary.Providers;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace KFTelegramBot.Model;

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