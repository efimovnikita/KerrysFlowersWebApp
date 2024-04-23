namespace SharedLibrary;

[Serializable] 
public class Image(bool active, string original, string w300, string w330, string w500, string w700)
{
    public bool Active { get; set; } = active;
    public string W300 { get; set; } = w300;
    public string W330 { get; set; } = w330;
    public string W500 { get; set; } = w500;
    public string W700 { get; set; } = w700;
    public string Original { get; set; } = original;
}