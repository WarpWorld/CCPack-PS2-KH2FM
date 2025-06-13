using ConnectorLib;
using CrowdControl.Games.SmartEffects;

namespace CrowdControl.Games.Packs.KH2FM;

public partial class KH2FM {
    [EffectHandler("tiny_weapon", "giant_weapon")]
    public class WeaponSize : BaseEffect
    {
        public WeaponSize(KH2FM pack) : base(pack) { }

        public override EffectHandlerType Type => EffectHandlerType.Durational;

        public override IList<String> Codes { get; } = new [] { 
            EffectIds.TinyWeapon, 
            EffectIds.GiantWeapon,
            EffectIds.IAmDarkness
        };

        public override Mutex Mutexes { get; } = new [] { 
            EffectIds.TinyWeapon, 
            EffectIds.GiantWeapon,
            EffectIds.IAmDarkness
        };

        public override bool StartAction()
        {
            return Connector.Write32LE(MiscAddresses.WeaponSize, Lookup(WeaponValues.TinyWeapon, WeaponValues.BigWeapon));
        }

        public override bool StopAction()
        {
            return Connector.Write32LE(MiscAddresses.WeaponSize, WeaponValues.NormalWeapon);
        }
    }
}