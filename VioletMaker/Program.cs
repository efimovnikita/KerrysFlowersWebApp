using System.CommandLine;
using System.Reflection;
using CliWrap;
using CliWrap.Buffered;
using Faker;
using Newtonsoft.Json;
using SharedLibrary;

namespace VioletMaker;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        string? assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        string solutionPath = Directory.GetParent(Directory.GetParent(Directory.GetParent(Directory.GetParent(assemblyPath).FullName).FullName).FullName).FullName;

        Option<string> nameOption = new("--name", () => String.Join(' ', Lorem.Words(2)).FirstCharToUpper(), "Violet name")
            {IsRequired = true};
        nameOption.AddAlias("-n");
        Option<string> breederOption = new("--breeder", Name.FullName, "Breeder name") {IsRequired = true};
        breederOption.AddAlias("-b");
        Option<string> descriptionOption = new("--description", () => Lorem.Paragraph(3), "Violet description")
            {IsRequired = true};
        descriptionOption.AddAlias("-d");
        Option<IEnumerable<string>> tagsOption = new("--tags", () => Lorem.Words(2), "Violet tags")
            {AllowMultipleArgumentsPerToken = true, IsRequired = true};
        tagsOption.AddAlias("-t");
        Option<DateTime> breedDateOption = new("--date", () => DateTime.Today, "Violet breeding date")
            {IsRequired = true};
        Option<FileInfo> image1Option = new("--image1", () => new FileInfo(Path.Combine(assemblyPath, "photo1.jpg")), "First image of violet") {IsRequired = true};
        Option<FileInfo> image2Option = new("--image2", () => new FileInfo(Path.Combine(assemblyPath, "photo2.jpg")), "Second image of violet") {IsRequired = true};
        Option<FileInfo> image3Option = new("--image3", () => new FileInfo(Path.Combine(assemblyPath, "photo3.jpg")),"Third image of violet") {IsRequired = true};
        Option<bool> chimeraOption = new("--chimera", () => false, "Is this violet chimera?") {IsRequired = true};
        Option<IEnumerable<VioletColor>> colorsOption =
            new("--colors", () => new List<VioletColor> {VioletColor.Blue, VioletColor.Green}, "Violet colors")
                {AllowMultipleArgumentsPerToken = true, IsRequired = true};
        Option<FileInfo> rootOption = new("--root",
            () => new FileInfo(
                "/home/maskedball/RiderProjects/KerrysFlowersWebApp/KerrysFlowersWebApp/wwwroot/Violets"),
            "Root folder for new violet") {IsRequired = true};
        rootOption.AddAlias("-r");

        RootCommand rootCommand = new("Violet maker");
        rootCommand.AddOption(nameOption);
        rootCommand.AddOption(breederOption);
        rootCommand.AddOption(descriptionOption);
        rootCommand.AddOption(tagsOption);
        rootCommand.AddOption(breedDateOption);
        rootCommand.AddOption(image1Option);
        rootCommand.AddOption(image2Option);
        rootCommand.AddOption(image3Option);
        rootCommand.AddOption(chimeraOption);
        rootCommand.AddOption(colorsOption);
        rootCommand.AddOption(rootOption);

        rootCommand.SetHandler(async context =>
        {
            string? name = context.ParseResult.GetValueForOption(nameOption);
            string? breeder = context.ParseResult.GetValueForOption(breederOption);
            string? description = context.ParseResult.GetValueForOption(descriptionOption);
            IEnumerable<string>? tags = context.ParseResult.GetValueForOption(tagsOption);
            DateTime date = context.ParseResult.GetValueForOption(breedDateOption);
            FileInfo? image1 = context.ParseResult.GetValueForOption(image1Option);
            FileInfo? image2 = context.ParseResult.GetValueForOption(image2Option);
            FileInfo? image3 = context.ParseResult.GetValueForOption(image3Option);
            bool chimera = context.ParseResult.GetValueForOption(chimeraOption);
            IEnumerable<VioletColor>? colors = context.ParseResult.GetValueForOption(colorsOption);
            FileInfo? rootFolder = context.ParseResult.GetValueForOption(rootOption);

            Guid id = Guid.NewGuid();
            DirectoryInfo violetDir = Directory.CreateDirectory(Path.Combine(rootFolder.FullName, id.ToString()));

            FileInfo?[] rawImages = { image1, image2, image3 };
            List<Image> images = new(3);
            for (int i = 0; i < rawImages.Length; i++)
            {
                FileInfo? fileInfo = rawImages[i];
                string fullName = fileInfo.FullName;
                string violetDirFullName = violetDir.FullName;

                await ExecuteGraphicsMagick(fullName, violetDirFullName, "300");
                await ExecuteGraphicsMagick(fullName, violetDirFullName, "330");
                await ExecuteGraphicsMagick(fullName, violetDirFullName, "500");
                await ExecuteGraphicsMagick(fullName, violetDirFullName, "700");
                
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fullName);

                Image image = new(i == 0,
                    Path.Combine(rootFolder.Name, id.ToString(), $"{fileNameWithoutExtension}_300.webp"),
                    Path.Combine(rootFolder.Name, id.ToString(), $"{fileNameWithoutExtension}_330.webp"),
                    Path.Combine(rootFolder.Name, id.ToString(), $"{fileNameWithoutExtension}_500.webp"),
                    Path.Combine(rootFolder.Name, id.ToString(), $"{fileNameWithoutExtension}_700.webp"));
                
                images.Add(image);
            }

            Violet violet = new(id, name, breeder, description, tags.ToList(), date, images, chimera, colors.ToList());
            string output = JsonConvert.SerializeObject(violet);
            File.WriteAllText(Path.Combine(violetDir.FullName, $"{id}.json"), output);

            string[] newFiles = Directory.GetFiles(violetDir.FullName).ToArray();

            BufferedCommandResult gitAddResult = await Cli
                .Wrap("git")
                .WithWorkingDirectory(solutionPath)
                .WithValidation(CommandResultValidation.None)
                .WithArguments($"add {String.Join(' ', newFiles)}")
                .ExecuteBufferedAsync();

            BufferedCommandResult gitCommitResult = await Cli
                .Wrap("git")
                .WithWorkingDirectory(solutionPath)
                .WithValidation(CommandResultValidation.None)
                .WithArguments("commit -m \"add new content\"")
                .ExecuteBufferedAsync();

            BufferedCommandResult gitPushResult = await Cli
                .Wrap("git")
                .WithWorkingDirectory(solutionPath)
                .WithValidation(CommandResultValidation.None)
                .WithArguments("push")
                .ExecuteBufferedAsync();

            Console.WriteLine(gitAddResult.StandardError);
            Console.WriteLine(gitAddResult.StandardOutput);
            
            Console.WriteLine(gitCommitResult.StandardError);
            Console.WriteLine(gitCommitResult.StandardOutput);

            Console.WriteLine(gitPushResult.StandardError);
            Console.WriteLine(gitPushResult.StandardOutput);

            Console.WriteLine("Success!");
        });

        return await rootCommand.InvokeAsync(args);
    }

    private static async Task ExecuteGraphicsMagick(string fullName, string violetDirFullName, string width)
    {
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fullName);
        string arguments = $"-convert \"{fullName}\" -resize {width}x \"{Path.Combine(violetDirFullName, $"{fileNameWithoutExtension}_{width}.webp")}\"";
        await Cli.Wrap("gm")
            .WithArguments(arguments)
            .ExecuteBufferedAsync();
    }
}

public static class StringExtensions
{
    public static string FirstCharToUpper(this string input) =>
        input switch
        {
            null => throw new ArgumentNullException(nameof(input)),
            "" => throw new ArgumentException($"{nameof(input)} cannot be empty", nameof(input)),
            _ => string.Concat(input[0].ToString().ToUpper(), input.AsSpan(1))
        };
}