using ConnectorLib;
using CrowdControl.Games.SmartEffects;

namespace CrowdControl.Games.Packs.KH2FM;

// Does not work, at least not while playing as Roxas

public partial class KH2FM {
    [EffectHandler("space_jump")]
    public class SpaceJump : BaseEffect
    {
        private uint jump;
        
        public SpaceJump(KH2FM pack) : base(pack) { }

        public override EffectHandlerType Type => EffectHandlerType.Durational;

        public override IList<String> Codes { get; } = [EffectIds.SpaceJump];

        public override Mutex Mutexes { get; } = [EffectIds.SpaceJump];

        public override bool StartAction()
        {
            // Store original jump amount for the reset
            return Connector.Read32LE(MiscAddresses.JumpAmount, out jump)
                && Connector.Write32LE(MiscAddresses.JumpAmount, 0);
        }

        public override bool StopAction() {
            return Connector.Write32LE(MiscAddresses.JumpAmount, jump);
        }
    }
}