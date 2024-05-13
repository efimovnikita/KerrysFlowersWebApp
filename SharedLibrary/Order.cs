namespace SharedLibrary;

public class Order(OrderedViolet[] orderedViolets)
{
    public OrderedViolet[] Violets { get; set; } = orderedViolets.Where(violet => violet.HasSomeOrderedParts).ToArray();
    public string PhoneNumber { get; set; } = "";
    public string Name { get; set; } = "";
    public string Address { get; set; } = "";
    public string Email { get; set; } = "";
    public double TotalPrice
    {
        get
        {
            double totalPrice = 0;
            foreach (var violet in Violets)
            {
                totalPrice += violet.CalculatedTotalVioletPrice;
            }
            return totalPrice;
        }
    }
}