using SharedLibrary;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TelegramBotConsole.Units;

internal class FirstImageUnit : IUnit
{
    private readonly ITelegramBotClient _client;

    public FirstImageUnit(ITelegramBotClient client)
    {
        _client = client;
    }

    public async Task Question(ChatId id)
    {
        await _client.SendTextMessageAsync(id, "Отправьте три фотографии новой фиалки");
    }
    
    public (bool, string) Validate(Update update)
    {
        return CommonValidatorsAndActions.ValidateDocument(update);
    }


    public async Task<(bool, string)> RunAction(Violet violet, Update update)
    {
        return await CommonValidatorsAndActions.ProcessDocument(_client, violet, update);
    }
}