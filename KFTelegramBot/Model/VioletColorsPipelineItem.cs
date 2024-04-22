using SharedLibrary;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace KFTelegramBot.Model;

public class VioletColorsPipelineItem : IPipelineItem
{
    public Task<Message> AskAQuestion(Message message, ITelegramBotClient botClient)
    {
        var possibleVariants = GetPossibleVariants();

        var text = $"Введите, через запятую, цвета новой фиалки.\n{possibleVariants}";
        return botClient.SendTextMessageAsync(message.Chat.Id, text, parseMode: ParseMode.Markdown);
    }

    private static string GetPossibleVariants()
    {
        var values = Enum.GetValues(typeof(VioletColor));
        var colors = values
            .OfType<VioletColor>()
            .Select(color => $"`{ExtensionMethods.GetEnumDescription(color)}`")
            .ToArray();

        var possibleVariants = $"Возможные варианты:\n{string.Join(',', colors)}";
        return possibleVariants;
    }

    public (bool, Task<Message>?) ValidateInput(Message message, ITelegramBotClient botClient)
    {
        if (message.Type != MessageType.Text)
        {
            return (false,
                botClient.SendTextMessageAsync(message.Chat.Id, "Ожидалось текстовое сообщение. Повторите ввод."));
        }

        var values = Enum.GetValues(typeof(VioletColor));
        var possibleColors = values
            .OfType<VioletColor>()
            .Select(color => ExtensionMethods.GetEnumDescription(color))
            .ToArray();

        var colors = message.Text!
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .ToArray();

        var allColorsExist = colors.All(c => possibleColors.Contains(c, StringComparer.OrdinalIgnoreCase));
        if (allColorsExist == false)
        {
            return (false,
                botClient.SendTextMessageAsync(message.Chat.Id, $"Выбраны не корректные цвета.\n{GetPossibleVariants()}.\nПовторите ввод.", parseMode: ParseMode.Markdown));
        }

        return (true, null);
    }

    public (bool, Task<Message>?) DoTheJob(Message message, object affectedObject, ITelegramBotClient botClient)
    {
        if (affectedObject is not Violet violet)
        {
            return (false,
                botClient.SendTextMessageAsync(message.Chat.Id, "Редактируемый объект неверного типа. Повторите ввод."));
        }

        var values = Enum.GetValues(typeof(VioletColor));
        var possibleColors = values
            .OfType<VioletColor>()
            .ToDictionary(color => ExtensionMethods.GetEnumDescription(color), color => color);

        var colors = message.Text!
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Select(s => possibleColors[s])
            .ToArray();

        violet.Colors.AddRange(colors);
        return (true, null);
    }
}