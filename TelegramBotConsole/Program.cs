using System.CommandLine;
using System.Text;

namespace TelegramBotConsole;

internal class Program
{
    private static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        
        Option<string> apiOption = new("--api", "API key for telegram bot") {IsRequired = true};
        apiOption.AddAlias("-a");

        Option<FileInfo> makerOption = new("--maker", "Path to violet maker utility") {IsRequired = true};
        makerOption.AddAlias("-m");
        
        Option<DirectoryInfo> rootOption = new("--root",
            () => new DirectoryInfo(
                "/home/maskedball/RiderProjects/KerrysFlowersWebApp/KerrysFlowersWebApp/wwwroot/Violets"),
            "Violets root folder") {IsRequired = true};
        rootOption.AddAlias("-r");
        
        Option<FileInfo> reducerOption = new("--reducer",
            () => new FileInfo("/home/maskedball/RiderProjects/KerrysFlowersWebApp/reducer/target/release/reducer"),
            "Path to reducer tool") {IsRequired = true};

        RootCommand rootCommand = new("Telegram interface for KerrysFlowersWebApp");
        rootCommand.AddOption(apiOption);
        rootCommand.AddOption(makerOption);
        rootCommand.AddOption(rootOption);
        rootCommand.AddOption(reducerOption);

        // ReSharper disable once ObjectCreationAsStatement
        rootCommand.SetHandler((api, maker, root, reducer) => new Runner(api, maker, root, reducer), apiOption, makerOption, rootOption, reducerOption);

        return await rootCommand.InvokeAsync(args);
    }
}