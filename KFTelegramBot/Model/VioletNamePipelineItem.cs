using SharedLibrary;
using SharedLibrary.Providers;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace KFTelegramBot.Model;

public class VioletNamePipelineItem(IVioletRepository violetRepository) : IPipelineItem
{
    public Task<Message> AskAQuestion(Message message, ITelegramBotClient botClient) => 
        botClient.SendTextMessageAsync(message.Chat.Id, "Введите имя новой фиалки");

    public (bool, Task<Message>?) ValidateInput(Message message, ITelegramBotClient botClient)
    {
        if (message.Type != MessageType.Text)
        {
            return (false,
                botClient.SendTextMessageAsync(message.Chat.Id, "Ожидалось текстовое сообщение. Повторите ввод."));
        }

        var names = violetRepository.GetAllViolets()
            .Select(violet => violet.Name.ToLowerInvariant())
            .ToArray();

        if (names.Contains(message.Text!.ToLowerInvariant()))
        {
            return (false,
                botClient.SendTextMessageAsync(message.Chat.Id, "Фиалка с таким именем уже существует. Повторите ввод."));
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

        violet.Name = message.Text!.Trim();
        return (true, null);
    }
}