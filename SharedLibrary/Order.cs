using MongoDB.Bson.Serialization.Attributes;

namespace SharedLibrary;

public class Order(OrderedViolet[] orderedViolets)
{
    public Guid Id { get; set; }
    public DateTime Date { get; set; } = DateTime.Now;
    public string Name { get; set; } = "";
    public string PhoneNumber { get; set; } = "";
    public string Address { get; set; } = "";
    public string Email { get; set; } = "";
    public bool Active { get; set; } = true;
    public OrderedViolet[] Violets { get; set; } = orderedViolets;
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