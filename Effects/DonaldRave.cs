using ConnectorLib;
using CrowdControl.Common;
using CrowdControl.Games.SmartEffects;
using System;
using System.Collections.Generic;
using System.Threading;

// Incomplete. Needs more work but I'm abandoning it for now.

namespace CrowdControl.Games.Packs.KH2FM;

public partial class KH2FM
{
    [EffectHandler("donald_rave")]
    public class DonaldRave : BaseEffect
    {
        public DonaldRave(KH2FM pack) : base(pack) { }

        public override EffectHandlerType Type => EffectHandlerType.Durational;

        public override IList<String> Codes { get; } = new[] {
            EffectIds.DonaldRave,
        };

        public override Mutex Mutexes { get; } = new[] {
            EffectIds.DonaldRave,
        };

        private byte firstAbilityValue;
        private byte firstAbilityEnabled;

        public override bool StartAction()
        {
            bool success = true;

            // Give Sora max MP
            success &= Connector.Read16LE(StatAddresses.MaxMP, out ushort maxMPValue);
            success &= Connector.Write16LE(StatAddresses.MP, maxMPValue);

            // Store what Donald's first ability is and whether it's enabled
            success &= Connector.Read8(EquipmentAddresses.DonaldAbilityStart + 4, out firstAbilityValue);
            success &= Connector.Read8(EquipmentAddresses.DonaldAbilityStart + 5, out firstAbilityEnabled);

            // Give Donald the Flare Force ability in the first ability slot
            success &= Connector.Write8(EquipmentAddresses.DonaldAbilityStart + 4, (byte)AbilityValues.FlareForce);
            success &= Connector.Write8(EquipmentAddresses.DonaldAbilityStart + 5, 0x80);

            // Set reaction to Duck Flare
            success &= Connector.Write16LE(DriveAddresses.ReactionPopup, (ushort)MiscValues.None);
            success &= Connector.Write16LE(DriveAddresses.ReactionOption, 344);
            success &= Connector.Write16LE(DriveAddresses.ReactionEnable, (ushort)MiscValues.None);

            Thread.Sleep(2000); // Wait for the reaction to be set

            success &= Connector.Write8(ButtonAddresses.ButtonsPressed, (byte)ButtonValues.Triangle);
            success &= Connector.Write8(ButtonAddresses.ButtonMask, ButtonValues.MaskTriangle);
            success &= Connector.Write8(ButtonAddresses.ButtonsDown, ButtonValues.Triangle);
            success &= Connector.Write8(ButtonAddresses.TriangleDown, 0xFF); // Triangle button down
            success &= Connector.Write8(ButtonAddresses.TrianglePressed, 0xFF); // Triangle button pressed

            // Set reaction to Rocket Flare
            success &= Connector.Write16LE(DriveAddresses.ReactionPopup, (ushort)MiscValues.None);
            success &= Connector.Write16LE(DriveAddresses.ReactionOption, 347);
            success &= Connector.Write16LE(DriveAddresses.ReactionEnable, (ushort)MiscValues.None);

            Thread.Sleep(2000); // Wait for the reaction to be set

            success &= Connector.Write8(ButtonAddresses.ButtonsPressed, (byte)ButtonValues.Triangle);
            success &= Connector.Write8(ButtonAddresses.ButtonMask, 0x10);
            success &= Connector.Write8(ButtonAddresses.ButtonsDown, 0xEF);
            success &= Connector.Write8(ButtonAddresses.TriangleDown, 0xFF); // Triangle button down
            success &= Connector.Write8(ButtonAddresses.TrianglePressed, 0xFF); // Triangle button pressed

            // Set reaction to Rocket Flare
            success &= Connector.Write16LE(DriveAddresses.ReactionPopup, (ushort)MiscValues.None);
            success &= Connector.Write16LE(DriveAddresses.ReactionOption, 348);
            success &= Connector.Write16LE(DriveAddresses.ReactionEnable, (ushort)MiscValues.None);

            Thread.Sleep(2000); // Wait for the reaction to be set

            success &= Connector.Write8(ButtonAddresses.ButtonsPressed, (byte)ButtonValues.Triangle);
            success &= Connector.Write8(ButtonAddresses.ButtonMask, 0x10);
            success &= Connector.Write8(ButtonAddresses.ButtonsDown, 0xEF);
            success &= Connector.Write8(ButtonAddresses.TriangleDown, 0xFF); // Triangle button down
            success &= Connector.Write8(ButtonAddresses.TrianglePressed, 0xFF); // Triangle button pressed

            return success;
        }

        // public override SITimeSpan RefreshInterval { get; } = 1;


        // public override bool RefreshAction()
        // {
        //     bool success = true;

        //     // Give Sora max MP
        //     success &= Connector.Read16LE(StatAddresses.MaxMP, out ushort maxMPValue);
        //     success &= Connector.Write16LE(StatAddresses.MP, maxMPValue);

        //     success &= Connector.Write8(ButtonAddresses.ButtonsPressed, ButtonValues.Triangle);
        //     success &= Connector.Write8(ButtonAddresses.ButtonsDown, ButtonValues.Triangle);
        //     success &= Connector.Write8(ButtonAddresses.ButtonMask, ButtonValues.MaskTriangle);

        //     return success;
        // }

        public override bool StopAction()
        {
            bool success = true;

            // Restore Donald's first ability and whether it was enabled
            success &= Connector.Write8(EquipmentAddresses.DonaldAbilityStart + 4, firstAbilityValue);
            success &= Connector.Write8(EquipmentAddresses.DonaldAbilityStart + 5, firstAbilityEnabled);

            return success;
        }

    }
}