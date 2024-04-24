using SharedLibrary;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace KFTelegramBot.Model;

public class VioletChimeraPipelineItem : IPipelineItem
{
    private const string AnswerFormatMsg = "Дайте ответ в формате \"да\", \"yes\", \"true\" или \"нет\", \"no\", \"false\".";

    private static readonly HashSet<string> PositiveAnswers = ["да", "yes", "true"];
    private static readonly HashSet<string> NegativeAnswers = ["нет", "no", "false"];

    public Task<Message> AskAQuestion(Message message, ITelegramBotClient botClient) =>
        botClient.SendTextMessageAsync(message.Chat.Id, $"Фиалка химера? {AnswerFormatMsg}");

    public (bool, Task<Message>?) ValidateInput(Message message, ITelegramBotClient botClient)
    {
        if (message.Type != MessageType.Text)
        {
            return (false,
                botClient.SendTextMessageAsync(message.Chat.Id, "Ожидалось текстовое сообщение. Повторите ввод."));
        }

        var text = message.Text!.Trim().ToLower();

        if (!PositiveAnswers.Contains(text) && !NegativeAnswers.Contains(text))
        {
            return (false,
                botClient.SendTextMessageAsync(message.Chat.Id,
                    $"{AnswerFormatMsg} Повторите ввод."));
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

        if (PositiveAnswers.Contains(text) == false)
        {
            violet.IsChimera = false;
            return (true, null);
        }

        violet.IsChimera = true;
        return (true, null);
    }
}