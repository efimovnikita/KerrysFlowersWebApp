using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace KerrysFlowersWebApp.Services;

public static class ImageHelperService
{
    public static async Task SaveImageFromBase64Async(string base64String, string outputPath)
    {
        var imageBytes = Convert.FromBase64String(base64String);
        using var ms = new MemoryStream(imageBytes);
        using var image = await Image.LoadAsync<Rgba32>(ms);
        await image.SaveAsJpegAsync(outputPath);
    }
}