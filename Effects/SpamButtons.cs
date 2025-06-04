using ConnectorLib;
using CrowdControl.Common;
using CrowdControl.Games.SmartEffects;
using System;
using System.Collections.Generic;

namespace CrowdControl.Games.Packs.KH2FM;

public partial class KH2FM {
    [EffectHandler("spam_random_buttons","button_spam_cross", "button_spam_circle", "button_spam_triangle", "button_spam_square", "button_spam_L1", "button_spam_R1")]
    public class SpamButtons : BaseEffect
    {
        private enum Mode
        {
            Random = 0,
            Cross = 1,
            Circle = 2,
            Triangle = 3,
            Square = 4,
            L1 = 5,
            R1 = 6
        }

        public SpamButtons(KH2FM pack) : base(pack) { }

        public override EffectHandlerType Type => EffectHandlerType.Durational;

        public override IList<String> Codes { get; } = new[] {
            EffectIds.SpamRandomButtons,
            EffectIds.SpamCrossButton,
            EffectIds.SpamCircleButton,
            EffectIds.SpamTriangleButton,
            EffectIds.SpamSquareButton,
            EffectIds.SpamL1Button,
            EffectIds.SpamR1Button
        };

        public override bool RefreshAction()
        {
            Mode mode = Lookup(
                Mode.Random,
                Mode.Cross,
                Mode.Circle,
                Mode.Triangle,
                Mode.Square,
                Mode.L1,
                Mode.R1
            );

            if(mode == Mode.Random)
            {
                // Randomly select a button to spam
                int randomValue = new Random().Next(1, 6);
                mode = (Mode)randomValue;
            }

            if (mode == Mode.Cross)
            {
                return Connector.Write8(ButtonAddresses.ButtonsPressed, ButtonValues.Cross)
                    && Connector.Write8(ButtonAddresses.ButtonsDown, ButtonValues.Cross)
                    && Connector.Write8(ButtonAddresses.ButtonMask, ButtonValues.MaskCross);
            }
            else if (mode == Mode.Square)
            {
                return Connector.Write8(ButtonAddresses.ButtonsPressed, ButtonValues.Square)
                    && Connector.Write8(ButtonAddresses.ButtonsDown, ButtonValues.Square)
                    && Connector.Write8(ButtonAddresses.ButtonMask, ButtonValues.MaskSquare);
            }
            else if (mode == Mode.Triangle)
            {
                return Connector.Write8(ButtonAddresses.ButtonsPressed, ButtonValues.Triangle)
                    && Connector.Write8(ButtonAddresses.ButtonsDown, ButtonValues.Triangle)
                    && Connector.Write8(ButtonAddresses.ButtonMask, ButtonValues.MaskTriangle);
            }
            else if (mode == Mode.Circle)
            {
                return Connector.Write8(ButtonAddresses.ButtonsPressed, ButtonValues.Circle)
                    && Connector.Write8(ButtonAddresses.ButtonsDown, ButtonValues.Circle)
                    && Connector.Write8(ButtonAddresses.ButtonMask, ButtonValues.MaskCircle);
            }
            else if (mode == Mode.L1)
            {
                return Connector.Write8(ButtonAddresses.ButtonsPressed, ButtonValues.L1)
                    && Connector.Write8(ButtonAddresses.ButtonsDown, ButtonValues.L1)
                    && Connector.Write8(ButtonAddresses.ButtonMask, ButtonValues.MaskL1);
            }
            else if (mode == Mode.R1)
            {
                return Connector.Write8(ButtonAddresses.ButtonsPressed, ButtonValues.R1)
                    && Connector.Write8(ButtonAddresses.ButtonsDown, ButtonValues.R1)
                    && Connector.Write8(ButtonAddresses.ButtonMask, ButtonValues.MaskR1);
            }

            return false;
        }
    }
}