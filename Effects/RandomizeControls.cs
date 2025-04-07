using CrowdControl.Games.SmartEffects;

namespace CrowdControl.Games.Packs.KH2FM;

public partial class KH2FM {
    [EffectHandler("randomize_controls")]
    public class RandomizeControls : BaseEffect
    {
        
        public RandomizeControls(KH2FM pack) : base(pack) { }

        public override EffectHandlerType Type => EffectHandlerType.Durational;

        public override IList<String> Codes { get; } = [EffectIds.RandomizeControls];

        public override Mutex Mutexes { get; } = [EffectIds.RandomizeControls];

        public override bool StartAction()
        {
            // TODO: Implement this. 
            // Returns false in case it makes it into the 
            // menu to protect from accidentally using coins
            return false;
        }
    }
}