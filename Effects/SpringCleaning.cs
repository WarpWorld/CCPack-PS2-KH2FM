using ConnectorLib;
using CrowdControl.Games.SmartEffects;
using System.Collections.Generic;

namespace CrowdControl.Games.Packs.KH2FM;

public partial class KH2FM {
    [EffectHandler("spring_cleaning")]
    public class SpringCleaning : BaseEffect
    {
        private static Dictionary<uint, byte> items = MiscAddresses.MakeInventoryDictionary();

        private static Dictionary<uint, ushort> slots = EquipmentAddresses.MakeSoraInventorySlotsDictionary();

        public SpringCleaning(KH2FM pack) : base(pack) { }

        public override EffectHandlerType Type => EffectHandlerType.Durational;

        public override IList<String> Codes { get; } = [EffectIds.SpringCleaning];

        public override Mutex Mutexes { get; } =
        [
            EffectIds.Itemaholic,
            EffectIds.AllBaseItems,
            EffectIds.AllAccessories,
            EffectIds.AllArmors,
            EffectIds.AllKeyblades,
            EffectIds.SpringCleaning,
            EffectIds.HeroSora,
            EffectIds.ZeroSora
        ];

        public override bool StartAction()
        {
            items = MiscAddresses.MakeInventoryDictionary();
            slots = EquipmentAddresses.MakeSoraInventorySlotsDictionary();

            bool success = true;
            // Save all current items, before writing max value to them
            foreach (var (itemAddress, _) in items)
            {
                success &= Connector.Read8(itemAddress, out byte itemCount);

                items[itemAddress] = itemCount;

                success &= Connector.Write8(itemAddress, byte.MinValue);
            }

            // Save all current slots
            foreach (var (slotAddress, _) in slots)
            {
                success &= Connector.Read16LE(slotAddress, out ushort slotValue);

                slots[slotAddress] = slotValue;

                if (slotAddress != EquipmentAddresses.SoraWeaponSlot && slotAddress != EquipmentAddresses.SoraValorWeaponSlot &&
                    slotAddress != EquipmentAddresses.SoraMasterWeaponSlot && slotAddress != EquipmentAddresses.SoraFinalWeaponSlot)
                {
                    success &= Connector.Write16LE(slotAddress, ushort.MinValue);
                }
            }

            return success;
        }

        public override bool StopAction() {
            bool success = true;
            // Write back all saved items
            foreach (var (itemAddress, itemCount) in items)
            
            {
                success &= Connector.Write8(itemAddress, itemCount);
            }

            // Write back all saved slots
            foreach (var (slotAddress, slotValue) in slots)
            {
                if (slotAddress != EquipmentAddresses.SoraWeaponSlot && slotAddress != EquipmentAddresses.SoraValorWeaponSlot &&
                    slotAddress != EquipmentAddresses.SoraMasterWeaponSlot && slotAddress != EquipmentAddresses.SoraFinalWeaponSlot)
                {
                    success &= Connector.Write16LE(slotAddress, slotValue);
                }
            }
            return success;
        }
    }
}