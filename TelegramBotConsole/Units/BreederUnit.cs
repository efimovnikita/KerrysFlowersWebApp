using SharedLibrary;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TelegramBotConsole.Units;

internal class BreederUnit : IUnit
{
    private readonly ITelegramBotClient _client;

    public BreederUnit(ITelegramBotClient client)
    {
        _client = client;
    }

    public async Task Question(ChatId chatId)
    {
        await _client.SendTextMessageAsync(chatId, "Введите имя селекционера фиалки");
    }
    
    public (bool, string) Validate(Message message)
    {
        if (message.Type != MessageType.Text)
        {
            return (false, "Ожидалось текстовое сообщение. Повторите ввод имени селекционера.");
        }
        
        return message.Text!.Length > 1 ? (true, "") : (false, "Именя селекционера должно содержать более 1 символа. Повторите ввод имени.");
    }

    public Task<(bool, string)> RunAction(Violet violet, Message message)
    {
        violet.Breeder = message.Text;
        return Task.FromResult((true, ""));
    }
}