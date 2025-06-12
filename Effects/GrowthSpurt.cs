using CrowdControl.Games.SmartEffects;
using System.Collections.Generic;

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
            for (uint i = EquipmentAddresses.SoraAbilityStart; i < EquipmentAddresses.SoraAbilityStart + 148; i += 2)
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

            Dictionary<byte, bool> abilityRemoved = new()
            {
                { (byte)AbilityValues.HighJumpMax, false },
                { (byte)AbilityValues.QuickRunMax, false },
                { (byte)AbilityValues.DodgeRollMax, false },
                { (byte)AbilityValues.AerialDodgeMax, false },
                { (byte)AbilityValues.GlideMax, false }
            };

            for (uint i = EquipmentAddresses.SoraAbilityStart + 147; i >= EquipmentAddresses.SoraAbilityStart; i--)
            {
                success &= Connector.Read8(i, out byte value);

                if (value == (byte)AbilityValues.HighJumpMax || value == (byte)AbilityValues.QuickRunMax ||
                    value == (byte)AbilityValues.DodgeRollMax || value == (byte)AbilityValues.AerialDodgeMax ||
                    value == (byte)AbilityValues.GlideMax)
                {
                    // Only remove the ability once so that we don't remove
                    // the genuinely acquired abilities
                    if (!abilityRemoved[value])
                    {
                        success &= Connector.Write8(i, 0);
                        success &= Connector.Write8(i + 1, 0);
                    }
                }
            }

            return success;
        }
    }
}