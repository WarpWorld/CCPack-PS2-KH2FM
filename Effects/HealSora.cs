using ConnectorLib;
using CrowdControl.Games.SmartEffects;

namespace CrowdControl.Games.Packs.KH2FM;

public partial class KH2FM {
    [EffectHandler("heal_sora")]
    public class HealSora : BaseEffect
    {
        public HealSora(KH2FM pack) : base(pack) { }

        public override EffectHandlerType Type => EffectHandlerType.Instant;

        public override IList<String> Codes { get; } = [EffectIds.HealSora];

        public override Mutex Mutexes { get; } = [EffectIds.HealSora, EffectIds.OneShotSora, EffectIds.Invulnerability];

        public override bool StartAction()
        {
            return Connector.Read32LE(StatAddresses.MaxHP, out uint maxHP)
                && Connector.Write32LE(StatAddresses.HP, maxHP);
        }
    }
}