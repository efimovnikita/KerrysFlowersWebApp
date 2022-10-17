using System.Text.Json;
using SharedLibrary;
using Telegram.Bot.Types;

namespace TelegramBotConsole;

internal interface IUnit
{
    Task Question(ChatId id);
    (bool, string) Validate(Update update);
    Task<(bool, string)> RunAction(Violet violet, Update update);
    void PrintViolet(Violet violet)
    {
        Console.WriteLine(JsonSerializer.Serialize(violet));
        Console.WriteLine();
    }
}