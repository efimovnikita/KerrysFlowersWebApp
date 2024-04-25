using SharedLibrary;
using SharedLibrary.Providers;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace KFTelegramBot.Model;

public class VioletTagsPipelineItem(IVioletRepository violetRepository) : IPipelineItem
{
    public Task<Message> AskAQuestion(Message message, ITelegramBotClient botClient, 
        object? violetObject = null)
    {
        var tags = violetRepository.GetAllViolets()
            .SelectMany(violet => violet.Tags)
            .Distinct()
            .Select(tag => $"`{tag}`")
            .ToArray();
        
        if (tags.Length == 0)
        {
            return botClient.SendTextMessageAsync(message.Chat.Id, "Введите теги (через запятую) для новой фиалки");
        }

        var joinedTags = string.Join(", ", tags);
        return botClient.SendTextMessageAsync(message.Chat.Id,
            $"Введите теги (через запятую) для новой фиалки.\nРанее введенные теги:\n{joinedTags}",
            parseMode: ParseMode.Markdown);
    }

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