using ConnectorLib;
using CrowdControl.Games.SmartEffects;

namespace CrowdControl.Games.Packs.KH2FM;

// Does not work, at least not while playing as Roxas

public partial class KH2FM {
    [EffectHandler("hastega", "slowga")]
    public class SoraSpeed : BaseEffect
    {
        private uint? speed = null;
        
        public SoraSpeed(KH2FM pack) : base(pack) { }

        public override EffectHandlerType Type => EffectHandlerType.Durational;

        public override IList<String> Codes { get; } = [EffectIds.Hastega, EffectIds.Slowga];

        public override Mutex Mutexes { get; } = [EffectIds.Hastega, EffectIds.Slowga];

        public override bool StartAction()
        {
            uint speedValue = Lookup(SpeedValues.SpeedUpx2, SpeedValues.SlowDownx2);
            bool success = true;
            success &= Connector.Read32LE(StatAddresses.Speed, out uint currentSpeed);
            if (speed == null) {
                speed = currentSpeed;
            }
            success &= Connector.Write32LE(StatAddresses.Speed, speedValue);
            return success;
        }

        public override bool StopAction() {
            if (speed != null) {
                return Connector.Write32LE(StatAddresses.Speed, speed.Value);
            }
            return true;
        }
    }
}