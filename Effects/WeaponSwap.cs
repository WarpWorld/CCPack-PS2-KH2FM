using ConnectorLib;
using CrowdControl.Games.SmartEffects;

namespace CrowdControl.Games.Packs.KH2FM;

public partial class KH2FM {
    [EffectHandler("struggling", "ultima", "forbidden_keyblade")]
    public class WeaponSwap : BaseEffect
    {
        private ushort? currentKeyblade = null;
        public WeaponSwap(KH2FM pack) : base(pack) { }

        public override EffectHandlerType Type => EffectHandlerType.Durational;

        public override IList<string> Codes { get; } = new [] { EffectIds.Struggling, EffectIds.Ultima, EffectIds.ForbiddenKeyblade };

        public override Mutex Mutexes { get; } = new [] { EffectIds.Struggling, EffectIds.Ultima, EffectIds.ForbiddenKeyblade };

        public override bool StartAction()
        {
            bool success = true;
            success &= Connector.Read16LE(EquipmentAddresses.SoraWeaponSlot, out ushort currKeyblade);
            // Only override the value that will be reset to at the end if there isn't a value currently queued for reset
            if (currentKeyblade == null) {
                currentKeyblade = currKeyblade;
            }
            success &= Connector.Write16LE(EquipmentAddresses.SoraWeaponSlot, (ushort) Lookup(KeybladeValues.StruggleBat, KeybladeValues.UltimaWeapon, KeybladeValues.Mulan));
            return success;
        }

        public override bool StopAction() {
            bool success = true;
            if (currentKeyblade != null) {
                success &= Connector.Write16LE(EquipmentAddresses.SoraWeaponSlot, (ushort)currentKeyblade);
                // Reset current keyblade so that the next invocation of the effect can overwrite it
                currentKeyblade = null;
            }

            return success;
        }
    }
}