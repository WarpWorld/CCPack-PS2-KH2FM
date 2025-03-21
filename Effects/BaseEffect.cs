using ConnectorLib;
using CrowdControl.Common;
using CrowdControl.Games.SmartEffects;

namespace CrowdControl.Games.Packs.KH2FM;

public abstract class BaseEffect : EffectHandler<KH2FM, IPS2Connector> {
    public KH2FM pack;
    public BaseEffect(KH2FM pack) : base(pack) { 
        this.pack = pack;
    }

    public override SITimeSpan RefreshInterval { get; } = 0.2;

    public override SITimeSpan HoldMutex => SITimeSpan.FromMilliseconds(500);

    public override bool StartCondition()
    {
        return pack.IsGameInPlay();
    }

    public override bool RefreshCondition()
    {
        return pack.IsGameInPlay();
    }
}