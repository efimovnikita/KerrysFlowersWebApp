using System.ComponentModel;

namespace SharedLibrary;

public enum VioletSize
{
    [Description("Мини")]
    Mini, 

    [Description("Полумини")]
    Midi, 

    [Description("Стандарт")]
    Standard, 

    [Description("Малый стандарт")]
    SmallStandard
}