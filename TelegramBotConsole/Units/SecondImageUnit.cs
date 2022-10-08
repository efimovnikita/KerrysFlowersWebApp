using SharedLibrary;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TelegramBotConsole.Units;

internal class SecondImageUnit : IUnit
{
    private readonly ITelegramBotClient _client;

    public SecondImageUnit(ITelegramBotClient client)
    {
        _client = client;
    }

    public Task Question(ChatId chatId)
    {
        return Task.CompletedTask;
    }
    
    public (bool, string) Validate(Message message)
    {
        return message.Type != MessageType.Photo ? (false, "Ожидался файл фотографии. Повторите отправку файла.") : (true, "");
    }

    public async Task<(bool, string)> RunAction(Violet violet, Message message)
    {
        PhotoSize photoSize = message.Photo!.Last();
        string fileId = photoSize.FileId;
        string violetDirInTemp = Path.Combine(Path.GetTempPath(), violet.Id.ToString());
        if (Directory.Exists(violetDirInTemp) == false)
        {
            Directory.CreateDirectory(violetDirInTemp);
        }

        string fullPath = Path.Combine(violetDirInTemp, $"{Guid.NewGuid()}.jpg");
        await using FileStream fileStream = System.IO.File.OpenWrite(fullPath);
        await _client.GetInfoAndDownloadFileAsync(fileId, fileStream);
        violet.Images.Add(new Image(false, fullPath, fullPath, fullPath, fullPath));

        return (true, "");
    }
}