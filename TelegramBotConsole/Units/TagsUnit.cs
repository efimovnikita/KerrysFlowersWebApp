using SharedLibrary;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TelegramBotConsole.Units;

internal class TagsUnit : IUnit
{
    private readonly ITelegramBotClient _client;

    public TagsUnit(ITelegramBotClient client)
    {
        _client = client;
    }

    public async Task Question(ChatId id)
    {
        await _client.SendTextMessageAsync(id, "Введите список тэгов (через запятую)");
    }
    
    public (bool, string) Validate(Update update)
    {
        if (update.Message!.Type != MessageType.Text)
        {
            return (false, "Ожидалось текстовое сообщение. Повторите ввод списока тэгов.");
        }

        string text = update.Message.Text!.ToLower();
        
        if (text.Split(',').All(String.IsNullOrEmpty))
        {
            return (false, "Список тегов пуст. Повторите ввод списка тэгов.");
        }

        return (true, "");
    }

    public Task<(bool, string)> RunAction(Violet violet, Update update)
    {
        string text = update.Message!.Text!.ToLower().Trim();
        List<string> tags = text.Split(',')
            .Where(tag => String.IsNullOrEmpty(tag) == false)
            .Select(tag => tag.Trim())
            .ToList();

        violet.Tags = tags;

        return Task.FromResult((true, ""));
    }
}