using SharedLibrary.Services;

namespace KerrysFlowersWebApp.Services;

public class FileService(IWebHostEnvironment webHostEnvironment) : IFileService
{
    public bool FileExists(string filePath)
    {
        return File.Exists(filePath);
    }

    public string MapPathToWebRoot(string relativePath)
    {
        return Path.Combine(webHostEnvironment.WebRootPath, relativePath);
    }
}

