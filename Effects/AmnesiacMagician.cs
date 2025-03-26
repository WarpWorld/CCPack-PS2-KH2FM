using ConnectorLib;
using CrowdControl.Common;
using CrowdControl.Games.SmartEffects;
using System;
using System.Collections.Generic;

namespace CrowdControl.Games.Packs.KH2FM;

public partial class KH2FM {
    [EffectHandler("amnesiac_magician")]
    public class AmnesiacMagician : BaseEffect
    {
        public AmnesiacMagician(KH2FM pack) : base(pack) { }

        public override EffectHandlerType Type => EffectHandlerType.Durational;

        public override IList<String> Codes { get; } = [EffectIds.AmnesiacMagician];

        public override Mutex Mutexes { get; } =
        [
            EffectIds.ExpertMagician,
            EffectIds.AmnesiacMagician,
            EffectIds.HeroSora,
            EffectIds.ZeroSora
        ];

        private byte fire;
        private byte blizzard;
        private byte thunder;
        private byte cure;
        private byte reflect;
        private byte magnet;

        public override bool StartAction()
        {
            bool success = true;
            success &= Connector.Read8((ulong)MagicAddresses.Fire, out fire);
            success &= Connector.Read8((ulong)MagicAddresses.Blizzard, out blizzard);
            success &= Connector.Read8((ulong)MagicAddresses.Thunder, out thunder);
            success &= Connector.Read8((ulong)MagicAddresses.Cure, out cure);
            success &= Connector.Read8((ulong)MagicAddresses.Reflect, out reflect);
            success &= Connector.Read8((ulong)MagicAddresses.Magnet, out magnet);

            success &= Connector.Write8((ulong)MagicAddresses.Fire, (byte)MiscValues.None);
            success &= Connector.Write8((ulong)MagicAddresses.Blizzard, (byte)MiscValues.None);
            success &= Connector.Write8((ulong)MagicAddresses.Thunder, (byte)MiscValues.None);
            success &= Connector.Write8((ulong)MagicAddresses.Cure, (byte)MiscValues.None);
            success &= Connector.Write8((ulong)MagicAddresses.Reflect, (byte)MiscValues.None);
            success &= Connector.Write8((ulong)MagicAddresses.Magnet, (byte)MiscValues.None);

            return success;
        }

        public override bool StopAction() {
            bool success = true;
            success &= Connector.Write8((ulong)MagicAddresses.Fire, fire);
            success &= Connector.Write8((ulong)MagicAddresses.Blizzard, blizzard);
            success &= Connector.Write8((ulong)MagicAddresses.Thunder, thunder);
            success &= Connector.Write8((ulong)MagicAddresses.Cure, cure);
            success &= Connector.Write8((ulong)MagicAddresses.Reflect, reflect);
            success &= Connector.Write8((ulong)MagicAddresses.Magnet, magnet);
            return success;
        }
    }
}