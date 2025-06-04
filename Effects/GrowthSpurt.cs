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
            // start at the end of the list and work backwards
            // to find the first empty slot so that we don't conflict with other abilities
            for (uint i = EquipmentAddresses.SoraAbilityStart + 147; i > EquipmentAddresses.SoraAbilityStart ; i -= 2)
            {
                success &= Connector.Read8(i, out byte value);

                if (value != 0) continue;

                startAddress = i;

                success &= Connector.Write8(startAddress - 1, (byte)AbilityValues.HighJumpMax);
                success &= Connector.Write8(startAddress, 0x80);

                success &= Connector.Write8(startAddress - 3, (byte)AbilityValues.QuickRunMax);
                success &= Connector.Write8(startAddress - 2, 0x80);

                success &= Connector.Write8(startAddress - 5, (byte)AbilityValues.DodgeRollMax);
                success &= Connector.Write8(startAddress - 4, 0x82);

                success &= Connector.Write8(startAddress - 7, (byte)AbilityValues.AerialDodgeMax);
                success &= Connector.Write8(startAddress - 6, 0x80);

                success &= Connector.Write8(startAddress - 9, (byte)AbilityValues.GlideMax);
                success &= Connector.Write8(startAddress - 8, 0x80);

                break;
            }

            return success;
        }

        public override bool StopAction() {
            bool success = true;

            success &= Connector.Write8(startAddress, 0);
            success &= Connector.Write8(startAddress - 1, 0);

            success &= Connector.Write8(startAddress - 2, 0);
            success &= Connector.Write8(startAddress - 3, 0);

            success &= Connector.Write8(startAddress - 4, 0);
            success &= Connector.Write8(startAddress - 5, 0);

            success &= Connector.Write8(startAddress - 6, 0);
            success &= Connector.Write8(startAddress - 7, 0);

            success &= Connector.Write8(startAddress - 8, 0);
            success &= Connector.Write8(startAddress - 9, 0);

            return success;
        }
    }
}