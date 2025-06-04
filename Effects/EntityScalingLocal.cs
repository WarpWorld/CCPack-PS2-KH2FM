using ConnectorLib;
using CrowdControl.Common;
using CrowdControl.Games.SmartEffects;
using System;
using System.Collections.Generic;

namespace CrowdControl.Games.Packs.KH2FM;

public partial class KH2FM {
    [EffectHandler("upside_down", "invisible_entities")]
    public class EntityScalingLocal : BaseEffect
    {
        public EntityScalingLocal(KH2FM pack) : base(pack) { }

        public override EffectHandlerType Type => EffectHandlerType.Durational;

        public override IList<String> Codes { get; } = new[] {
            EffectIds.UpsideDown,
            EffectIds.InvisibleEntities
        };

        public override Mutex Mutexes { get; } = new[] {
            EffectIds.UpsideDown,
            EffectIds.InvisibleEntities
        };

        // sets local scaling for each entity, effectively scaling each entity up or down, or upside down
        private uint PerEntityScalingAddress = 0x2036CED4;

        private uint UpsideDownValue = BitConverter.ToUInt32(BitConverter.GetBytes(1f));
        private uint InvisibleValue = BitConverter.ToUInt32(BitConverter.GetBytes(0f));
        private uint DefaultValue = BitConverter.ToUInt32(BitConverter.GetBytes(-1f));

        public override bool StartAction()
        {
            return Connector.Write32LE(PerEntityScalingAddress, Lookup(UpsideDownValue, InvisibleValue));
        }

        public override bool StopAction() {
            return Connector.Write32LE(PerEntityScalingAddress, DefaultValue);
        }
    }
}