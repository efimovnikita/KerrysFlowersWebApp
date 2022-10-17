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

    public async Task Question(ChatId id)
    {
        InlineKeyboardMarkup inlineKeyboard = new(new []
        {
            new []
            {
                InlineKeyboardButton.WithCallbackData(text: "Да", callbackData: "да"),
                InlineKeyboardButton.WithCallbackData(text: "Нет", callbackData: "нет"),
            }
        });
        
        await _client.SendTextMessageAsync(id, "Является ли фиалка химерой?", replyMarkup: inlineKeyboard);
    }

    public (bool, string) Validate(Update update)
    {
        if (update.Type != UpdateType.CallbackQuery)
        {
            return (false, "Нажмите на одну из двух кнопок (да или нет).");
        }
        
        return (true, "");
    }

    public Task<(bool, string)> RunAction(Violet violet, Update update)
    {
        if (update.CallbackQuery!.Data == "да")
        {
            violet.IsChimera = true;
            return Task.FromResult((true, ""));
        }
        
        violet.IsChimera = false;
        return Task.FromResult((true, ""));
    }
}