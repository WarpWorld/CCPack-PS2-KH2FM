using ConnectorLib;
using CrowdControl.Common;
using CrowdControl.Games.SmartEffects;
using System;
using System.Collections.Generic;

namespace CrowdControl.Games.Packs.KH2FM;

public partial class KH2FM {
    [EffectHandler("zero_mp_sora", "unlimited_mp_sora")]
    public class SoraMP : BaseEffect
    {
        public SoraMP(KH2FM pack) : base(pack) { }

        public override EffectHandlerType Type => EffectHandlerType.Durational;

        public override IList<String> Codes { get; } = new[] {
            EffectIds.ZeroMPSora,
            EffectIds.UnlimitedMPSora,
        };

        public override Mutex Mutexes { get; } = new[] {
            EffectIds.ZeroMPSora,
            EffectIds.UnlimitedMPSora,
            EffectIds.ProCodes,
            EffectIds.EzCodes
        };
        
        private bool isZeroMP => Lookup(0, 1) == 0;
        private bool isUnlimitedMP => Lookup(0, 1) == 1;

        public override bool StartAction() {
            if (isZeroMP) {
                return Connector.Write16LE(StatAddresses.MP, 0);
            } else {
                bool success = Connector.Read16LE(StatAddresses.MaxMP, out ushort maxMPValue);
                success &= Connector.Write16LE(StatAddresses.MP, maxMPValue);
                return success;
            }
        }

        public override bool RefreshAction() {
            if (isZeroMP) {
                return Connector.Write16LE(StatAddresses.MP, 0);
            } else {
                return Connector.Read16LE(StatAddresses.MaxMP, out ushort maxMPValue)
                    && Connector.Write16LE(StatAddresses.MP, maxMPValue);
            }
        }

        public override bool StopAction() {
            return Connector.Read32LE(StatAddresses.MaxMP, out uint maxMP)
                && Connector.Write32LE(StatAddresses.MP, maxMP);
        }
    }
}