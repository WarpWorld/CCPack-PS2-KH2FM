using ConnectorLib;
using CrowdControl.Games.SmartEffects;

namespace CrowdControl.Games.Packs.KH2FM;

public partial class KH2FM {
    [EffectHandler("give_munny", "take_munny")]
    public class Munny : BaseEffect
    {
        public Munny(KH2FM pack) : base(pack) { }

        public override EffectHandlerType Type => EffectHandlerType.Instant;

        public override IList<(string, Type)> Parameters { get; } = [("quantity", typeof(int))];

        public override IList<String> Codes { get; } = [EffectIds.GiveMunny, EffectIds.TakeMunny];

        public override Mutex Mutexes { get; } = [EffectIds.HealSora, EffectIds.OneShotSora, EffectIds.Invulnerability];

        public override bool StartAction()
        {
            bool success = Connector.Read32LE(MiscAddresses.Munny, out uint munny);
            // Quantity is injected by CC based on the Parameters field above 
            int newAmount = (int)munny + (int)Quantity * Lookup(10, -10);
            return success && Connector.Write32LE(MiscAddresses.Munny, (uint)newAmount);
        }
    }
}