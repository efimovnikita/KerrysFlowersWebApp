using SharedLibrary;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace KFTelegramBot.Model;

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