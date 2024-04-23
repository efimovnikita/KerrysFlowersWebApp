using SharedLibrary;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace KFTelegramBot.Model;

public class VioletImagesPipelineItem : IPipelineItem
{
    public Task<Message> AskAQuestion(Message message, ITelegramBotClient botClient) =>
        botClient.SendTextMessageAsync(message.Chat.Id, "Отправьте изображения фиалки (3 шт., минимальный размер по ширине 700 px)");

    public (bool, Task<Message>?) ValidateInput(Message message, ITelegramBotClient botClient)
    {
        if (message.Type != MessageType.Photo)
        {
            return (false,
                botClient.SendTextMessageAsync(message.Chat.Id, "Ожидалось сообщение в формате фотографии. Повторите ввод."));
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

        var photoSize = message.Photo![^1];

        var file = botClient.GetFileAsync(photoSize.FileId).Result;

        var path = Path.GetTempPath();
        var fileName = Path.Combine(path, file.FileId + ".jpg");
        using (var stream = System.IO.File.OpenWrite(fileName))
        {
            botClient.DownloadFileAsync(file.FilePath!, stream).GetAwaiter().GetResult();
        }

        var resizer = new ImageResizer();
        var hasMinimumWidth = resizer.HasMinimumWidth(fileName, 700);
        if (hasMinimumWidth == false)
        {
            return (false,
                botClient.SendTextMessageAsync(message.Chat.Id, "Изображение должноб быть не менее чем 700 px в ширину. Повторите ввод."));
        }

        var name300 = $"{Path.GetFileNameWithoutExtension(fileName)}_300.jpg";
        var path300 = Path.Combine(path, name300);
        resizer.ResizeImage(fileName, path300, 300);
        var imageBytes300 = System.IO.File.ReadAllBytes(path300);
        var base64300 = Convert.ToBase64String(imageBytes300);

        var name330 = $"{Path.GetFileNameWithoutExtension(fileName)}_330.jpg";
        var path330 = Path.Combine(path, name330);
        resizer.ResizeImage(fileName, path330, 330);
        var imageBytes330 = System.IO.File.ReadAllBytes(path330);
        var base64330 = Convert.ToBase64String(imageBytes330);

        var name500 = $"{Path.GetFileNameWithoutExtension(fileName)}_500.jpg";
        var path500 = Path.Combine(path, name500);
        resizer.ResizeImage(fileName, path500, 500);
        var imageBytes500 = System.IO.File.ReadAllBytes(path500);
        var base64500 = Convert.ToBase64String(imageBytes500);

        var name700 = $"{Path.GetFileNameWithoutExtension(fileName)}_700.jpg";
        var path700 = Path.Combine(path, name700);
        resizer.ResizeImage(fileName, path700, 700);
        var imageBytes700 = System.IO.File.ReadAllBytes(path700);
        var base64700 = Convert.ToBase64String(imageBytes700);

        var image = new Image(violet.Images.Count == 0, base64300, base64330, base64500, base64700);

        violet.Images.Add(image);

        if (violet.Images.Count == 3)
        {
            _ = botClient.SendTextMessageAsync(message.Chat.Id, $"Загружено {violet.Images.Count} из 3 изображений.")
                .Result;
            return (true, null);
        }

        return (false,
            botClient.SendTextMessageAsync(message.Chat.Id, $"Загружено {violet.Images.Count} из 3 изображений."));
    }
}