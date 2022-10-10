using System.CommandLine;
using System.Reflection;
using System.Text.Json;
using CliWrap;
using CliWrap.Buffered;
using SharedLibrary;

namespace VioletRemover;

internal class Program
{
    public static async Task<int> Main(string[] args)
    {
        Option<string> guidOption = new("--guid", "Guid for deleted violet") {IsRequired = true};
        guidOption.AddAlias("-g");

        Option<FileInfo> rootOption = new("--root",
            () => new FileInfo(
                "/home/maskedball/RiderProjects/KerrysFlowersWebApp/KerrysFlowersWebApp/wwwroot/Violets"),
            "Violets root folder") {IsRequired = true};
        rootOption.AddAlias("-r");
        
        RootCommand rootCommand = new("Violet remover");
        rootCommand.AddOption(guidOption);
        rootCommand.AddOption(rootOption);

        rootCommand.SetHandler(async (guid, root) => { await Run(root, guid); }, guidOption, rootOption);
        
        return await rootCommand.InvokeAsync(args);
    }

    private static async Task Run(FileInfo root, string guid)
    {
        string solutionPath = GetSolutionPath();

        if (Directory.Exists(root.FullName) == false)
        {
            Console.WriteLine("Root dir not found");
            return;
        }

        string[] directories = Directory.GetDirectories(root.FullName);
        if (directories.Length == 0)
        {
            Console.WriteLine("Root folder is empty");
            return;
        }

        foreach (string directory in directories)
        {
            string jsonFile = Directory.GetFiles(directory, "*.json").FirstOrDefault();
            if (jsonFile == null)
            {
                continue;
            }

            Violet violet = null;
            try
            {
                violet = JsonSerializer.Deserialize<Violet>(await File.ReadAllTextAsync(jsonFile));
            }
            catch
            {
                // ignored
            }

            if (violet == null || !violet.Id.Equals(Guid.Parse(guid)))
            {
                continue;
            }

            string[] files = Directory.GetFiles(directory);
            foreach (string file in files)
            {
                File.Delete(file);
            }

            Directory.Delete(directory);

            BufferedCommandResult gitAddResult = await Cli
                .Wrap("git")
                .WithWorkingDirectory(solutionPath)
                .WithValidation(CommandResultValidation.None)
                .WithArguments($"add {String.Join(' ', files)}")
                .ExecuteBufferedAsync();
            
            BufferedCommandResult gitCommitResult = await Cli
                .Wrap("git")
                .WithWorkingDirectory(solutionPath)
                .WithValidation(CommandResultValidation.None)
                .WithArguments($"commit -m \"remove violet {violet.Id}\"")
                .ExecuteBufferedAsync();
            
            Console.WriteLine(gitAddResult.StandardError);
            Console.WriteLine(gitAddResult.StandardOutput);
            
            Console.WriteLine(gitCommitResult.StandardError);
            Console.WriteLine(gitCommitResult.StandardOutput);

            Console.WriteLine("Success!");

            break;
        }
    }

    private static string GetSolutionPath()
    {
        string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly()
            .Location);
        string solutionPath =
            Directory.GetParent(
                Directory.GetParent(Directory.GetParent(Directory.GetParent(assemblyPath!)!.FullName)!.FullName)!
                    .FullName)!.FullName;
        return solutionPath;
    }
}