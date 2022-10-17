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

    public async Task Question(ChatId id)
    {
        await _client.SendTextMessageAsync(id, "Введите имя селекционера фиалки");
    }
    
    public (bool, string) Validate(Update update)
    {
        if (update.Message!.Type != MessageType.Text)
        {
            return (false, "Ожидалось текстовое сообщение. Повторите ввод имени селекционера.");
        }
        
        return update.Message.Text!.Length > 1 ? (true, "") : (false, "Именя селекционера должно содержать более 1 символа. Повторите ввод имени.");
    }

    public Task<(bool, string)> RunAction(Violet violet, Update update)
    {
        violet.Breeder = update.Message!.Text;
        return Task.FromResult((true, ""));
    }
}