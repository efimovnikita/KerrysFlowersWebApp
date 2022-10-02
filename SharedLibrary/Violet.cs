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
    public bool NewLabel { get; set; } = true;
    public List<Image> Images { get; set; }
    public DateTime BreedingDate { get; set; }
    public DateTime PublishDate { get; set; } = DateTime.Now;
    public bool IsChimera { get; set; }

    public List<VioletColor> Colors { get; set; }
}