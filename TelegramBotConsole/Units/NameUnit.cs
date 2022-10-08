using SharedLibrary;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TelegramBotConsole.Units;

internal class NameUnit : IUnit
{
    private readonly ITelegramBotClient _client;

    public NameUnit(ITelegramBotClient client)
    {
        _client = client;
    }

    public async Task Question(ChatId chatId)
    {
        await _client.SendTextMessageAsync(chatId, "Введите имя новой фиалки");
    }

    public (bool, string) Validate(Message message)
    {
        if (message.Type != MessageType.Text)
        {
            return (false, "Ожидалось текстовое сообщение. Повторите ввод имени.");
        }
        
        return message.Text!.Length > 1 ? (true, "") : (false, "Имя должно содержать более 1 символа. Повторите ввод имени.");
    }

    public Task<(bool, string)> RunAction(Violet violet, Message message)
    {
        violet.Name = message.Text;
        return Task.FromResult((true, ""));
    }
}