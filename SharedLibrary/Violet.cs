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
        List<VioletColor> colors)
    {
        Name = name;
        Breeder = breeder;
        Description = description;
        Tags = tags;
        BreedingDate = breedingDate;
        Images = images;
        IsChimera = isChimera;
        Colors = colors;
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
}

public class Filter
{
    public Filter(List<Tag> tags, List<ColorTag> colorTags, List<BreederTag> breederTags, List<YearTag> yearTags,
        List<ChimeraTag> chimeraTags)
    {
        Tags = tags;
        ColorTags = colorTags;
        BreederTags = breederTags;
        YearTags = yearTags;
        ChimeraTags = chimeraTags;
    }

    public List<Tag> Tags { get; set; }
    public List<ColorTag> ColorTags { get; set; }
    public List<BreederTag> BreederTags { get; set; }
    public List<YearTag> YearTags { get; set; }
    public List<ChimeraTag> ChimeraTags { get; set; }
}

public class ColorTag : Tag
{
    public ColorTag(string name, bool active) : base(name)
    {
    }
}

public class BreederTag : Tag
{
    public BreederTag(string name, bool active) : base(name)
    {
    }
}

public class YearTag : Tag
{
    public YearTag(string name, bool active) : base(name)
    {
    }
}

public class ChimeraTag : Tag
{
    public ChimeraTag(string name, bool active) : base(name)
    {
    }
}

public class Tag
{
    public Tag(string name)
    {
        Name = name;
    }

    public bool Active { get; set; } = true;
    public string Name { get; set; }
}