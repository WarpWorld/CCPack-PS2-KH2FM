namespace CrowdControl.Games.Packs.KH2FM;

public static class AccessoryAddresses {
    public static uint AbilityRing = 0x2032F0B7;
    public static uint EngineersRing = 0x2032F0B8;
    public static uint TechniciansRing = 0x2032F0B9;
    public static uint ExpertsRing = 0x2032F0BA;
    public static uint SardonyxRing = 0x2032F0BB;
    public static uint TourmalineRing = 0x2032F0BC;
    public static uint AquamarineRing = 0x2032F0BD;
    public static uint GarnetRing = 0x2032F0BE;
    public static uint DiamondRing = 0x2032F0BF;
    public static uint SilverRing = 0x2032F0C0;
    public static uint GoldRing = 0x2032F0C1;
    public static uint PlatinumRing = 0x2032F0C2;
    public static uint MythrilRing = 0x2032F0C3;
    public static uint OrichalcumRing = 0x2032F0CA;
    public static uint MastersRing = 0x2032F0CB;
    public static uint MoonAmulet = 0x2032F0CC;
    public static uint StarCharm = 0x2032F0CE;
    public static uint SkillRing = 0x2032F0CF;
    public static uint SkillfulRing = 0x2032F0D0;
    public static uint SoldierEarring = 0x2032F0D6;
    public static uint FencerEarring = 0x2032F0D7;
    public static uint MageEarring = 0x2032F0D8;
    public static uint SlayerEarring = 0x2032F0DC;
    public static uint CosmicRing = 0x2032F0DD;
    public static uint Medal = 0x2032F0E0;
    public static uint CosmicArts = 0x2032F0E1;
    public static uint ShadowArchive = 0x2032F0E2;
    public static uint ShadowArchivePlus = 0x2032F0E7;
    public static uint LuckyRing = 0x2032F0E8;
    public static uint FullBloom = 0x2032F0E9;
    public static uint FullBloomPlus = 0x2032F0EB;
    public static uint DrawRing = 0x2032F0EA;
    public static uint ExecutivesRing = 0x2032F1E5;

    /// <summary>
    /// Creates a dictionary of all accessories, each with a quantity of 0.
    /// You can use this to reset the quantity of all accessories to 0
    /// or to store the quantities of all accessories.
    /// 
    /// Key: Memory address for the accessory quantity in inventory
    /// Value: Quantity of the accessory in inventory. Initialized to zero.
    /// 
    /// </summary>
    /// <returns>a dictionary with each accessory address and a value of zero</returns>
    public static Dictionary<uint, byte> MakeAccessoriesDictionary() {
        return new Dictionary<uint, byte> {
            { AbilityRing, 0 },
            { EngineersRing, 0 },
            { TechniciansRing, 0 },
            { ExpertsRing, 0 },
            { SardonyxRing, 0 },
            { TourmalineRing, 0 },
            { AquamarineRing, 0 },
            { GarnetRing, 0 },
            { DiamondRing, 0 },
            { SilverRing, 0 },
            { GoldRing, 0 },
            { PlatinumRing, 0 },
            { MythrilRing, 0 },
            { OrichalcumRing, 0 },
            { MastersRing, 0 },
            { MoonAmulet, 0 },
            { StarCharm, 0 },
            { SkillRing, 0 },
            { SkillfulRing, 0 },
            { SoldierEarring, 0 },
            { FencerEarring, 0 },
            { MageEarring, 0 },
            { SlayerEarring, 0 },
            { CosmicRing, 0 },
            { Medal, 0 },
            { CosmicArts, 0 },
            { ShadowArchive, 0 },
            { ShadowArchivePlus, 0 },
            { LuckyRing, 0 },
            { FullBloom, 0 },
            { FullBloomPlus, 0 },
            { DrawRing, 0 },
            { ExecutivesRing, 0 },
        };
    }
}