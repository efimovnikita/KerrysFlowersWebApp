using SharedLibrary;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace KFTelegramBot.Model;

public class VioletDocumentsPipelineItem : IPipelineItem
{
    public Task<Message> AskAQuestion(Message message, ITelegramBotClient botClient, object? violetObject = null) =>
        botClient.SendTextMessageAsync(message.Chat.Id, "Отправьте файлы изображений фиалки (3 шт., минимальный размер по ширине 700 px)");

    public (bool, Task<Message>?) ValidateInput(Message message, ITelegramBotClient botClient)
    {
        if (message.Type != MessageType.Document)
        {
            return (false,
                botClient.SendTextMessageAsync(message.Chat.Id,
                    "Ожидалось сообщение в формате документа. Повторите ввод."));
        }

        var document = message.Document;
        var fileName = document!.FileName;
        var extension = Path.GetExtension(fileName);

        if (extension!.Equals(".jpg", StringComparison.OrdinalIgnoreCase) == false)
        {
            return (false,
                botClient.SendTextMessageAsync(message.Chat.Id,
                    "Формат документа должен быть 'jpg'. Повторите ввод."));
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

        var document = message.Document;
        var fileName = document!.FileName;
        var documentFileId = document.FileId;

        var file = botClient.GetFileAsync(documentFileId).GetAwaiter().GetResult();

        var tempPath = Path.GetTempPath();
        var fullDocumentFileName = Path.Combine(tempPath, fileName!);

        using (var stream = System.IO.File.OpenWrite(fullDocumentFileName))
        {
            botClient.DownloadFileAsync(file.FilePath!, stream).GetAwaiter().GetResult();
        }

        var resizer = new ImageResizer();
        var hasMinimumWidth = resizer.HasMinimumWidth(fullDocumentFileName, 700);
        if (hasMinimumWidth == false)
        {
            return (false,
                botClient.SendTextMessageAsync(message.Chat.Id, "Изображение должно быть не менее чем 700 px в ширину. Повторите ввод."));
        }

        var originalBytes = System.IO.File.ReadAllBytes(fullDocumentFileName);
        var originalBase64 = Convert.ToBase64String(originalBytes);

        var name300 = $"{Path.GetFileNameWithoutExtension(fileName)}_300.jpg";
        var path300 = Path.Combine(tempPath, name300);
        resizer.ResizeImage(fullDocumentFileName, path300, 300);
        var imageBytes300 = System.IO.File.ReadAllBytes(path300);
        var base64300 = Convert.ToBase64String(imageBytes300);

        var name330 = $"{Path.GetFileNameWithoutExtension(fileName)}_330.jpg";
        var path330 = Path.Combine(tempPath, name330);
        resizer.ResizeImage(fullDocumentFileName, path330, 330);
        var imageBytes330 = System.IO.File.ReadAllBytes(path330);
        var base64330 = Convert.ToBase64String(imageBytes330);

        var name500 = $"{Path.GetFileNameWithoutExtension(fileName)}_500.jpg";
        var path500 = Path.Combine(tempPath, name500);
        resizer.ResizeImage(fullDocumentFileName, path500, 500);
        var imageBytes500 = System.IO.File.ReadAllBytes(path500);
        var base64500 = Convert.ToBase64String(imageBytes500);

        var name700 = $"{Path.GetFileNameWithoutExtension(fileName)}_700.jpg";
        var path700 = Path.Combine(tempPath, name700);
        resizer.ResizeImage(fullDocumentFileName, path700, 700);
        var imageBytes700 = System.IO.File.ReadAllBytes(path700);
        var base64700 = Convert.ToBase64String(imageBytes700);

        var image = new Image(violet.Images.Count == 0, originalBase64, base64300, base64330, base64500, base64700);

        violet.Images.Add(image);

        var text = $"Загружено {violet.Images.Count} из 3 изображений.";

        if (violet.Images.Count == 3)
        {
            _ = botClient.SendTextMessageAsync(message.Chat.Id, text)
                .Result;
            return (true, null);
        }

        return (false,
            botClient.SendTextMessageAsync(message.Chat.Id, text));
    }
}