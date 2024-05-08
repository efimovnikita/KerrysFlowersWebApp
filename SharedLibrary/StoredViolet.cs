namespace SharedLibrary;

public class StoredViolet(Violet violet, WarehouseVioletItem warehouseItem = null) : Violet(violet.Id, violet.Name,
    violet.Breeder, violet.Description, violet.Tags,
    violet.BreedingDate, violet.Images, violet.IsChimera, violet.Colors, violet.Size)
{
    private const int DefaultCount = 0;
    private const double DefaultPrice = 0.0;

    private int LeafCount { get; set; } = warehouseItem?.LeafCount ?? DefaultCount;
    private double LeafPrice { get; set; } = warehouseItem?.LeafPrice ?? DefaultPrice;
    private int SelectedLeafs { get; set; }
    private int ChildCount { get; set; } = warehouseItem?.ChildCount ?? DefaultCount;
    private double ChildPrice { get; set; } = warehouseItem?.ChildPrice ?? DefaultPrice;
    private int SelectedChildren { get; set; }
    private int WholePlantCount { get; set; } = warehouseItem?.WholePlantCount ?? DefaultCount;
    private double WholePlantPrice { get; set; } = warehouseItem?.WholePlantPrice ?? DefaultPrice;
    private int SelectedWholePlants { get; set; }

    public bool IsLeafsInfoValid() => LeafCount != 0 && LeafPrice != 0.0;
    public bool IsChildrenInfoValid() => ChildCount != 0 && ChildPrice != 0.0;
    public bool IsWholePlantInfoValid() => WholePlantCount != 0 && WholePlantPrice != 0.0;
    public bool IsWarehouseInfoValid() => IsLeafsInfoValid() || IsChildrenInfoValid() || IsWholePlantInfoValid();

    public double CalcSelectedLeafsPrice() => SelectedLeafs * LeafCount;
    public double CalcSelectedChildrenPrice() => SelectedChildren * ChildPrice;
    public double CalcSelectedWholePlantsPrice() => SelectedWholePlants * WholePlantPrice;

    public double CalcTotalPriceForViolet() =>
        CalcSelectedLeafsPrice() + CalcSelectedChildrenPrice() + CalcSelectedWholePlantsPrice();
    public void IncreaseNumberOfSelectedLeafs()
    {
        if (SelectedLeafs + 1 > LeafCount)
        {
            return;
        }

        SelectedLeafs += 1;
    }
    
    public void DecreaseNumberOfSelectedLeafs()
    {
        if (SelectedLeafs == 0)
        {
            return;
        }

        SelectedLeafs -= 1;
    }
    
    public void IncreaseNumberOfSelectedChildren()
    {
        if (SelectedChildren + 1 > ChildCount)
        {
            return;
        }

        SelectedChildren += 1;
    }
    
    public void DecreaseNumberOfSelectedChildren()
    {
        if (SelectedChildren == 0)
        {
            return;
        }

        SelectedChildren -= 1;
    }
    
    public void IncreaseNumberOfSelectedWholePlants()
    {
        if (SelectedWholePlants + 1 > WholePlantCount)
        {
            return;
        }

        SelectedWholePlants += 1;
    }
    
    public void DecreaseNumberOfSelectedWholePlants()
    {
        if (SelectedWholePlants == 0)
        {
            return;
        }

        SelectedWholePlants -= 1;
    }
}

