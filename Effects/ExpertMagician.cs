using CrowdControl.Games.SmartEffects;

namespace CrowdControl.Games.Packs.KH2FM;

public partial class KH2FM {
    [EffectHandler("expert_magician")]
    public class ExpertMagician : BaseEffect
    {
        public ExpertMagician(KH2FM pack) : base(pack) { }

        public override EffectHandlerType Type => EffectHandlerType.Durational;

        public override IList<String> Codes { get; } = [EffectIds.ExpertMagician];

        public override Mutex Mutexes { get; } =
        [
            EffectIds.ExpertMagician,
            EffectIds.AmnesiacMagician,
            EffectIds.HeroSora,
            EffectIds.ZeroSora
        ];

        private static byte fire;
        private static byte blizzard;
        private static byte thunder;
        private static byte cure;
        private static byte reflect;
        private static byte magnet;

        private static byte fireCost;
        private static byte blizzardCost;
        private static byte thunderCost;
        private static byte cureCost;
        private static byte reflectCost;
        private static byte magnetCost;

        public override bool StartAction()
        {
            bool success = true;

            // Save Magic
            success &= Connector.Read8((ulong)MagicAddresses.Fire, out fire);
            success &= Connector.Read8((ulong)MagicAddresses.Blizzard, out blizzard);
            success &= Connector.Read8((ulong)MagicAddresses.Thunder, out thunder);
            success &= Connector.Read8((ulong)MagicAddresses.Cure, out cure);
            success &= Connector.Read8((ulong)MagicAddresses.Reflect, out reflect);
            success &= Connector.Read8((ulong)MagicAddresses.Magnet, out magnet);

            // Write Max Magic
            success &= Connector.Write8((ulong)MagicAddresses.Fire, (byte)MagicValues.Firaga);
            success &= Connector.Write8((ulong)MagicAddresses.Blizzard, (byte)MagicValues.Blizzaga);
            success &= Connector.Write8((ulong)MagicAddresses.Thunder, (byte)MagicValues.Thundaga);
            success &= Connector.Write8((ulong)MagicAddresses.Cure, (byte)MagicValues.Curaga);
            success &= Connector.Write8((ulong)MagicAddresses.Reflect, (byte)MagicValues.Reflega);
            success &= Connector.Write8((ulong)MagicAddresses.Magnet, (byte)MagicValues.Magnega);

            // Save Magic Costs
            success &= Connector.Read8(MPCostAddresses.FiragaCost, out fireCost);
            success &= Connector.Read8(MPCostAddresses.BlizzagaCost, out blizzardCost);
            success &= Connector.Read8(MPCostAddresses.ThundagaCost, out thunderCost);
            success &= Connector.Read8(MPCostAddresses.CuragaCost, out cureCost);
            success &= Connector.Read8(MPCostAddresses.ReflegaCost, out reflectCost);
            success &= Connector.Read8(MPCostAddresses.MagnegaCost, out magnetCost);

            // Write Magic Costs
            success &= Connector.Write8(MPCostAddresses.FiragaCost, 0x1);
            success &= Connector.Write8(MPCostAddresses.BlizzagaCost, 0x2);
            success &= Connector.Write8(MPCostAddresses.ThundagaCost, 0x3);
            success &= Connector.Write8(MPCostAddresses.CuragaCost, 0x10);
            success &= Connector.Write8(MPCostAddresses.ReflegaCost, 0x6);
            success &= Connector.Write8(MPCostAddresses.MagnegaCost, 0x5);

            return success;
        }

        // Maybe we use RefreshAction here and continuously set MP to Max MP?

        public override bool StopAction() {
            bool success = true;
            // Write back saved Magic
            success &= Connector.Write8((ulong)MagicAddresses.Fire, fire);
            success &= Connector.Write8((ulong)MagicAddresses.Blizzard, blizzard);
            success &= Connector.Write8((ulong)MagicAddresses.Thunder, thunder);
            success &= Connector.Write8((ulong)MagicAddresses.Cure, cure);
            success &= Connector.Write8((ulong)MagicAddresses.Reflect, reflect);
            success &= Connector.Write8((ulong)MagicAddresses.Magnet, magnet);

            // Write back saved Magic Costs
            success &= Connector.Write8(MPCostAddresses.FiragaCost, fireCost);
            success &= Connector.Write8(MPCostAddresses.BlizzagaCost, blizzardCost);
            success &= Connector.Write8(MPCostAddresses.ThundagaCost, thunderCost);
            success &= Connector.Write8(MPCostAddresses.CuragaCost, cureCost);
            success &= Connector.Write8(MPCostAddresses.ReflegaCost, reflectCost);
            success &= Connector.Write8(MPCostAddresses.MagnegaCost, magnetCost);

            return success;
        }
    }
}