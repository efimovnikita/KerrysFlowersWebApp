namespace SharedLibrary.Services;

public interface IFileService
{
    bool FileExists(string filePath);
    string MapPathToWebRoot(string relativePath);
}

