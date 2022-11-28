using SharedLibrary;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TelegramBotConsole.Units;

internal class SecondImageUnit : IUnit
{
    private readonly ITelegramBotClient _client;

    public SecondImageUnit(ITelegramBotClient client)
    {
        _client = client;
    }

    public Task Question(ChatId id)
    {
        return Task.CompletedTask;
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