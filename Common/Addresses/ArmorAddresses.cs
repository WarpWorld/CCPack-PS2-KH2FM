namespace CrowdControl.Games.Packs.KH2FM;

public static class ArmorAddresses {
    public static uint ElvenBandana = 0x2032F0EC;
    public static uint DivineBandana = 0x2032F0ED;
    public static uint PowerBand = 0x2032F0EE;
    public static uint BusterBand = 0x2032F0F6;
    public static uint ProtectBelt = 0x2032F0F7;
    public static uint GaiaBelt = 0x2032F0FA;
    public static uint CosmicBelt = 0x2032F101;
    public static uint ShockCharm = 0x2032F102;
    public static uint ShockCharmPlus = 0x2032F103;
    public static uint FireBangle = 0x2032F107;
    public static uint FiraBangle = 0x2032F108;
    public static uint FiragaBangle = 0x2032F109;
    public static uint FiragunBangle = 0x2032F10A;
    public static uint BlizzardArmlet = 0x2032F10C;
    public static uint BlizzaraArmlet = 0x2032F10D;
    public static uint BlizzagaArmlet = 0x2032F10E;
    public static uint BlizzagunArmlet = 0x2032F10F;
    public static uint ThunderTrinket = 0x2032F112;
    public static uint ThundaraTrinket = 0x2032F113;
    public static uint ThundagaTrinket = 0x2032F114;
    public static uint ThundagunTrinket = 0x2032F115;
    public static uint ShadowAnklet = 0x2032F129;
    public static uint DarkAnklet = 0x2032F12B;
    public static uint MidnightAnklet = 0x2032F12C;
    public static uint ChaosAnklet = 0x2032F12D;
    public static uint AbasChain = 0x2032F12F;
    public static uint AegisChain = 0x2032F130;
    public static uint CosmicChain = 0x2032F136;
    public static uint Acrisius = 0x2032F131;
    public static uint AcrisiusPlus = 0x2032F135;
    public static uint PetiteRibbon = 0x2032F134;
    public static uint Ribbon = 0x2032F132;
    public static uint GrandRibbon = 0x2032F104;
    public static uint ChampionBelt = 0x2032F133;

    /// <summary>
    /// Creates a dictionary of all armor, each with a quantity of 0.
    /// You can use this to reset the quantity of all armor to 0
    /// or to store the quantities of all armor.
    /// 
    /// Key: Memory address for the armor quantity in inventory
    /// Value: Quantity of the armor in inventory. Initialized to zero.
    /// 
    /// </summary>
    /// <returns>a dictionary with each armor address and a value of zero</returns>
    public static Dictionary<uint, byte> MakeArmorDictionary() {
        return new Dictionary<uint, byte> {
            { ElvenBandana, 0 },
            { DivineBandana, 0 },
            { PowerBand, 0 },
            { BusterBand, 0 },
            { ProtectBelt, 0 },
            { GaiaBelt, 0 },
            { CosmicBelt, 0 },
            { ShockCharm, 0 },
            { ShockCharmPlus, 0 },
            { FireBangle, 0 },
            { FiraBangle, 0 },
            { FiragaBangle, 0 },
            { FiragunBangle, 0 },
            { BlizzardArmlet, 0 },
            { BlizzaraArmlet, 0 },
            { BlizzagaArmlet, 0 },
            { BlizzagunArmlet, 0 },
            { ThunderTrinket, 0 },
            { ThundaraTrinket, 0 },
            { ThundagaTrinket, 0 },
            { ThundagunTrinket, 0 },
            { ShadowAnklet, 0 },
            { DarkAnklet, 0 },
            { MidnightAnklet, 0 },
            { ChaosAnklet, 0 },
            { AbasChain, 0 },
            { AegisChain, 0 },
            { CosmicChain, 0 },
            { Acrisius, 0 },
            { AcrisiusPlus, 0 },
            { PetiteRibbon, 0 },
            { Ribbon, 0 },
            { GrandRibbon, 0 },
            { ChampionBelt, 0 },
        };
    }
}