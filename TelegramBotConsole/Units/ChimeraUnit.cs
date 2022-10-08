using SharedLibrary;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TelegramBotConsole.Units;

internal class ChimeraUnit : IUnit
{
    private readonly ITelegramBotClient _client;

    public ChimeraUnit(ITelegramBotClient client)
    {
        _client = client;
    }

    public async Task Question(ChatId chatId)
    {
        await _client.SendTextMessageAsync(chatId, "Является ли фиалка химерой (\'да\' или \'нет\')?");
    }

    public (bool, string) Validate(Message message)
    {
        if (message.Type != MessageType.Text)
        {
            return (false, "Ожидалось текстовое сообщение. Повторите ввод признака химеры.");
        }

        if (new[]{ "да", "нет" }.Contains(message.Text!.ToLower().Trim()) == false)
        {
            return (false, "Ожидался ответ который содержит либо \"да\" либо \"нет\". Повторите признака химеры.");
        }

        return (true, "");
    }

    public Task<(bool, string)> RunAction(Violet violet, Message message)
    {
        string text = message.Text!.ToLower().Trim();
        if (text.ToLower() == "да")
        {
            violet.IsChimera = true;
            return Task.FromResult((true, ""));
        }

        violet.IsChimera = false;
        return Task.FromResult((true, ""));
    }
}