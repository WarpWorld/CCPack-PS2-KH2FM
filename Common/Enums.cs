namespace CrowdControl.Games.Packs.KH2FM;

public enum EffectFunction
{
    TryEffect,
    StartTimed,
    RepeatAction
}

public enum Category
{
    None = 0,
    Enemy = 1,
    Environment = 2,
    Item = 3,
    ModelSwap = 4,
    Party = 5,
    Sora = 6,
    Equipment = 6,
}

public enum DataType
{
    None = 0,

    Binary = 1,
    Byte = 2,
    TwoBytes = 3,
    FourBytes = 4,
    EightBytes = 5,
    Float = 6,
    Double = 7,
    String = 8,
    ByteArray = 9
}

public enum ManipulationType
{
    None = 0,

    Set = 1,
    Add = 2,
    Subtract = 3
}

public enum SubCategory
{
    None = 0,

    Accessory = 1,
    Armor = 2,
    BaseItem = 3,
    Munny = 4,
    Weapon = 5, // Weapon to be used for Party Members - Moved down to be more specific - Keyblade, Staff, Shield, Weapon (Party)
    Ability = 6,
    Drive = 7,
    QuickMenu = 8,
    Magic = 9,
    Stats = 10,
    Summon = 11,
    Keyblade = 12,
    Staff = 13,
    Shield = 14,
    Friend = 15,
    Enemy = 16
}