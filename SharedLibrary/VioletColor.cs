using System.ComponentModel;

namespace SharedLibrary;
public enum VioletColor
{
    [Description("Желтый")]
    Yellow, 
    
    [Description("Зеленый")]
    Green,

    [Description("Синий")]
    Blue,

    [Description("Красный")]
    Red,

    [Description("Белый")]
    White,
    
    [Description("Розовый")]
    Pink,
    
    [Description("Голубой")]
    LightBlue,
    
    [Description("Персиковый")]
    Peach,

    [Description("Сиреневый")]
    Lilac,

    [Description("Фиолетовый")]
    Purple,

    [Description("Бордовый")]
    Burgundy
}