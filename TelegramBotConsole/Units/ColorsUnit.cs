using SharedLibrary;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TelegramBotConsole.Units;

internal class ColorsUnit : IUnit
{
    private readonly ITelegramBotClient _client;

    public ColorsUnit(ITelegramBotClient client)
    {
        _client = client;
    }

    public async Task Question(ChatId chatId)
    {
        VioletColor[] colors = Enum.GetValues(typeof(VioletColor))
            .Cast<VioletColor>()
            .ToArray();
        string[] descriptions = colors.Select(color => ExtensionMethods.GetEnumDescription(color)
            .ToLower()).ToArray();
        string resultDescriptions = String.Join(", ", descriptions);

        await _client
            .SendTextMessageAsync(chatId,
                $"Введите список цветов фиалки (через запятую). Список возможных цветов: {resultDescriptions}.");
    }
    
    public (bool, string) Validate(Message message)
    {
        if (message.Type != MessageType.Text)
        {
            return (false, "Ожидалось текстовое сообщение. Повторите ввод списока цветов.");
        }

        string text = message.Text!.ToLower();
        
        if (text.Split(',').All(String.IsNullOrEmpty))
        {
            return (false, "Список цветов пуст. Повторите ввод списка цветов.");
        }

        return (true, "");
    }

    public Task<(bool, string)> RunAction(Violet violet, Message message)
    {
        string text = message.Text!.ToLower().Trim();
        List<string> colors = text.Split(',')
            .Where(color => String.IsNullOrEmpty(color) == false)
            .Select(color => color.Trim().Replace('ё', 'е'))
            .ToList();
        VioletColor[] allColors = Enum.GetValues(typeof(VioletColor)).Cast<VioletColor>().ToArray();
        List<VioletColor> selectedColors = allColors.Where(color =>
            colors.Contains(ExtensionMethods.GetEnumDescription(color).ToLower())).ToList();
        violet.Colors = selectedColors;

        return Task.FromResult((true, ""));
    }
}