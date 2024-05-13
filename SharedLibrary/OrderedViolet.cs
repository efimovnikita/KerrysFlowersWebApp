using System.Globalization;
using Humanizer;

namespace SharedLibrary;

public class OrderedViolet(StoredViolet storedViolet)
{
    public string Name { get; } = storedViolet.Name;
    private double LeafPrice { get; } = storedViolet.LeafPrice;
    public int SelectedLeafs { get; } = storedViolet.SelectedLeafs;
    private double ChildPrice { get; } = storedViolet.ChildPrice;
    public int SelectedChildren { get; } = storedViolet.SelectedChildren;
    private double WholePlantPrice { get; } = storedViolet.WholePlantPrice;
    public int SelectedWholePlants { get; } = storedViolet.SelectedWholePlants;
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
    
    public override string ToString()
    {
        var culture = new CultureInfo("ru-RU");
        var result = $"Фиалка {Name}:" + Environment.NewLine;

        if (SelectedLeafs > 0)
        {
            result += $"Листья: {SelectedLeafs.ToWords(culture)} шт. по {LeafPrice.ToString("C", culture)} каждый, всего {CalculatedLeafsPrice.ToString("C", culture)}." + Environment.NewLine;
        }
        
        if (SelectedChildren > 0)
        {
            result += $"Детки: {SelectedChildren.ToWords(culture)} шт. по {ChildPrice.ToString("C", culture)} каждая, всего {CalculatedChildrenPrice.ToString("C", culture)}." + Environment.NewLine;
        }
        
        if (SelectedWholePlants > 0)
        {
            result += $"Целые растения: {SelectedWholePlants.ToWords(culture)} шт. по {WholePlantPrice.ToString("C", culture)} каждое, всего {CalculatedWholePlantsPrice.ToString("C", culture)}." + Environment.NewLine;
        }
        
        result += $"Итоговая цена: {CalculatedTotalVioletPrice}";

        return result;
    }
}