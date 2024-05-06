namespace SharedLibrary;

public class WarehouseVioletItem(
    Guid violetId,
    int leafCount,
    double leafPrice,
    int childCount,
    double childPrice,
    int wholePlantCount,
    double wholePlantPrice)
{
    public Guid VioletId { get; set; } = violetId;
    public int LeafCount { get; set; } = leafCount;
    public double LeafPrice { get; set; } = leafPrice;
    public int ChildCount { get; set; } = childCount;
    public double ChildPrice { get; set; } = childPrice;
    public int WholePlantCount { get; set; } = wholePlantCount;
    public double WholePlantPrice { get; set; } = wholePlantPrice;
}