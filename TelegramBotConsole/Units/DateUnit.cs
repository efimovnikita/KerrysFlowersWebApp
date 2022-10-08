using SharedLibrary;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TelegramBotConsole.Units;

internal class DateUnit : IUnit
{
    private readonly ITelegramBotClient _client;

    public DateUnit(ITelegramBotClient client)
    {
        _client = client;
    }

    public async Task Question(ChatId chatId)
    {
        await _client.SendTextMessageAsync(chatId, "Введите год селекции фиалки (например: 2022)");
    }

    public (bool, string) Validate(Message message)
    {
        if (message.Type != MessageType.Text)
        {
            return (false, "Ожидалось текстовое сообщение. Повторите ввод года селекции.");
        }

        string text = message.Text!.Trim();
        if (text.Length != 4)
        {
            return (false, "Введите год (четыре цифры)");
        }
        
        bool parseNumberResult = Int32.TryParse(text, out int _);
        if (parseNumberResult == false)
        {
            return (false, "Введите год (четыре цифры)");
        }

        return (true, "");
    }

    public Task<(bool, string)> RunAction(Violet violet, Message message)
    {
        string text = message.Text!.Trim();
        Int32.TryParse(text, out int parsedNumber);
        violet.BreedingDate = new DateTime(parsedNumber, 1, 1);
        return Task.FromResult((true, ""));
    }
}