using System.ComponentModel;

namespace SharedLibrary;

public enum VioletColor
{
    [Description("Желтый")]
    Yellow, 
    
    [Description("Желто-зеленый")]
    YellowGreen,
    
    [Description("Зеленый")]
    Green, 
    
    [Description("Сине-зеленый")]
    BlueGreen, 
    
    [Description("Синий")]
    Blue, 
    
    [Description("Сине-фиолетовый")]
    BlueViolet, 
    
    [Description("Фиолетовый")]
    Violet, 
    
    [Description("Красно-фиолетовый")]
    RedViolet, 
    
    [Description("Красный")]
    Red, 
    
    [Description("Красно-оранжевый")]
    RedOrange, 
    
    [Description("Оранжевый")]
    Orange, 
    
    [Description("Желто-оранжевый")]
    YellowOrange, 
    
    [Description("Белый")]
    White
}