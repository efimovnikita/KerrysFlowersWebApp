namespace Shared;

[Serializable]
public class Violet
{
    public Violet(string name, string breeder, string description, List<string> tags, bool newLabel, DateTime date,
        List<Image> images)
    {
        Name = name;
        Breeder = breeder;
        Description = description;
        Tags = tags;
        NewLabel = newLabel;
        Date = date;
        Images = images;
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; set; }
    public string Breeder { get; set; }
    public string Description { get; set; }
    public List<string> Tags { get; set; }
    public bool NewLabel { get; set; }
    public List<Image> Images { get; set; }
    public DateTime Date { get; set; }
}