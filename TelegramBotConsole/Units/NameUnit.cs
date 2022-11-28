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
    private readonly DirectoryInfo _root;

    public NameUnit(ITelegramBotClient client, DirectoryInfo root)
    {
        _client = client;
        _root = root;
    }

    public async Task Question(ChatId id)
    {
        await _client.SendTextMessageAsync(id, "Введите имя новой фиалки");
    }

    public (bool, string) Validate(Update update)
    {
        if (update.Message!.Type != MessageType.Text)
        {
            return (false, "Ожидалось текстовое сообщение. Повторите ввод имени.");
        }

        if (update.Message.Text!.Length <= 1)
        {
            return (false, "Имя должно содержать более 1 символа. Повторите ввод имени.");
        }

        string[] files = Directory.GetFiles(_root.FullName, "*.json", SearchOption.AllDirectories);
        List<Violet> violets = files.Select(File.ReadAllText).Select(s => JsonSerializer.Deserialize<Violet>(s)).ToList();
        Violet violet = violets.FirstOrDefault(violet => violet.Name.Equals(update.Message.Text.Trim(), StringComparison.OrdinalIgnoreCase));
        if (violet != null)
        {
            return (false, $"Фиалка с таким именем уже существует (http://www.kerrisflowers.ru/details/{violet.TransliteratedName}).\nПридумайте другое имя.");
        }

        return (true, "");
    }

    public Task<(bool, string)> RunAction(Violet violet, Update update)
    {
        violet.Name = update.Message!.Text;
        return Task.FromResult((true, ""));
    }
}