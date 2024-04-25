using KFTelegramBot.Providers;
using SharedLibrary;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace KFTelegramBot.Model;

public class VioletColorsPipelineItem(IRecommendationsProvider recommendationsProvider) : IPipelineItem
{
    public async Task<Message> AskAQuestion(Message message, ITelegramBotClient botClient, object? violetObject = null)
    {
        var chatId = message.Chat.Id;
        const string aiRecommendationMsg = "_Рекомендация от ИИ:_";
        var possibleVariants = GetPossibleVariants();
        var msg = $"Введите, через запятую, цвета новой фиалки.\n{possibleVariants}";

        var question = await botClient.SendTextMessageAsync(chatId, $"{msg}\n\n{aiRecommendationMsg} \u23f1\ufe0f", parseMode: ParseMode.Markdown);

        _ = Task.Run(async () =>
        {
            if (violetObject is Violet violet)
            {
                var recommendation = await recommendationsProvider.GetColorsRecommendation(violet.Images.First().W700);
                if (string.IsNullOrEmpty(recommendation) == false)
                {
                    await botClient.EditMessageTextAsync(chatId, question.MessageId, $"{msg}\n\n{aiRecommendationMsg}\n`{recommendation}`",
                        parseMode: ParseMode.Markdown);
                }
                else
                {
                    await botClient.EditMessageTextAsync(chatId, question.MessageId, msg, parseMode: ParseMode.Markdown);
                }
            }
        });

        return question;
    }

    private static string GetPossibleVariants()
    {
        var values = Enum.GetValues(typeof(VioletColor));
        var colors = values
            .OfType<VioletColor>()
            .Select(color => $"`{ExtensionMethods.GetEnumDescription(color)}`")
            .ToArray();

        var possibleVariants = $"Возможные варианты:\n{string.Join(", ", colors)}";
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
            .ToDictionary(color => ExtensionMethods.GetEnumDescription(color).ToLowerInvariant(), color => color);

        var colors = message.Text!
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim().ToLowerInvariant())
            .Select(s => possibleColors[s])
            .ToArray();

        violet.Colors.AddRange(colors);
        return (true, null);
    }
}