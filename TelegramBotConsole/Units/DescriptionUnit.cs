using SharedLibrary;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TelegramBotConsole.Units;

internal class DescriptionUnit : IUnit
{
    private readonly ITelegramBotClient _client;

    public DescriptionUnit(ITelegramBotClient client)
    {
        _client = client;
    }

    public async Task Question(ChatId chatId)
    {
        await _client.SendTextMessageAsync(chatId, "Введите описание новой фиалки");
    }
    
    public (bool, string) Validate(Message message)
    {
        if (message.Type != MessageType.Text)
        {
            return (false, "Ожидалось текстовое сообщение. Повторите ввод описания.");
        }
        
        return message.Text!.Length > 1 ? (true, "") : (false, "Описание должно содержать более 1 символа. Повторите ввод описания.");
    }

    public Task<(bool, string)> RunAction(Violet violet, Message message)
    {
        string htmlText = message.Text!;

        htmlText = ImplementBoldFormatting(message, htmlText);
        htmlText = ImplementLineBreaksFormatting(htmlText);

        violet.Description = htmlText;
        return Task.FromResult((true, ""));
    }

    private static string ImplementLineBreaksFormatting(string htmlText)
    {
        htmlText = htmlText.Replace(Environment.NewLine, "<br />");
        return htmlText;
    }

    private static string ImplementBoldFormatting(Message message, string htmlText)
    {
        Queue<string> keys = new(new HashSet<string>
        {
            "套",
            "用",
            "來",
            "處",
            "理",
            "中",
            "文",
            "字",
            "詞",
            "的",
            "函",
            "式",
            "庫",
            "目",
            "前",
            "具",
            "備",
            "的",
            "功",
            "能",
            "主",
            "要",
            "是",
            "反",
            "查",
            "串",
            "中",
            "文",
            "字",
            "的",
            "注",
            "音",
            "或",
            "拼",
            "音"
        });
        Dictionary<string, string> dictionary = new();
        MessageEntity[] entities = message.Entities ?? Array.Empty<MessageEntity>();
        List<MessageEntity> boldEntities = entities.Where(entity => entity.Type == MessageEntityType.Bold).ToList();
        if (boldEntities.Count != 0 && boldEntities.Count <= keys.Count)
        {
            boldEntities.ForEach(entity =>
            {
                string substring = htmlText.Substring(entity.Offset, entity.Length);
                string key = keys.Dequeue();
                key = String.Concat(Enumerable.Repeat(key, substring.Length));
                string editedSubstring = $"<b>{substring}</b>";
                dictionary.Add(key, editedSubstring);
                htmlText = htmlText.Remove(entity.Offset, entity.Length);
                htmlText = htmlText.Insert(entity.Offset, key);
            });

            foreach (KeyValuePair<string, string> pair in dictionary)
            {
                htmlText = htmlText.Replace(pair.Key, pair.Value);
            }
        }

        return htmlText;
    }
}