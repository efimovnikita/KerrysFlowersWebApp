namespace SharedLibrary;

public class StoredViolet(Violet violet, WarehouseVioletItem warehouseItem = null) : Violet(violet.Id, violet.Name,
    violet.Breeder, violet.Description, violet.Tags,
    violet.BreedingDate, violet.Images, violet.IsChimera, violet.Colors, violet.Size)
{
    private const int DefaultCount = 0;
    private const double DefaultPrice = 0.0;

    public int LeafCount { get; set; } = warehouseItem?.LeafCount ?? DefaultCount;
    private double LeafPrice { get; set; } = warehouseItem?.LeafPrice ?? DefaultPrice;
    public int SelectedLeafs { get; set; }
    public int ChildCount { get; set; } = warehouseItem?.ChildCount ?? DefaultCount;
    private double ChildPrice { get; set; } = warehouseItem?.ChildPrice ?? DefaultPrice;
    public int SelectedChildren { get; set; }
    public int WholePlantCount { get; set; } = warehouseItem?.WholePlantCount ?? DefaultCount;
    private double WholePlantPrice { get; set; } = warehouseItem?.WholePlantPrice ?? DefaultPrice;
    public int SelectedWholePlants { get; set; }

    public VioletPurchaseOption[] PurchaseOptions
    {
        get
        {
            var result = new List<VioletPurchaseOption>();

            if (IsLeafsInfoValid())
            {
                result.Add(VioletPurchaseOption.Leaf);
            }
            
            if (IsChildrenInfoValid())
            {
                result.Add(VioletPurchaseOption.Offshoot);
            }
            
            if (IsWholePlantInfoValid())
            {
                result.Add(VioletPurchaseOption.WholePlant);
            }

            return result.ToArray();
        }
    }

    public bool IsLeafsInfoValid() => LeafCount != 0 && LeafPrice != 0.0;
    public bool IsChildrenInfoValid() => ChildCount != 0 && ChildPrice != 0.0;
    public bool IsWholePlantInfoValid() => WholePlantCount != 0 && WholePlantPrice != 0.0;
    public bool IsWarehouseInfoValid() => IsLeafsInfoValid() || IsChildrenInfoValid() || IsWholePlantInfoValid();
    public double CalculatedLeafsPrice => SelectedLeafs * LeafPrice;
    public double CalculatedChildrenPrice => SelectedChildren * ChildPrice;
    public double CalculatedWholePlantsPrice => SelectedWholePlants * WholePlantPrice;
    public double CalculatedTotalVioletPrice =>
        CalculatedLeafsPrice + CalculatedChildrenPrice + CalculatedWholePlantsPrice;
}

