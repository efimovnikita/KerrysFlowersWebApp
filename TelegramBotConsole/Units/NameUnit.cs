using System.Text.Json;
using SharedLibrary;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using File = System.IO.File;

namespace TelegramBotConsole.Units;

internal class NameUnit : IUnit
{
    private readonly ITelegramBotClient _client;
    private readonly FileInfo _root;

    public NameUnit(ITelegramBotClient client, FileInfo root)
    {
        _client = client;
        _root = root;
    }

    public async Task Question(ChatId chatId)
    {
        await _client.SendTextMessageAsync(chatId, "Введите имя новой фиалки");
    }

    public (bool, string) Validate(Message message)
    {
        if (message.Type != MessageType.Text)
        {
            return (false, "Ожидалось текстовое сообщение. Повторите ввод имени.");
        }

        if (message.Text!.Length <= 1)
        {
            return (false, "Имя должно содержать более 1 символа. Повторите ввод имени.");
        }

        string[] files = Directory.GetFiles(_root.FullName, "*.json", SearchOption.AllDirectories);
        List<Violet> violets = files.Select(File.ReadAllText).Select(s => JsonSerializer.Deserialize<Violet>(s)).ToList();
        Violet violet = violets.FirstOrDefault(violet => violet.Name.Equals(message.Text.Trim(), StringComparison.OrdinalIgnoreCase));
        if (violet != null)
        {
            return (false, $"Фиалка с таким именем уже существует (http://www.kerrisflowers.ru/details/{violet.TransliteratedName}).\nПридумайте другое имя.");
        }

        return (true, "");
    }

    public Task<(bool, string)> RunAction(Violet violet, Message message)
    {
        violet.Name = message.Text;
        return Task.FromResult((true, ""));
    }
}