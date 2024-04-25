using SharedLibrary;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace KFTelegramBot.Model;

public class VioletSizePipelineItem : IPipelineItem
{
    public Task<Message> AskAQuestion(Message message, ITelegramBotClient botClient, object? violetObject = null)
    {
        var possibleVariants = GetPossibleVariants();

        var text = $"Введите размер новой фиалки.\n{possibleVariants}";
        return botClient.SendTextMessageAsync(message.Chat.Id, text, parseMode: ParseMode.Markdown);
    }

    private static string GetPossibleVariants()
    {
        var values = Enum.GetValues(typeof(VioletSize));
        var sizes = values
            .OfType<VioletSize>()
            .Select(size => $"`{ExtensionMethods.GetEnumDescription(size)}`")
            .ToArray();

        var possibleVariants = $"Возможные варианты размера:\n{string.Join(", ", sizes)}";
        return possibleVariants;
    }

    public (bool, Task<Message>?) ValidateInput(Message message, ITelegramBotClient botClient)
    {
        if (message.Type != MessageType.Text)
        {
            return (false,
                botClient.SendTextMessageAsync(message.Chat.Id, "Ожидалось текстовое сообщение. Повторите ввод."));
        }

        var values = Enum.GetValues(typeof(VioletSize));
        var possibleSizes = values
            .OfType<VioletSize>()
            .Select(size => ExtensionMethods.GetEnumDescription(size))
            .ToArray();

        var sizeExists = possibleSizes.Contains(message.Text, StringComparer.OrdinalIgnoreCase);
        if (sizeExists == false)
        {
            return (false,
                botClient.SendTextMessageAsync(message.Chat.Id,
                    $"Выбран не корректный размер.\n{GetPossibleVariants()}.\nПовторите ввод.",
                    parseMode: ParseMode.Markdown));
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

        var values = Enum.GetValues(typeof(VioletSize));
        var possibleSizes = values
            .OfType<VioletSize>()
            .ToDictionary(size => ExtensionMethods.GetEnumDescription(size).ToLowerInvariant(), size => size);

        violet.Size = possibleSizes[message.Text!.ToLowerInvariant()];
        return (true, null);
    }
}