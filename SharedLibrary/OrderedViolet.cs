namespace SharedLibrary;

public class OrderedViolet(StoredViolet storedViolet)
{
    public string Name { get; set; } = storedViolet.Name;
    public Guid VioletId { get; set; } = storedViolet.Id;
    public double LeafPrice { get; set; } = storedViolet.LeafPrice;
    public int SelectedLeafs { get; set; } = storedViolet.SelectedLeafs;
    public double ChildPrice { get; set; } = storedViolet.ChildPrice;
    public int SelectedChildren { get; set; } = storedViolet.SelectedChildren;
    public double WholePlantPrice { get; set; } = storedViolet.WholePlantPrice;
    public int SelectedWholePlants { get; set; } = storedViolet.SelectedWholePlants;
    public bool HasSomeOrderedParts
    {
        get
        {
            if (SelectedLeafs > 0)
            {
                return true;
            }

            if (SelectedChildren > 0)
            {
                return true;
            }
            
            if (SelectedWholePlants > 0)
            {
                return true;
            }

            return false;
        }
    }
    public double CalculatedLeafsPrice => SelectedLeafs * LeafPrice;
    public double CalculatedChildrenPrice => SelectedChildren * ChildPrice;
    public double CalculatedWholePlantsPrice => SelectedWholePlants * WholePlantPrice;
    public double CalculatedTotalVioletPrice =>
        CalculatedLeafsPrice + CalculatedChildrenPrice + CalculatedWholePlantsPrice;
}