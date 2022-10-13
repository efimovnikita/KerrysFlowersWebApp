using System.Diagnostics.CodeAnalysis;

namespace PushContentLocator;

[SuppressMessage("ReSharper", "InconsistentNaming")]
[Serializable]public class PushEvent
{
    public string type { get; set; }
    public Repo repo { get; set; }
    public DateTime created_at { get; set; }
}