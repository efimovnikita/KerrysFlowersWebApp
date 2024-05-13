namespace SharedLibrary;

[Serializable]
public class WarehouseVioletItem
{
    public WarehouseVioletItem(Guid violetId,
        int leafCount,
        double leafPrice,
        int childCount,
        double childPrice,
        int wholePlantCount,
        double wholePlantPrice)
    {
        VioletId = violetId;
        LeafCount = leafCount;
        LeafPrice = leafPrice;
        ChildCount = childCount;
        ChildPrice = childPrice;
        WholePlantCount = wholePlantCount;
        WholePlantPrice = wholePlantPrice;
    }
    
    public Guid Id { get; set; }
    public Guid VioletId { get; set; }
    public int LeafCount { get; set; }
    public double LeafPrice { get; set; }
    public int ChildCount { get; set; }
    public double ChildPrice { get; set; }
    public int WholePlantCount { get; set; }
    public double WholePlantPrice { get; set; }
}