using SharedLibrary.Providers;

namespace KerrysFlowersWebApp.Services;

public class PrerenderImagesBackgroundService(
    ILogger<PrerenderImagesBackgroundService> logger,
    IWebHostEnvironment environment,
    IVioletRepository violetRepository)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("PrerenderImagesBackgroundService is starting.");
        logger.LogInformation("PrerenderImagesBackgroundService is running.");
        
        await PrerenderImageAsync();
        
        logger.LogInformation("PrerenderImagesBackgroundService has stopped.");
    }

    private async Task PrerenderImageAsync()
    {
        // Ensure the directory exists
        var dynamicFolder = Path.Combine(environment.WebRootPath, "images");
        Directory.CreateDirectory(dynamicFolder);

        // Generate and save the image
        var violets = violetRepository.GetAllViolets();
        
        foreach (var violet in violets)
        {
            for (var i = 0; i < violet.Images.Count; i++)
            {
                var image = violet.Images[i];
                await GenerateImageFile(dynamicFolder, image.W300, violet.Id, $"{nameof(image.W300)}_{i}");
                await GenerateImageFile(dynamicFolder, image.W330, violet.Id, $"{nameof(image.W330)}_{i}");
                await GenerateImageFile(dynamicFolder, image.W500, violet.Id, $"{nameof(image.W500)}_{i}");
                await GenerateImageFile(dynamicFolder, image.W700, violet.Id, $"{nameof(image.W700)}_{i}");
            }
        }
    }

    private async Task GenerateImageFile(string folder, string base64String, Guid violetId, string suffix)
    {
        var path = Path.Combine(folder, $"{violetId}_{suffix}.jpg");
        if (File.Exists(path))
        {
            return;
        }
        
        await ImageHelperService.SaveImageFromBase64Async(base64String, path);
        logger.LogInformation($"Generated image saved at: {path}");
    }
}