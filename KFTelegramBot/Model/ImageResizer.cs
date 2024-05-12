using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.Fonts;
using SixLabors.ImageSharp.Drawing.Processing;

namespace KFTelegramBot.Model;

public class ImageResizer
{
    public void ResizeImage(string inputFile, string outputFile, int newWidth, string violetName, string siteName,
        float fontSize, float padding = 10)
    {
        using var image = Image.Load(inputFile);
        var newHeight = (int)(image.Height / ((float)image.Width / newWidth));

        // Create the font
        var fontFamily = SystemFonts.Collection.Get("Calibri");
        var font = new Font(fontFamily, fontSize, FontStyle.Regular); // Adjust the size as needed

        // Define the options for the watermark text
        var violetNameRichTextOptions = new RichTextOptions(font)
        {
            Origin = new PointF(0, 0), // We will set the position later based on the image size
        };
        
        var siteNameRichTextOptions = new RichTextOptions(font)
        {
            Origin = new PointF(0, 0), // We will set the position later based on the image size
        };

        // Measure the size of the text
        var violetNameSize = TextMeasurer.MeasureAdvance(violetName, violetNameRichTextOptions);
        var siteNameSize = TextMeasurer.MeasureAdvance(siteName, violetNameRichTextOptions);

        var violetXPosition = (newWidth - violetNameSize.Width) / 2; 
        var siteXPosition = (newWidth - siteNameSize.Width) / 2; 

        violetNameRichTextOptions.Origin = new PointF(violetXPosition, newHeight - violetNameSize.Height - padding);
        siteNameRichTextOptions.Origin = new PointF(siteXPosition, padding); 

        // Define the color and opacity of the watermark text
        var watermarkColor = Color.White.WithAlpha(0.9f); // 50% opacity
        
        image.Mutate(ctx =>
        {
            ctx.Resize(newWidth, newHeight, KnownResamplers.Lanczos3);
            ctx.DrawText(violetNameRichTextOptions, violetName, watermarkColor);
            ctx.DrawText(siteNameRichTextOptions, siteName, watermarkColor);
        });

        image.SaveAsJpeg(outputFile);
    }

    public bool HasMinimumWidth(string imagePath, int minWidth)
    {
        using var image = Image.Load(imagePath);
        return image.Width >= minWidth;
    }
}