using ConnectorLib;
using CrowdControl.Common;
using CrowdControl.Games.SmartEffects;
using System;
using System.Collections.Generic;

namespace CrowdControl.Games.Packs.KH2FM;

public partial class KH2FM {
    [EffectHandler("one_shot_goofy", "invulnerable_goofy")]
    public class GoofyHP : BaseEffect
    {
        public GoofyHP(KH2FM pack) : base(pack) { }

        public override EffectHandlerType Type => EffectHandlerType.Durational;

        public override IList<String> Codes { get; } = new[] { EffectIds.OneShotGoofy, EffectIds.InvulnerableGoofy };

        public override Mutex Mutexes { get; } = new[] { EffectIds.OneShotGoofy, EffectIds.InvulnerableGoofy };
        
        private bool isOneShot => Lookup(0, 1) == 0;
        private bool isInvulnerable => Lookup(0, 1) == 1;

        public override bool StartAction() {
            if (isOneShot) {
                return Connector.Write16LE(StatAddresses.GoofyHP, 1);
            } else {
                bool success = Connector.Read16LE(StatAddresses.GoofyMaxHP, out ushort maxHPValue);
                success &= Connector.Write16LE(StatAddresses.GoofyHP, maxHPValue);
                return success;
            }
        }

        public override bool RefreshAction() {
            if (isOneShot) {
                return Connector.Write16LE(StatAddresses.GoofyHP, 1);
            } else {
                return Connector.Read16LE(StatAddresses.GoofyMaxHP, out ushort maxHPValue)
                    && Connector.Write16LE(StatAddresses.GoofyHP, maxHPValue);
            }
        }

        public override bool StopAction() {
            return Connector.Read32LE(StatAddresses.GoofyMaxHP, out uint maxHP)
                && Connector.Write32LE(StatAddresses.GoofyHP, maxHP);
        }
    }
}