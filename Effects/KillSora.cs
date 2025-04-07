using CrowdControl.Games.SmartEffects;

namespace CrowdControl.Games.Packs.KH2FM;

public partial class KH2FM {
    [EffectHandler("kill_sora")]
    public class KillSora : BaseEffect
    {
        public KillSora(KH2FM pack) : base(pack) { }

        public override EffectHandlerType Type => EffectHandlerType.Instant;

        public override IList<String> Codes { get; } = [EffectIds.KillSora];

        public override Mutex Mutexes { get; } = [EffectIds.KillSora];

        public override bool StartAction()
        {
            // TODO: Implement this. 
            // Returns false in case it makes it into the 
            // menu to protect from accidentally using coins
            return false;
        }
    }
}