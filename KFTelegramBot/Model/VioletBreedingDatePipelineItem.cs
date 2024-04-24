using System.Globalization;
using SharedLibrary;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace KFTelegramBot.Model;

public class VioletBreedingDatePipelineItem : IPipelineItem
{
    private const string DateFormat = "yyyy";

    public Task<Message> AskAQuestion(Message message, ITelegramBotClient botClient) =>
        botClient.SendTextMessageAsync(message.Chat.Id, $"Введите год селекции фиалки (в формате \"{DateFormat}\")");

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

        violet.BreedingDate = dateTime + TimeSpan.FromDays(1);
        return (true, null);
    }
}