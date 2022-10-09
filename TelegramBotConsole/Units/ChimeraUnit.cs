using SharedLibrary;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

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
        ReplyKeyboardMarkup replyKeyboardMarkup = new(new []
        {
            new KeyboardButton[] { "Да", "Нет" },
        })
        {
            ResizeKeyboard = true
        };
        await _client.SendTextMessageAsync(chatId, "Является ли фиалка химерой?", replyMarkup: replyKeyboardMarkup);
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

    public async Task<(bool, string)> RunAction(Violet violet, Message message)
    {
        string text = message.Text!.ToLower().Trim();
        if (text.ToLower() == "да")
        {
            violet.IsChimera = true;
            await RemoveKeyBoard();

            return (true, "");
        }

        violet.IsChimera = false;
        await RemoveKeyBoard();

        return (true, "");

        async Task RemoveKeyBoard() => await _client.SendTextMessageAsync(message.Chat.Id, $"Выбран ответ \"{text}\"", replyMarkup: new ReplyKeyboardRemove());
    }
}