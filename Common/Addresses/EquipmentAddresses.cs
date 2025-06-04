namespace CrowdControl.Games.Packs.KH2FM;

public static class EquipmentAddresses {
    public static uint SoraWeaponSlot = 0x2032E020;
    public static uint SoraValorWeaponSlot = 0x2032EE24;
    public static uint SoraMasterWeaponSlot = 0x2032EECC;
    public static uint SoraFinalWeaponSlot = 0x2032EF04;
    public static uint DonaldWeaponSlot = 0x2032E134;
    public static uint GoofyWeaponSlot = 0x2032E248;


    public static uint SoraArmorSlotCount = 0x2032E030;
    public static uint SoraArmorSlot1 = 0x2032E034;
    public static uint SoraArmorSlot2 = 0x2032E036;
    public static uint SoraArmorSlot3 = 0x2032E038;
    public static uint SoraArmorSlot4 = 0x2032E03A;
    public static uint SoraAccessorySlotCount = 0x2032E031;
    public static uint SoraAccessorySlot1 = 0x2032E044;
    public static uint SoraAccessorySlot2 = 0x2032E046;
    public static uint SoraAccessorySlot3 = 0x2032E048;
    public static uint SoraAccessorySlot4 = 0x2032E04A;
    public static uint SoraItemSlotCount = 0x2032E032;
    public static uint SoraItemSlot1 = 0x2032E054;
    public static uint SoraItemSlot2 = 0x2032E056;
    public static uint SoraItemSlot3 = 0x2032E058;
    public static uint SoraItemSlot4 = 0x2032E05A;
    public static uint SoraItemSlot5 = 0x2032E05C;
    public static uint SoraItemSlot6 = 0x2032E05E;
    public static uint SoraItemSlot7 = 0x2032E060;
    public static uint SoraItemSlot8 = 0x2032E062;
    public static uint SoraQuickMenuSlot1 = 0x2032F228;
    public static uint SoraQuickMenuSlot2 = 0x2032F22A;
    public static uint SoraQuickMenuSlot3 = 0x2032F22C;
    public static uint SoraQuickMenuSlot4 = 0x2032F22E;
    public static uint SoraAbilityStart = 0x2032E074;

    /// <summary>
    /// Creates a dictionary of all of Sora's inventory slots, each with a quantity of 0.
    /// You can use this to unequip all of Sora's armor, accessories, keyblades, and items,
    /// or to store the quantities of that inventory.
    /// 
    /// Key: Memory address for the inventory slot
    /// Value: Equipment, item, or accessory assigned to the slot. 
    //  Initialized to Kingdom Key for keyblade slots and zero for everything else.
    /// 
    /// </summary>
    /// <returns>a dictionary with each inventory address and a value of zero</returns>
    public static Dictionary<uint, ushort> MakeSoraInventorySlotsDictionary() {
        return new Dictionary<uint, ushort> {
            { SoraWeaponSlot, (ushort) KeybladeValues.KingdomKey },
            { SoraValorWeaponSlot, (ushort) KeybladeValues.KingdomKey },
            { SoraMasterWeaponSlot, (ushort) KeybladeValues.KingdomKey },
            { SoraFinalWeaponSlot, (ushort) KeybladeValues.KingdomKey },
            { SoraArmorSlot1, 0 },
            { SoraArmorSlot2, 0 },
            { SoraArmorSlot3, 0 },
            { SoraArmorSlot4, 0 },
            { SoraAccessorySlot1, 0 },
            { SoraAccessorySlot2, 0 },
            { SoraAccessorySlot3, 0 },
            { SoraAccessorySlot4, 0 },
            { SoraItemSlot1, 0 },
            { SoraItemSlot2, 0 },
            { SoraItemSlot3, 0 },
            { SoraItemSlot4, 0 },
            { SoraItemSlot5, 0 },
            { SoraItemSlot6, 0 },
            { SoraItemSlot7, 0 },
            { SoraItemSlot8, 0 },
        };
    }

    public static uint DonaldArmorSlotCount = 0x2032E144;
    public static uint DonaldArmorSlot1 = 0x2032E148;
    public static uint DonaldArmorSlot2 = 0x2032E14A;
    public static uint DonaldAccessorySlotCount = 0x2032E145;
    public static uint DonaldAccessorySlot1 = 0x2032E158;
    public static uint DonaldAccessorySlot2 = 0x2032E15A;
    public static uint DonaldAccessorySlot3 = 0x2032E15C;
    public static uint DonaldItemSlotCount = 0x2032E146;
    public static uint DonaldItemSlot1 = 0x2032E168;
    public static uint DonaldItemSlot2 = 0x2032E16A;
    public static uint DonaldAbilityStart = 0x2032E188; 
    // First ability is at: 0x2032E18C

    public static uint GoofyArmorSlotCount = 0x2032E258;
    public static uint GoofyArmorSlot1 = 0x2032E25C;
    public static uint GoofyArmorSlot2 = 0x2032E25E;
    public static uint GoofyArmorSlot3 = 0x2032E260;
    public static uint GoofyAccessorySlotCount = 0x2032E259;
    public static uint GoofyAccessorySlot1 = 0x2032E26C;
    public static uint GoofyAccessorySlot2 = 0x2032E26E;
    public static uint GoofyItemSlotCount = 0x2032E25A;
    public static uint GoofyItemSlot1 = 0x2032E27C;
    public static uint GoofyItemSlot2 = 0x2032E27E;
    public static uint GoofyItemSlot3 = 0x2032E280;
    public static uint GoofyItemSlot4 = 0x2032E282;
    public static uint GoofyAbilityStart = 0x2032E29C;

    public static uint MulanArmorSlotCount = 0x2032E594;
    public static uint MulanArmorSlot1 = 0x2032E598;
    public static uint MulanAccessorySlotCount = 0x2032E595;
    public static uint MulanAccessorySlot1 = 0x2032E5A8;
    public static uint MulanItemSlotCount = 0x2032E596;
    public static uint MulanItemSlot1 = 0x2032E5B8;
    public static uint MulanItemSlot2 = 0x2032E5BA;
    public static uint MulanItemSlot3 = 0x2032E5BC;
    public static uint MulanAbilityStart = 0x2032E5D8;

    public static uint BeastAccessorySlotCount = 0x2032E8D1;
    public static uint BeastAccessorySlot1 = 0x2032E8E4;
    public static uint BeastItemSlotCount = 0x2032E8D2;
    public static uint BeastItemSlot1 = 0x2032E8F4;
    public static uint BeastItemSlot2 = 0x2032E8F6;
    public static uint BeastItemSlot3 = 0x2032E8F8;
    public static uint BeastItemSlot4 = 0x2032E8FA;
    public static uint BeastAbilityStart = 0x2032E914;

    public static uint AuronArmorSlotCount = 0x2032E480;
    public static uint AuronArmorSlot1 = 0x2032E484;
    public static uint AuronItemSlotCount = 0x2032E482;
    public static uint AuronItemSlot1 = 0x2032E4A4;
    public static uint AuronItemSlot2 = 0x2032E4A6;
    public static uint AuronAbilityStart = 0x2032E4C4;

    public static uint CaptainJackSparrowArmorSlotCount = 0x2032E7BC;
    public static uint CaptainJackSparrowArmorSlot1 = 0x2032E7C0;
    public static uint CaptainJackSparrowAccessorySlotCount = 0x2032E7BD;
    public static uint CaptainJackSparrowAccessorySlot1 = 0x2032E7D0;
    public static uint CaptainJackSparrowItemSlotCount = 0x2032E7BE;
    public static uint CaptainJackSparrowItemSlot1 = 0x2032E7E0;
    public static uint CaptainJackSparrowItemSlot2 = 0x2032E7E2;
    public static uint CaptainJackSparrowItemSlot3 = 0x2032E7E4;
    public static uint CaptainJackSparrowItemSlot4 = 0x2032E7E6;
    public static uint CaptainJackSparrowAbilityStart = 0x2032E800;

    public static uint AladdinArmorSlotCount = 0x2032E6A8;
    public static uint AladdinArmorSlot1 = 0x2032E6AC;
    public static uint AladdinArmorSlot2 = 0x2032E6AE;
    public static uint AladdinItemSlotCount = 0x2032E6AA;
    public static uint AladdinItemSlot1 = 0x2032E6CC;
    public static uint AladdinItemSlot2 = 0x2032E6CE;
    public static uint AladdinItemSlot3 = 0x2032E6D0;
    public static uint AladdinItemSlot4 = 0x2032E6D2;
    public static uint AladdinItemSlot5 = 0x2032E6D4;
    public static uint AladdinAbilityStart = 0x2032E6EC;

    public static uint JackSkellingtonAccessorySlotCount = 0x2032E9E5;
    public static uint JackSkellingtonAccessorySlot1 = 0x2032E9F8;
    public static uint JackSkellingtonAccessorySlot2 = 0x2032E9FA;
    public static uint JackSkellingtonItemSlotCount = 0x2032E9E6;
    public static uint JackSkellingtonItemSlot1 = 0x2032EA08;
    public static uint JackSkellingtonItemSlot2 = 0x2032EA0A;
    public static uint JackSkellingtonItemSlot3 = 0x2032EA0C;
    public static uint JackSkellingtonAbilityStart = 0x2032EA28;

    public static uint SimbaAccessorySlotCount = 0x2032EAF9;
    public static uint SimbaAccessorySlot1 = 0x2032EB0C;
    public static uint SimbaAccessorySlot2 = 0x2032EB0E;
    public static uint SimbaItemSlotCount = 0x2032EAFA;
    public static uint SimbaItemSlot1 = 0x2032EB1C;
    public static uint SimbaItemSlot2 = 0x2032EB1E;
    public static uint SimbaItemSlot3 = 0x2032EB20;
    public static uint SimbaAbilityStart = 0x2032EB3C;

    public static uint TronArmorSlotCount = 0x2032EC0C;
    public static uint TronArmorSlot1 = 0x2032EC10;
    public static uint TronAccessorySlotCount = 0x2032EC0D;
    public static uint TronAccessorySlot1 = 0x2032EC20;
    public static uint TronItemSlotCount = 0x2032EC0E;
    public static uint TronItemSlot1 = 0x2032EC30;
    public static uint TronItemSlot2 = 0x2032EC32;
    public static uint TronAbilityStart = 0x2032EC50;

    public static uint RikuArmorSlotCount = 0x2032ED20;
    public static uint RikuArmorSlot1 = 0x2032ED24;
    public static uint RikuArmorSlot2 = 0x2032ED26;
    public static uint RikuAccessorySlotCount = 0x2032ED21;
    public static uint RikuAccessorySlot1 = 0x2032ED34;
    public static uint RikuItemSlotCount = 0x2032ED22;
    public static uint RikuItemSlot1 = 0x2032ED44;
    public static uint RikuItemSlot2 = 0x2032ED46;
    public static uint RikuItemSlot3 = 0x2032ED48;
    public static uint RikuItemSlot4 = 0x2032ED4A;
    public static uint RikuItemSlot5 = 0x2032ED4C;
    public static uint RikuItemSlot6 = 0x2032ED4E;
    public static uint RikuAbilityStart = 0x2032ED64;
}