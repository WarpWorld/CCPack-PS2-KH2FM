using ConnectorLib;
using CrowdControl.Games.SmartEffects;

namespace CrowdControl.Games.Packs.KH2FM;

public partial class KH2FM {
    [EffectHandler("pro_codes", "ez_codes")]
    public class ProAndEzCodes : BaseEffect
    {
        public ProAndEzCodes(KH2FM pack) : base(pack) { }

        public override EffectHandlerType Type => EffectHandlerType.Durational;

        public override IList<String> Codes { get; } = [EffectIds.ProCodes, EffectIds.EzCodes];

        public override Mutex Mutexes { get; } =
        [
            EffectIds.HealSora, 
            EffectIds.OneShotSora, 
            EffectIds.Invulnerability,
            EffectIds.ProCodes,
            EffectIds.EzCodes
        ];

        // Clamps the value to be between the 5% of the maximum value and the maximum value
        private float Clamp(float value, float max) {
            return Math.Min(Math.Max(value, max * 0.05f), max);
        }

        public override bool RefreshAction() {
            bool success = true;

            // if pro codes, then take hp, mp, and drive
            // if ez codes, then give hp, mp, and drive
            float percentChange = Lookup(0.999f, 1.001f);

            // HP
            success &= Connector.Read32LE(StatAddresses.HP, out uint currentHP);
            success &= Connector.Read32LE(StatAddresses.MaxHP, out uint maxHP);
            float newHP = Clamp(currentHP * percentChange, maxHP);
            success &= Connector.Write32LE(StatAddresses.HP, (uint)newHP);

            // MP
            success &= Connector.Read32LE(StatAddresses.MP, out uint currentMP);
            success &= Connector.Read32LE(StatAddresses.MaxMP, out uint maxMP);
            float newMP = Clamp(currentMP * percentChange, maxMP);
            success &= Connector.Write32LE(StatAddresses.MP, (uint)newMP);

            // Drive
            success &= Connector.Read32LE(DriveAddresses.Drive, out uint currentDrive);
            success &= Connector.Read32LE(DriveAddresses.MaxDrive, out uint maxDrive);
            float newDrive = Clamp(currentDrive * percentChange, maxDrive);
            success &= Connector.Write32LE(DriveAddresses.Drive, (uint)newDrive);

            return success;
        }
    }
}