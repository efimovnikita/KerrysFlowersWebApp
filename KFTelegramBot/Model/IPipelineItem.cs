using Telegram.Bot;
using Telegram.Bot.Types;

namespace KFTelegramBot.Model;

public interface IPipelineItem
{
    Task<Message> AskAQuestion(Message message, ITelegramBotClient botClient);
    (bool, Task<Message>?) ValidateInput(Message message, ITelegramBotClient botClient);
    (bool, Task<Message>?) DoTheJob(Message message, object affectedObject, ITelegramBotClient botClient);
}