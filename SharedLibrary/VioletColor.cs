using System.ComponentModel;

namespace SharedLibrary;
// +белый, +розовый, +голубой, +синий, +красный, +желтый, +персиковый, +зеленый
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
    Peach
}