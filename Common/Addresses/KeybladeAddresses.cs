namespace CrowdControl.Games.Packs.KH2FM;

public static class KeybladeAddresses {
    public static uint KingdomKey = 0x2032F0D1;
    public static uint Oathkeeper = 0x2032F0D2;
    public static uint Oblivion = 0x2032F0D3;
    public static uint DetectionSaber = 0x2032F0D4;
    public static uint FrontierOfUltima = 0x2032F0D5;
    public static uint StarSeeker = 0x2032F1AB;
    public static uint HiddenDragon = 0x2032F1AC;
    public static uint HerosCrest = 0x2032F1AF;
    public static uint Monochrome = 0x2032F1B0;
    public static uint FollowTheWind = 0x2032F1B1;
    public static uint CircleOfLife = 0x2032F1B2;
    public static uint PhotonDebugger = 0x2032F1B3;
    public static uint GullWing = 0x2032F1B4;
    public static uint RumblingRose = 0x2032F1B5;
    public static uint GuardianSoul = 0x2032F1B6;
    public static uint WishingLamp = 0x2032F1B7;
    public static uint DecisivePumpkin = 0x2032F1B8;
    public static uint SleepingLion = 0x2032F1B9;
    public static uint SweetMemories = 0x2032F1BA;
    public static uint MysteriousAbyss = 0x2032F1BB;
    public static uint BondOfFlame = 0x2032F1BC;
    public static uint FatalCrest = 0x2032F1BD;
    public static uint Fenrir = 0x2032F1BE;
    public static uint UltimaWeapon = 0x2032F1BF;
    public static uint TwoBecomeOne = 0x2032F1C8;
    public static uint WinnersProof = 0x2032F1C9;

    /// <summary>
    /// Creates a dictionary of all keyblades, each with a quantity of 0.
    /// You can use this to reset the quantity of all keyblades to 0
    /// or to store the quantities of all keyblades.
    /// 
    /// Key: Memory address for the keyblade's quantity in inventory
    /// Value: Quantity of the keyblade's in inventory. Initialized to zero.
    /// 
    /// </summary>
    /// <returns>a dictionary with each keyblade's address and a value of zero</returns>
    public static Dictionary<uint, byte> MakeKeybladesDictionary () {
        return new Dictionary<uint, byte> {
            { KingdomKey, 0 },
            { Oathkeeper, 0 },
            { Oblivion, 0 },
            { DetectionSaber, 0 },
            { FrontierOfUltima, 0 },
            { StarSeeker, 0 },
            { HiddenDragon, 0 },
            { HerosCrest, 0 },
            { Monochrome, 0 },
            { FollowTheWind, 0 },
            { CircleOfLife, 0 },
            { PhotonDebugger, 0 },
            { GullWing, 0 },
            { RumblingRose, 0 },
            { GuardianSoul, 0 },
            { WishingLamp, 0 },
            { DecisivePumpkin, 0 },
            { SleepingLion, 0 },
            { SweetMemories, 0 },
            { MysteriousAbyss, 0 },
            { BondOfFlame, 0 },
            { FatalCrest, 0 },
            { Fenrir, 0 },
            { UltimaWeapon, 0 },
            { TwoBecomeOne, 0 },
            { WinnersProof, 0 },
        };
    }
}