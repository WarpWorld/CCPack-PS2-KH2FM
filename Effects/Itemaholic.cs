using ConnectorLib;
using CrowdControl.Games.SmartEffects;
using System.Collections.Generic;
using System;

namespace CrowdControl.Games.Packs.KH2FM;

public partial class KH2FM {
    [EffectHandler("itemaholic", "all_base_items", "all_accessories", "all_armors", "all_keyblades")]
    public class Itemaholic : BaseEffect
    {
        private enum EffectMode
        {
            Itemaholic,
            BaseItems,
            Accessories,
            Armors,
            Keyblades
        }

        public Itemaholic(KH2FM pack) : base(pack) { }

        public override EffectHandlerType Type => EffectHandlerType.Durational;

        public override IList<String> Codes { get; } = new [] { 
            EffectIds.Itemaholic,
            EffectIds.AllBaseItems,
            EffectIds.AllAccessories,
            EffectIds.AllArmors,
            EffectIds.AllKeyblades
        };

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

        private static Dictionary<uint, byte> baseItems = BaseItemAddresses.MakeBaseItemsDictionary();
        private static Dictionary<uint, byte> accessories = AccessoryAddresses.MakeAccessoriesDictionary();
        private static Dictionary<uint, byte> armors = ArmorAddresses.MakeArmorDictionary();
        private static Dictionary<uint, byte> keyblades = KeybladeAddresses.MakeKeybladesDictionary();

        private static Dictionary<uint, ushort> slots = EquipmentAddresses.MakeSoraInventorySlotsDictionary();

        public override bool StartAction()
        {
            bool success = true;
            EffectMode effectMode = Lookup(EffectMode.Itemaholic, EffectMode.BaseItems, EffectMode.Accessories, EffectMode.Armors, EffectMode.Keyblades);

            if (effectMode == EffectMode.Itemaholic || effectMode == EffectMode.BaseItems)
            {
                foreach (var (itemAddress, _) in baseItems)
                {
                    success &= Connector.Read8(itemAddress, out byte itemCount);
                    success &= Connector.Write8(itemAddress, (byte) (itemCount + 8));
                }
            }

            if (effectMode == EffectMode.Itemaholic || effectMode == EffectMode.Accessories)
            {
                foreach (var (itemAddress, _) in accessories)
                {
                    success &= Connector.Read8(itemAddress, out byte itemCount);
                    success &= Connector.Write8(itemAddress, (byte) (itemCount + 8));
                }
            }

            if (effectMode == EffectMode.Itemaholic || effectMode == EffectMode.Armors)
            {
                foreach (var (itemAddress, _) in armors)
                {
                    success &= Connector.Read8(itemAddress, out byte itemCount);
                    success &= Connector.Write8(itemAddress, (byte) (itemCount + 8));
                }
            }
            if (effectMode == EffectMode.Itemaholic || effectMode == EffectMode.Keyblades)
            {
                foreach (var (itemAddress, _) in keyblades)
                {
                    success &= Connector.Read8(itemAddress, out byte itemCount);
                    success &= Connector.Write8(itemAddress, (byte) (itemCount + 1));
                }
            }

            // Save all current slots so that when the effect ends,
            // we can reassign what the player had assigned to them beforehand.
            // This avoids issues with missing items breaking the game.
            // i.e. having a keyblade assigned that you don't have in your inventory
            foreach (var (slotAddress, _) in slots)
            {
                success &= Connector.Read16LE(slotAddress, out ushort slotValue);
                slots[slotAddress] = slotValue;
            }

            return success;
        }

        public override bool StopAction() {
            bool success = true;
            EffectMode effectMode = Lookup(EffectMode.Itemaholic, EffectMode.BaseItems, EffectMode.Accessories, EffectMode.Armors, EffectMode.Keyblades);
            
            // Write back all saved items for the given effect mode

            if(effectMode == EffectMode.Itemaholic || effectMode == EffectMode.BaseItems)
            {
                foreach (var (itemAddress, _) in baseItems)
                {
                    success &= Connector.Read8(itemAddress, out byte itemCount);
                    success &= Connector.Write8(itemAddress, (byte) (itemCount - 8));
                }
            }
            if(effectMode == EffectMode.Itemaholic || effectMode == EffectMode.Accessories)
            {
                foreach (var (itemAddress, _) in accessories)
                {
                    success &= Connector.Read8(itemAddress, out byte itemCount);
                    success &= Connector.Write8(itemAddress, (byte) (itemCount - 8));
                }
            }
            if(effectMode == EffectMode.Itemaholic || effectMode == EffectMode.Armors)
            {
                foreach (var (itemAddress, _) in armors)
                {
                    success &= Connector.Read8(itemAddress, out byte itemCount);
                    success &= Connector.Write8(itemAddress, (byte) (itemCount - 8));
                }
            }
            if(effectMode == EffectMode.Itemaholic || effectMode == EffectMode.Keyblades)
            {
                foreach (var (itemAddress, _) in keyblades)
                {
                    success &= Connector.Read8(itemAddress, out byte itemCount);
                    success &= Connector.Write8(itemAddress, (byte) (itemCount - 1));
                }
            }

            // Write back all saved slots
            foreach (var (slotAddress, slotValue) in slots)
            {
                success &= Connector.Write16LE(slotAddress, slotValue);
            }

            return success;
        }
    }
}