using CrowdControl.Common;

namespace CrowdControl.Games.Packs.KH2FM;

public static class MiscAddresses {
    public static uint None = 0x0;

    #region Munny
    public static uint Munny = 0x2032DF70;
    #endregion Munny

    public static uint WeaponSize = 0x2036CED0;
    public static uint WeaponSizeAlt = 0x2036CECC; // TODO Is this the right one?
    public static uint JumpAmount = 0x20191C70;

    public static Dictionary<uint, byte> MakeInventoryDictionary() {
        Dictionary<uint, byte> inventory = new Dictionary<uint, byte>();
        inventory.AddRange(BaseItemAddresses.MakeBaseItemsDictionary());
        inventory.AddRange(AccessoryAddresses.MakeAccessoriesDictionary());
        inventory.AddRange(ArmorAddresses.MakeArmorDictionary());
        inventory.AddRange(KeybladeAddresses.MakeKeybladesDictionary());

        return inventory;
    }
}