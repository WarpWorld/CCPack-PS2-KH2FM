using ConnectorLib;
using CrowdControl.Common;
using CrowdControl.Games.SmartEffects;
using System;
using System.Collections.Generic;

namespace CrowdControl.Games.Packs.KH2FM;

public partial class KH2FM {
    [EffectHandler("zero_mp_donald", "unlimited_mp_donald")]
    public class DonaldMP : BaseEffect
    {
        public DonaldMP(KH2FM pack) : base(pack) { }

        public override EffectHandlerType Type => EffectHandlerType.Durational;

        public override IList<String> Codes { get; } = new[] { EffectIds.ZeroMPDonald, EffectIds.UnlimitedMPDonald };

        public override Mutex Mutexes { get; } = new[] { EffectIds.ZeroMPDonald, EffectIds.UnlimitedMPDonald };
        
        private bool isZeroMP => Lookup(0, 1) == 0;
        private bool isUnlimitedMP => Lookup(0, 1) == 1;

        public override bool StartAction() {
            if (isZeroMP) {
                return Connector.Write16LE(StatAddresses.DonaldMP, 0);
            } else {
                bool success = Connector.Read16LE(StatAddresses.DonaldMaxMP, out ushort maxMPValue);
                success &= Connector.Write16LE(StatAddresses.DonaldMP, maxMPValue);
                return success;
            }
        }

        public override bool RefreshAction() {
            if (isZeroMP) {
                return Connector.Write16LE(StatAddresses.DonaldMP, 0);
            } else {
                return Connector.Read16LE(StatAddresses.DonaldMaxMP, out ushort maxMPValue)
                    && Connector.Write16LE(StatAddresses.DonaldMP, maxMPValue);
            }
        }

        public override bool StopAction() {
            return Connector.Read32LE(StatAddresses.DonaldMaxMP, out uint maxMP)
                && Connector.Write32LE(StatAddresses.DonaldMP, maxMP);
        }
    }
}