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

        RootCommand rootCommand = new("Telegram interface for KerrysFlowersWebApp");
        rootCommand.AddOption(apiOption);
        rootCommand.AddOption(makerOption);
            
        // ReSharper disable once ObjectCreationAsStatement
        rootCommand.SetHandler((api, maker) => new Runner(api, maker), apiOption, makerOption);

        return await rootCommand.InvokeAsync(args);
    }
}