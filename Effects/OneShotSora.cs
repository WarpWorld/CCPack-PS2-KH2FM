using ConnectorLib;
using CrowdControl.Games.SmartEffects;

namespace CrowdControl.Games.Packs.KH2FM;

public partial class KH2FM {
    [EffectHandler("one_shot_sora")]
    public class OneShotSora : BaseEffect
    {
        public OneShotSora(KH2FM pack) : base(pack) { }

        public override EffectHandlerType Type => EffectHandlerType.Durational;

        public override IList<String> Codes { get; } = [EffectIds.OneShotSora];

        public override Mutex Mutexes { get; } = [EffectIds.HealSora, EffectIds.OneShotSora, EffectIds.Invulnerability];

        public override bool StartAction()
        {
            return Connector.Write32LE(StatAddresses.HP, 1);
        }

        public override bool RefreshAction()
            => Connector.Write32LE(StatAddresses.HP, 1);

        public override bool StopAction() {
            return Connector.Read32LE(StatAddresses.MaxHP, out uint maxHP)
                && Connector.Write32LE(StatAddresses.HP, maxHP);
        }
    }
}