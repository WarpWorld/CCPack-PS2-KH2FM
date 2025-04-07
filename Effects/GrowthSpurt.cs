using CrowdControl.Games.SmartEffects;

namespace CrowdControl.Games.Packs.KH2FM;

public partial class KH2FM {
    [EffectHandler("growth_spurt")]
    public class GrowthSpurt : BaseEffect
    {
        private uint startAddress;

        public GrowthSpurt(KH2FM pack) : base(pack) { }

        public override EffectHandlerType Type => EffectHandlerType.Durational;

        public override IList<String> Codes { get; } = [EffectIds.GrowthSpurt];

        public override Mutex Mutexes { get; } = [EffectIds.GrowthSpurt];

        public override bool StartAction()
        {
            bool success = true;
            // Sora has 148 (maybe divided by 2?) slots available for abilities
            for (uint i = EquipmentAddresses.SoraAbilityStart; i < (EquipmentAddresses.SoraAbilityStart + 148); i += 2)
            {
                success &= Connector.Read8(i, out byte value);

                if (value != 0) continue;

                startAddress = i;

                success &= Connector.Write8(startAddress, (byte)AbilityValues.HighJumpMax);
                success &= Connector.Write8(startAddress + 1, 0x80);

                success &= Connector.Write8(startAddress + 2, (byte)AbilityValues.QuickRunMax);
                success &= Connector.Write8(startAddress + 3, 0x80);

                success &= Connector.Write8(startAddress + 4, (byte)AbilityValues.DodgeRollMax);
                success &= Connector.Write8(startAddress + 5, 0x82);

                success &= Connector.Write8(startAddress + 6, (byte)AbilityValues.AerialDodgeMax);
                success &= Connector.Write8(startAddress + 7, 0x80);

                success &= Connector.Write8(startAddress + 8, (byte)AbilityValues.GlideMax);
                success &= Connector.Write8(startAddress + 9, 0x80);

                break;
            }

            return success;
        }

        public override bool StopAction() {
            bool success = true;

            success &= Connector.Write8(startAddress, 0);
            success &= Connector.Write8(startAddress + 1, 0);

            success &= Connector.Write8(startAddress + 2, 0);
            success &= Connector.Write8(startAddress + 3, 0);

            success &= Connector.Write8(startAddress + 4, 0);
            success &= Connector.Write8(startAddress + 5, 0);

            success &= Connector.Write8(startAddress + 6, 0);
            success &= Connector.Write8(startAddress + 7, 0);

            success &= Connector.Write8(startAddress + 8, 0);
            success &= Connector.Write8(startAddress + 9, 0);

            return success;
        }
    }
}