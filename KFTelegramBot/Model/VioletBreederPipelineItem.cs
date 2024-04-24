using SharedLibrary;
using SharedLibrary.Providers;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace KFTelegramBot.Model;

public class VioletBreederPipelineItem(IVioletRepository violetRepository) : IPipelineItem
{
    public Task<Message> AskAQuestion(Message message, ITelegramBotClient botClient)
    {
        var breeders = violetRepository.GetAllViolets()
            .Select(violet => violet.Breeder)
            .Distinct()
            .Select(breeder => $"`{breeder}`")
            .ToArray();

        if (breeders.Length == 0)
        {
            return botClient.SendTextMessageAsync(message.Chat.Id, "Введите имя селекционера фиалки");
        }

        var joinedBreeders = string.Join(", ", breeders);
        return botClient.SendTextMessageAsync(message.Chat.Id,
            $"Введите имя селекционера фиалки.\nРанее введенные имена:\n{joinedBreeders}",
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

        violet.Breeder = message.Text!.Trim();
        return (true, null);
    }
}