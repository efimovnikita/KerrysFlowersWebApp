namespace SharedLibrary;

[Serializable] 
public class Image
{
    public Image(bool active, string w300, string w330, string w500, string w700)
    {
        Active = active;
        W300 = w300;
        W330 = w330;
        W500 = w500;
        W700 = w700;
    }

    public bool Active { get; set; }
    public string W300 { get; set; }
    public string W330 { get; set; }
    public string W500 { get; set; }
    public string W700 { get; set; }
}