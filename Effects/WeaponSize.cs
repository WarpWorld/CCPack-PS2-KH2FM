using ConnectorLib;
using CrowdControl.Games.SmartEffects;
using System;
using System.Collections.Generic;

namespace CrowdControl.Games.Packs.KH2FM;

public partial class KH2FM {
    [EffectHandler("tiny_weapon", "giant_weapon")]
    public class WeaponSize : BaseEffect
    {
        private ushort? currentKeyblade = null;
        
        public WeaponSize(KH2FM pack) : base(pack) { }

        public override EffectHandlerType Type => EffectHandlerType.Durational;

        public override IList<String> Codes { get; } = [EffectIds.TinyWeapon, EffectIds.GiantWeapon];

        public override Mutex Mutexes { get; } = [EffectIds.TinyWeapon, EffectIds.GiantWeapon];

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