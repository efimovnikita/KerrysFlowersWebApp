namespace SharedLibrary;

public class WarehouseVioletItem
{
    public Guid VioletId { get; set; }
    public int LeafCount { get; set; }
    public double LeafPrice { get; set; }
    public int ChildCount { get; set; }
    public double ChildPrice { get; set; }
    public int WholePlantCount { get; set; }
    public double WholePlantPrice { get; set; }
}