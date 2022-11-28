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

    public async Task Question(ChatId id)
    {
        VioletColor[] colors = Enum.GetValues(typeof(VioletColor))
            .Cast<VioletColor>()
            .ToArray();
        string[] descriptions = colors.Select(color => ExtensionMethods.GetEnumDescription(color)
            .ToLower()).ToArray();
        string resultDescriptions = String.Join(", ", descriptions);

        await _client
            .SendTextMessageAsync(id,
                $"Введите список цветов фиалки (через запятую). Список возможных цветов:\n\n<i>{resultDescriptions}</i>.",
                parseMode: ParseMode.Html);
    }
    
    public (bool, string) Validate(Update update)
    {
        if (update.Message!.Type != MessageType.Text)
        {
            return (false, "Ожидалось текстовое сообщение. Повторите ввод списка цветов.");
        }

        string text = update.Message!.Text!.ToLower().Trim();
        List<VioletColor> selectedColors = GetSelectedColors(text);
        
        if (text.Split(',').All(String.IsNullOrEmpty))
        {
            return (false, "Список цветов пуст. Повторите ввод списка цветов.");
        }

        if (selectedColors.Count == 0)
        {
            return (false, "Введите цвета фиалки из списка возможных цветов.");
        }

        return (true, "");
    }

    public Task<(bool, string)> RunAction(Violet violet, Update update)
    {
        string text = update.Message!.Text!.ToLower().Trim();
        List<VioletColor> selectedColors = GetSelectedColors(text);
        violet.Colors = selectedColors;

        return Task.FromResult((true, ""));
    }

    private static List<VioletColor> GetSelectedColors(string text)
    {
        List<string> colors = text.Split(',')
            .Where(color => String.IsNullOrEmpty(color) == false)
            .Select(color => color.Trim().Replace('ё', 'е'))
            .ToList();
        VioletColor[] allColors = Enum.GetValues(typeof(VioletColor)).Cast<VioletColor>().ToArray();
        List<VioletColor> selectedColors = allColors.Where(color =>
            colors.Contains(ExtensionMethods.GetEnumDescription(color).ToLower())).ToList();
        return selectedColors;
    }
}