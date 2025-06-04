using ConnectorLib;
using CrowdControl.Common;
using CrowdControl.Games.SmartEffects;
using System;
using System.Collections.Generic;

namespace CrowdControl.Games.Packs.KH2FM;

public partial class KH2FM {
    [EffectHandler("one_shot_donald", "invulnerable_donald")]
    public class DonaldHP : BaseEffect
    {
        public DonaldHP(KH2FM pack) : base(pack) { }

        public override EffectHandlerType Type => EffectHandlerType.Durational;

        public override IList<String> Codes { get; } = new[] { EffectIds.OneShotDonald, EffectIds.InvulnerableDonald };

        public override Mutex Mutexes { get; } = new[] { EffectIds.OneShotDonald, EffectIds.InvulnerableDonald };
        
        private bool isOneShot => Lookup(0, 1) == 0;
        private bool isInvulnerable => Lookup(0, 1) == 1;

        public override bool StartAction() {
            if (isOneShot) {
                return Connector.Write16LE(StatAddresses.DonaldHP, 1);
            } else {
                bool success = Connector.Read16LE(StatAddresses.DonaldMaxHP, out ushort maxHPValue);
                success &= Connector.Write16LE(StatAddresses.DonaldHP, maxHPValue);
                return success;
            }
        }

        public override bool RefreshAction() {
            if (isOneShot) {
                return Connector.Write16LE(StatAddresses.DonaldHP, 1);
            } else {
                return Connector.Read16LE(StatAddresses.DonaldMaxHP, out ushort maxHPValue)
                    && Connector.Write16LE(StatAddresses.DonaldHP, maxHPValue);
            }
        }

        public override bool StopAction() {
            return Connector.Read32LE(StatAddresses.DonaldMaxHP, out uint maxHP)
                && Connector.Write32LE(StatAddresses.DonaldHP, maxHP);
        }
    }
}