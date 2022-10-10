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
        
        Option<FileInfo> rootOption = new("--root",
            () => new FileInfo(
                "/home/maskedball/RiderProjects/KerrysFlowersWebApp/KerrysFlowersWebApp/wwwroot/Violets"),
            "Violets root folder") {IsRequired = true};
        rootOption.AddAlias("-r");

        RootCommand rootCommand = new("Telegram interface for KerrysFlowersWebApp");
        rootCommand.AddOption(apiOption);
        rootCommand.AddOption(makerOption);
        rootCommand.AddOption(rootOption);

        // ReSharper disable once ObjectCreationAsStatement
        rootCommand.SetHandler((api, maker, root) => new Runner(api, maker, root), apiOption, makerOption, rootOption);

        return await rootCommand.InvokeAsync(args);
    }
}