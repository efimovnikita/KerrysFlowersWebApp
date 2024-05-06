namespace SharedLibrary;

public class StoredViolet(Violet violet, WarehouseVioletItem warehouseItem) : Violet(violet.Id, violet.Name,
    violet.Breeder, violet.Description, violet.Tags,
    violet.BreedingDate, violet.Images, violet.IsChimera, violet.Colors, violet.Size)
{
    public int LeafCount { get; set; } = warehouseItem.LeafCount;
    public double LeafPrice { get; set; } = warehouseItem.LeafPrice;
    public int ChildCount { get; set; } = warehouseItem.ChildCount;
    public double ChildPrice { get; set; } = warehouseItem.ChildPrice;
    public int WholePlantCount { get; set; } = warehouseItem.WholePlantCount;
    public double WholePlantPrice { get; set; } = warehouseItem.WholePlantPrice;
}
