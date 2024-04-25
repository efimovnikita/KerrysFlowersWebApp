using KFTelegramBot.Providers;
using SharedLibrary;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace KFTelegramBot.Model;

public class VioletDescriptionPipelineItem(IRecommendationsProvider recommendationsProvider) : IPipelineItem
{
    public async Task<Message> AskAQuestion(Message message, ITelegramBotClient botClient, 
        object? violetObject = null)
    {
        const string msg = "Введите описание фиалки";
        var chatId = message.Chat.Id;
        const string aiRecommendationMsg = "_Рекомендация от ИИ:_";
        var question = await botClient.SendTextMessageAsync(chatId, $"{msg}\n\n{aiRecommendationMsg} \u23f1\ufe0f", parseMode: ParseMode.Markdown);

        _ = Task.Run(async () =>
        {
            if (violetObject is Violet violet)
            {
                var recommendation = await recommendationsProvider.GetDescriptionRecommendation($"Фиалка+{violet.Name}+{violet.Breeder}");
                if (string.IsNullOrEmpty(recommendation) == false)
                {
                    await botClient.EditMessageTextAsync(chatId, question.MessageId, $"{msg}\n\n{aiRecommendationMsg}\n`{recommendation}`",
                        parseMode: ParseMode.Markdown);
                }
                else
                {
                    await botClient.EditMessageTextAsync(chatId, question.MessageId, msg, parseMode: ParseMode.Markdown);
                }
            }
        });

        return question;
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

        violet.Description = message.Text!.Trim();
        return (true, null);
    }
}