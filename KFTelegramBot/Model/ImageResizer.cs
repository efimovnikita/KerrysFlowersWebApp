using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace KFTelegramBot.Model;

public class ImageResizer
{
    public void ResizeImage(string inputFile, string outputFile, int newWidth)
    {
        using var image = SixLabors.ImageSharp.Image.Load(inputFile);
        var newHeight = (int)(image.Height / ((float)image.Width / newWidth));

        image.Mutate(ctx => ctx.Resize(newWidth, newHeight));

        ImageExtensions.SaveAsJpeg(image, outputFile);
    }

    public bool HasMinimumWidth(string imagePath, int minWidth)
    {
        using var image = SixLabors.ImageSharp.Image.Load(imagePath);
        return image.Width >= minWidth;
    }
}