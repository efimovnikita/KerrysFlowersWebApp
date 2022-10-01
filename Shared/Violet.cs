namespace Shared;

[Serializable]
public class Violet
{
    public string Name { get; set; }
    public string Breeder { get; set; }
    public string Description { get; set; }
    public List<string> Tags { get; set; }
    public bool New { get; set; }
    public List<Image> Images { get; set; } = new(3);
    public DateTime Date { get; set; }
}