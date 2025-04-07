namespace CrowdControl.Games.Packs.KH2FM;

public static class BaseItemAddresses {
    public static int Potion = 0x2032F0B0;
    public static int HiPotion = 0x2032F0B1;
    public static int Ether = 0x2032F0B2;
    public static int Elixir = 0x2032F0B3;
    public static int MegaPotion = 0x2032F0B4;
    public static int MegaEther = 0x2032F0B5;
    public static int Megalixir = 0x2032F0B6;
    public static int Tent = 0x2032F111;
    public static int DriveRecovery = 0x2032F194;
    public static int HighDriveRecovery = 0x2032F195;
    public static int PowerBoost = 0x2032F196;
    public static int MagicBoost = 0x2032F197;
    public static int DefenseBoost = 0x2032F198;
    public static int APBoost = 0x2032F199;

    /// <summary>
    /// Creates a dictionary of all base items (or consumable items), each with a quantity of 0.
    /// You can use this to reset the quantity of all base items to 0
    /// or to store the quantities of all base items.
    /// 
    /// Key: Memory address for the base item quantity in inventory
    /// Value: Quantity of the base item in inventory. Initialized to zero.
    /// 
    /// </summary>
    /// <returns>a dictionary with each base item address and a value of zero</returns>
    public static Dictionary<uint, byte> MakeBaseItemsDictionary() {
        return new Dictionary<uint, byte> {
            { (uint)Potion, 0 },
            { (uint)HiPotion, 0 },
            { (uint)Ether, 0 },
            { (uint)Elixir, 0 },
            { (uint)MegaPotion, 0 },
            { (uint)MegaEther, 0 },
            { (uint)Megalixir, 0 },
            { (uint)Tent, 0 },
            { (uint)DriveRecovery, 0 },
            { (uint)HighDriveRecovery, 0 },
            { (uint)PowerBoost, 0 },
            { (uint)MagicBoost, 0 },
            { (uint)DefenseBoost, 0 },
            { (uint)APBoost, 0 },
        };
    }
}