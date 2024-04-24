using Humanizer;
using NickBuhro.Translit;

namespace SharedLibrary;

[Serializable]
public class Violet
{
    public Violet(Guid id, string name,
        string breeder,
        string description,
        List<string> tags,
        DateTime breedingDate,
        List<Image> images, 
        bool isChimera, 
        List<VioletColor> colors, VioletSize size)
    {
        Name = name;
        Breeder = breeder;
        Description = description;
        Tags = tags;
        BreedingDate = breedingDate;
        Images = images;
        IsChimera = isChimera;
        Colors = colors;
        Size = size;
        Id = id;
    }

    public Guid Id { get; set; }
    public string Name { get; set; }

    public string TransliteratedName => Transliteration.CyrillicToLatin(Name).ToLower().Replace(' ', '_');

    public string Breeder { get; set; }
    public string Description { get; set; }
    public List<string> Tags { get; set; }
    public bool NewLabel => DateTime.Now < PublishDate + TimeSpan.FromDays(10);
    public List<Image> Images { get; set; }
    public DateTime BreedingDate { get; set; }
    public DateTime PublishDate { get; set; } = DateTime.Now;
    public bool IsChimera { get; set; }
    public List<VioletColor> Colors { get; set; }
    public VioletSize Size { get; set; }

    public override string ToString()
    {
        return ToString("");
    }

    public string ToString(string format)
    {
        if (String.IsNullOrEmpty(format)) format = "G";

        switch (format.ToUpperInvariant())
        {
            case "G": // General format
                return $"""
                        Имя: {Name}
                        Описание: {Description.Truncate(100)}
                        Год селекции: {BreedingDate:yyyy}
                        Дата публикации: {PublishDate:dd.MM.yyyy}
                        Селекционер: {Breeder}
                        Размер: {ExtensionMethods.GetEnumDescription(Size).ToLowerInvariant()}
                        Теги: {string.Join(", ", Tags.Select(s => s.ToLowerInvariant()))}
                        Цвета: {string.Join(", ", Colors.Select(color => ExtensionMethods.GetEnumDescription(color).ToLowerInvariant()))}
                        Химера: {(IsChimera ? "да" : "нет")}
                        """;
            case "M": // Markdown format
                return $"""
                        *Имя:* {Name}
                        *Описание:* {Description.Truncate(100)}
                        *Год селекции:* {BreedingDate:yyyy}
                        *Дата публикации:* {PublishDate:dd.MM.yyyy}
                        *Селекционер:* {Breeder}
                        *Размер:* {ExtensionMethods.GetEnumDescription(Size).ToLowerInvariant()}
                        *Теги:* {string.Join(", ", Tags.Select(s => s.ToLowerInvariant()))}
                        *Цвета:* {string.Join(", ", Colors.Select(color => ExtensionMethods.GetEnumDescription(color).ToLowerInvariant()))}
                        *Химера:* {(IsChimera ? "да" : "нет")}
                        """;
            case "N": // Name only
                return Name;
            default:
                throw new FormatException($"The {format} format string is not supported.");
        }
    }
}

public class Filter
{
    public Filter(List<Tag> tags, List<Tag> colorTags, List<Tag> breederTags, List<Tag> yearTags,
        List<Tag> chimeraTags, List<Tag> sizeTags)
    {
        Tags = tags;
        ColorTags = colorTags;
        BreederTags = breederTags;
        YearTags = yearTags;
        ChimeraTags = chimeraTags;
        SizeTags = sizeTags;
    }

    public List<Tag> Tags { get; set; }
    public List<Tag> ColorTags { get; set; }
    public List<Tag> BreederTags { get; set; }
    public List<Tag> SizeTags { get; set; }
    public List<Tag> YearTags { get; set; }
    public List<Tag> ChimeraTags { get; set; }
}

public class Tag
{
    public Tag(string name)
    {
        Name = name;
    }

    public bool Active { get; set; }
    public string Name { get; set; }
}

public class FiltersPanelVisibility
{
    public bool Invisible { get; set; } = true;
    public bool TagsVisible { get; set; }
    public bool ColorsVisible { get; set; } 
    public bool BreederVisible { get; set; }
    public bool YearsVisible { get; set; }
    public bool ChimeraVisible { get; set; }
    public bool SizesVisible { get; set; }
}