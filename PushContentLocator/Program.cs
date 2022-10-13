using System.CommandLine;
using System.Reflection;

namespace PushContentLocator;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        string solutionPath = Directory.GetParent(Directory.GetParent(Directory.GetParent(Directory.GetParent(assemblyPath!)!.FullName)!.FullName)!.FullName)!.FullName;

        Option<string> tokenOption = new("--token", "GitHub access token") {IsRequired = true};
        tokenOption.AddAlias("-t");

        Option<FileInfo> publishDirOption = new("--publish-dir", "Violets folder in publish dir") {IsRequired = true};
        publishDirOption.AddAlias("-p");
        
        Option<FileInfo> sourceDirOption = new("--source-dir", "Violets folder in source dir") {IsRequired = true};
        sourceDirOption.AddAlias("-s");

        RootCommand rootCommand = new("GitHub push event locator");
        rootCommand.AddOption(sourceDirOption);
        rootCommand.AddOption(publishDirOption);
        rootCommand.AddOption(tokenOption);

        rootCommand.SetHandler(async (source, publish, token) =>
        {
            Runner runner = new(source, publish, token, new FileInfo(solutionPath));
            await runner.Run();
        }, sourceDirOption, publishDirOption, tokenOption);

        return await rootCommand.InvokeAsync(args);
    }
}