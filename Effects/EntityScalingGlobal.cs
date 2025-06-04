using ConnectorLib;
using CrowdControl.Common;
using CrowdControl.Games.SmartEffects;
using System;
using System.Collections.Generic;

namespace CrowdControl.Games.Packs.KH2FM;

public partial class KH2FM {
    [EffectHandler("giant_entities", "tiny_entities")]
    public class EntityScalingGlobal : BaseEffect
    {
        public EntityScalingGlobal(KH2FM pack) : base(pack) { }

        public override EffectHandlerType Type => EffectHandlerType.Durational;

        public override IList<String> Codes { get; } = new[] {
            EffectIds.GiantEntities,
            EffectIds.TinyEntities
        };

        public override Mutex Mutexes { get; } = new[] {
            EffectIds.GiantEntities,
            EffectIds.TinyEntities
        };

        // scales the entire non-static game world, effectively scaling the player and all entities
        private uint GlobalEntityScaleAddress = 0x2036CECC;
        private uint GiantValue = BitConverter.ToUInt32(BitConverter.GetBytes(0.25f));
        private uint TinyValue = BitConverter.ToUInt32(BitConverter.GetBytes(2f));
        private uint DefaultValue = BitConverter.ToUInt32(BitConverter.GetBytes(1f));

        public override bool StartAction()
        {
            return Connector.Write32LE(GlobalEntityScaleAddress, Lookup(GiantValue, TinyValue));
        }

        public override bool StopAction() {
            return Connector.Write32LE(GlobalEntityScaleAddress, DefaultValue);
        }
    }
}