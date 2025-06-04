using ConnectorLib;
using CrowdControl.Games.SmartEffects;

namespace CrowdControl.Games.Packs.KH2FM;

public partial class KH2FM {
    [EffectHandler("pro_codes", "ez_codes")]
    public class ProAndEzCodes : BaseEffect
    {
        public ProAndEzCodes(KH2FM pack) : base(pack) { }

        public override EffectHandlerType Type => EffectHandlerType.Durational;

        // Refresh every 2 seconds so that we don't just 
        // tear through the values too quickly.
        public override SITimeSpan RefreshInterval { get; } = 2.0f;

        public override IList<String> Codes { get; } = new [] { EffectIds.ProCodes, EffectIds.EzCodes };

        public override Mutex Mutexes { get; } =
        [
            EffectIds.HealSora, 
            EffectIds.OneShotSora, 
            EffectIds.Invulnerability,
            EffectIds.ZeroMPSora,
            EffectIds.UnlimitedMPSora,
            EffectIds.ZeroDrive,
            EffectIds.UnlimitedDrive,
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
            // We want to lower the byte until it rolls over,
            //  wherein we will lower the current drive, until we hit 0
            success &= Connector.Read8(DriveAddresses.DriveFill, out byte currentDriveFill);
            success &= Connector.Read8(DriveAddresses.Drive, out byte currentDrive);
            float newDriveFill = Clamp(currentDriveFill * percentChange, 255.0f);

            if (newDriveFill > currentDriveFill)
            {
                if (currentDrive > 0)
                {
                    int newCurrentDrive = currentDrive - 1;

                    success &= Connector.Write8(DriveAddresses.Drive, (byte)newCurrentDrive);
                    success &= Connector.Write8(DriveAddresses.DriveFill, (byte)newDriveFill);
                }
            }
            else
            {
                success &= Connector.Write8(DriveAddresses.DriveFill, (byte)newDriveFill);
            }

            return success;
        }
    }
}