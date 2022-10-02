using System.CommandLine;
using Newtonsoft.Json;
using SharedLibrary;

namespace VioletMaker;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        Option<string> nameOption = new("--name", "Violet name") {IsRequired = true};
        nameOption.AddAlias("-n");
        Option<string> breederOption = new("--breeder", "Breeder name") {IsRequired = true};
        breederOption.AddAlias("-b");
        Option<string> descriptionOption = new("--description", "Violet description") {IsRequired = true};
        descriptionOption.AddAlias("-d");
        Option<IEnumerable<string>> tagsOption = new("--tags", "Violet tags")
            {AllowMultipleArgumentsPerToken = true, IsRequired = true};
        tagsOption.AddAlias("-t");
        Option<DateTime> breedDateOption = new("--date", "Violet breeding date") {IsRequired = true};
        Option<FileInfo> image1Option = new("--image1", "First image of violet") {IsRequired = true};
        Option<FileInfo> image2Option = new("--image2", "Second image of violet") {IsRequired = true};
        Option<FileInfo> image3Option = new("--image3", "Third image of violet") {IsRequired = true};
        Option<bool> chimeraOption = new("--chimera", "Is this violet chimera?") {IsRequired = true};
        Option<IEnumerable<VioletColor>> colorsOption = new("--colors", "Violet colors") {AllowMultipleArgumentsPerToken = true, IsRequired = true};
        Option<FileInfo> rootOption = new("--root", "Root folder for new violet") {IsRequired = true};
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

        rootCommand.SetHandler(context =>
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
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileInfo.FullName);
                string extension = Path.GetExtension(fileInfo.FullName);
                string threeHundred = $"{fileNameWithoutExtension}_300{extension}";
                string threeHundredAndThirty = $"{fileNameWithoutExtension}_330{extension}";
                string fiveHundred = $"{fileNameWithoutExtension}_500{extension}";
                string sevenHundred = $"{fileNameWithoutExtension}_700{extension}";

                string threeHundredPath = Path.Combine(violetDir.FullName, threeHundred);
                string threeHundredAndThirtyPath = Path.Combine(violetDir.FullName, threeHundredAndThirty);
                string fiveHundredPath = Path.Combine(violetDir.FullName, fiveHundred);
                string sevenHundredPath = Path.Combine(violetDir.FullName, sevenHundred);

                File.Copy(fileInfo.FullName, threeHundredPath);
                File.Copy(fileInfo.FullName, threeHundredAndThirtyPath);
                File.Copy(fileInfo.FullName, fiveHundredPath);
                File.Copy(fileInfo.FullName, sevenHundredPath);

                Image image = new(i == 0,
                    Path.Combine(rootFolder.Name, id.ToString(), threeHundred),
                    Path.Combine(rootFolder.Name, id.ToString(), threeHundredAndThirty),
                    Path.Combine(rootFolder.Name, id.ToString(), fiveHundred),
                    Path.Combine(rootFolder.Name, id.ToString(), sevenHundred)); 
                
                images.Add(image);
            }

            Violet violet = new(id, name, breeder, description, tags.ToList(), date, images, chimera, colors.ToList());
            string output = JsonConvert.SerializeObject(violet);
            File.WriteAllText(Path.Combine(violetDir.FullName, $"{id}.json"), output);
            
            Console.WriteLine("Success!");
        });

        return await rootCommand.InvokeAsync(args);
    }
}