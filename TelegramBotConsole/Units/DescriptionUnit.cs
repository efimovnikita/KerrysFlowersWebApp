using SharedLibrary;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TelegramBotConsole.Units;

internal class DescriptionUnit : IUnit
{
    private readonly ITelegramBotClient _client;

    public DescriptionUnit(ITelegramBotClient client)
    {
        _client = client;
    }

    public async Task Question(ChatId chatId)
    {
        await _client.SendTextMessageAsync(chatId, "Введите описание новой фиалки");
    }
    
    public (bool, string) Validate(Message message)
    {
        if (message.Type != MessageType.Text)
        {
            return (false, "Ожидалось текстовое сообщение. Повторите ввод описания.");
        }
        
        return message.Text!.Length > 1 ? (true, "") : (false, "Описание должно содержать более 1 символа. Повторите ввод описания.");
    }

    public Task<(bool, string)> RunAction(Violet violet, Message message)
    {
        violet.Description = message.Text;
        return Task.FromResult((true, ""));
    }
}