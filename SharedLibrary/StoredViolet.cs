namespace SharedLibrary;

public class StoredViolet(Violet violet, WarehouseVioletItem warehouseItem = null) : Violet(violet.Id, violet.Name,
    violet.Breeder, violet.Description, violet.Tags,
    violet.BreedingDate, violet.Images, violet.IsChimera, violet.Colors, violet.Size)
{
    private const int DefaultCount = 0;
    private const double DefaultPrice = 0.0;

    public int LeafCount { get; set; } = warehouseItem?.LeafCount ?? DefaultCount;
    public double LeafPrice { get; set; } = warehouseItem?.LeafPrice ?? DefaultPrice;
    public int ChildCount { get; set; } = warehouseItem?.ChildCount ?? DefaultCount;
    public double ChildPrice { get; set; } = warehouseItem?.ChildPrice ?? DefaultPrice;
    public int WholePlantCount { get; set; } = warehouseItem?.WholePlantCount ?? DefaultCount;
    public double WholePlantPrice { get; set; } = warehouseItem?.WholePlantPrice ?? DefaultPrice;

    public bool IsLeafsInfoValid() => LeafCount != 0 && LeafPrice != 0.0;
    public bool IsChildrenInfoValid() => ChildCount != 0 && ChildPrice != 0.0;
    public bool IsWholePlantInfoValid() => WholePlantCount != 0 && WholePlantPrice != 0.0;
    public bool IsWarehouseInfoValid() => IsLeafsInfoValid() || IsChildrenInfoValid() || IsWholePlantInfoValid();
}

