using System;
using System.CommandLine;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using File = System.IO.File;

namespace TelegramBotRemoverConsole;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        
        Option<string> apiOption = new("--api", "API key for telegram bot") {IsRequired = true};
        apiOption.AddAlias("-a");

        Option<FileInfo> removerOption = new("--remover", "Path to violet remover utility") {IsRequired = true};
        removerOption.AddAlias("-r");

        Option<FileInfo> rootOption = new("--root", "Violets root dir") {IsRequired = true};

        RootCommand rootCommand = new("Telegram interface for KerrysFlowersWebApp");
        rootCommand.AddOption(apiOption);
        rootCommand.AddOption(removerOption);
        rootCommand.AddOption(rootOption);
            
        // ReSharper disable once ObjectCreationAsStatement
        rootCommand.SetHandler((api, remover, root) =>
        {
            if (File.Exists(remover.FullName) == false)
            {
                Console.WriteLine("Remover tool not found");
                return;
            }

            if (Directory.Exists(root.FullName) == false)
            {
                Console.WriteLine("Root dir not exist");
                return;
            }
            
            IHost host = CreateHostBuilder(args, api, remover, root).Build();
            host.StartAsync();
        }, apiOption, removerOption, rootOption);

        return await rootCommand.InvokeAsync(args);
    }
    
    private static IHostBuilder CreateHostBuilder(string[] args, string api, FileInfo remover, FileInfo root)
    {
        return Host.CreateDefaultBuilder(args)
            .UseSystemd()
            .ConfigureServices((_, services) =>
            {
                services.AddTransient(_ => new Remover(api, remover, root));
            });
    }
}