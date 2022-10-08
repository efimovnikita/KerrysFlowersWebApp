using System.Text.Json;
using SharedLibrary;
using Telegram.Bot.Types;

namespace TelegramBotConsole;

internal interface IUnit
{
    Task Question(ChatId chatId);
    (bool,string) Validate(Message message);
    Task<(bool, string)> RunAction(Violet violet, Message message);
    void PrintViolet(Violet violet)
    {
        Console.WriteLine(JsonSerializer.Serialize(violet));
        Console.WriteLine();
    }
}