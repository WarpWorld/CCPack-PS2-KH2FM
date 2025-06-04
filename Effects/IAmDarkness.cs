using ConnectorLib;
using CrowdControl.Games.SmartEffects;

namespace CrowdControl.Games.Packs.KH2FM;

public partial class KH2FM {
    [EffectHandler("i_am_darkness")]
    public class IAmDarkness : BaseEffect
    {
        public IAmDarkness(KH2FM pack) : base(pack) { }

        public override EffectHandlerType Type => EffectHandlerType.Instant;

        public override IList<String> Codes { get; } = [EffectIds.IAmDarkness];

        public override Mutex Mutexes { get; } = new [] { 
            EffectIds.IAmDarkness, 
            EffectIds.BackseatDriver, 
            EffectIds.HeroSora, 
            EffectIds.ZeroSora, 
            EffectIds.GiantWeapon,
            EffectIds.TinyWeapon,
        };

        public override bool StartAction()
        {
            bool success = true;

            // Get us out of a Drive first if we are in one
            success &= Connector.WriteFloat(DriveAddresses.DriveTime, MiscValues.None);
            Thread.Sleep(200);

            success &= Connector.Write16LE(DriveAddresses.ReactionPopup, (ushort)MiscValues.None);

            success &= Connector.Write16LE(DriveAddresses.ReactionOption, (ushort)ReactionValues.ReactionAnti);

            success &= Connector.Write16LE(DriveAddresses.ReactionEnable, (ushort)MiscValues.None);

            Utils.TriggerReaction(Connector);
            return success;
        }
    }
}