using SharedLibrary;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TelegramBotConsole;

public static class CommonValidatorsAndActions
{
    public static (bool, string) ValidateDocument(Update update)
    {
        if (update.Message!.Type != MessageType.Document)
        {
            return (false, "Ожидался файл фотографии. Повторите отправку файла.");
        }

        string fileName = update.Message!.Document!.FileName;
        if (fileName != null && Path.GetExtension(fileName).Contains("jpg") == false)
        {
            return (false, "Ожидался файл формата JPG. Повторите отправку файла.");
        }

        return (true, "");
    }
    
    public static async Task<(bool, string)> ProcessDocument(ITelegramBotClient client, Violet violet, Update update)
    {
        string fileId = update.Message!.Document!.FileId;
        string violetDirInTemp = Path.Combine(Path.GetTempPath(), violet.Id.ToString());
        if (Directory.Exists(violetDirInTemp) == false)
        {
            Directory.CreateDirectory(violetDirInTemp);
        }

        string fullPath = Path.Combine(violetDirInTemp, $"{Guid.NewGuid()}.jpg");
        await using FileStream fileStream = System.IO.File.OpenWrite(fullPath);
        await client.GetInfoAndDownloadFileAsync(fileId, fileStream);
        violet.Images.Add(new Image(false, fullPath, fullPath, fullPath, fullPath));

        return (true, "");
    }
}