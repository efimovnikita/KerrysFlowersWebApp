using SharedLibrary;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace KFTelegramBot.Model;

public class DigestWarehouseXlsPipelineItem : IPipelineItem
{
    public Task<Message> AskAQuestion(Message message, ITelegramBotClient botClient, object? violetObject = null) =>
        botClient.SendTextMessageAsync(message.Chat.Id, "Отправьте Excel (.xlsx) файл с информацией о складских остатках и ценах.");

    public (bool, Task<Message>?) ValidateInput(Message message, ITelegramBotClient botClient)
    {
        if (message.Type != MessageType.Document)
        {
            return (false,
                botClient.SendTextMessageAsync(message.Chat.Id,
                    "Ожидалось сообщение в формате документа Excel (.xlsx). Повторите ввод."));
        }

        var document = message.Document;
        var fileName = document!.FileName;
        var extension = Path.GetExtension(fileName);

        if (extension!.Equals(".xlsx", StringComparison.OrdinalIgnoreCase) == false)
        {
            return (false,
                botClient.SendTextMessageAsync(message.Chat.Id,
                    "Формат документа должен быть 'xlsx'. Повторите ввод."));
        }

        return (true, null);
    }

    public (bool, Task<Message>?) DoTheJob(Message message, object affectedObject, ITelegramBotClient botClient)
    {
        if (affectedObject is not List<WarehouseVioletItem> items)
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

        var warehouseVioletItems = ExcelReader.ReadExcel(fullDocumentFileName);
        if (warehouseVioletItems.Count == 0)
        {
            return (false,
                botClient.SendTextMessageAsync(message.Chat.Id, "Во время обработки файла с информацией о складских остатках возникла ошибка. Повторите ввод."));
        }
        
        items.AddRange(warehouseVioletItems);

        return (true,
            botClient.SendTextMessageAsync(message.Chat.Id, "Файл с информацией о складских остатках загружен и обработан."));
    }
}