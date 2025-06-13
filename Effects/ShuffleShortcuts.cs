using ConnectorLib;
using CrowdControl.Games.SmartEffects;

namespace CrowdControl.Games.Packs.KH2FM;

public partial class KH2FM {
    [EffectHandler("shuffle_shortcuts")]
    public class ShuffleShortcuts : BaseEffect
    {
        public ShuffleShortcuts(KH2FM pack) : base(pack) { }

        public override EffectHandlerType Type => EffectHandlerType.Durational;

        public override IList<String> Codes { get; } = [EffectIds.ShuffleShortcuts];

        public override Mutex Mutexes { get; } = [EffectIds.ShuffleShortcuts];

        private readonly Random random = new();
        private readonly Dictionary<int, Tuple<int, int>> values = new()
            {
                { BaseItemAddresses.Potion, new Tuple<int, int>(QuickSlotValues.PotionQuickSlotValue, ItemValues.Potion) }, { BaseItemAddresses.HiPotion, new Tuple<int, int>(QuickSlotValues.HiPotionQuickSlotValue, ItemValues.HiPotion) },
                { BaseItemAddresses.MegaPotion, new Tuple<int, int>(QuickSlotValues.MegaPotionQuickSlotValue, ItemValues.MegaPotion) }, { BaseItemAddresses.Ether, new Tuple<int, int>(QuickSlotValues.EtherQuickSlotValue, ItemValues.Ether) },
                { BaseItemAddresses.MegaEther, new Tuple<int, int>(QuickSlotValues.MegaEtherQuickSlotValue, ItemValues.MegaEther) }, { BaseItemAddresses.Elixir, new Tuple<int, int>(QuickSlotValues.ElixirQuickSlotValue, ItemValues.Elixir) },
                { BaseItemAddresses.Megalixir, new Tuple<int, int>(QuickSlotValues.MegalixirQuickSlotValue, ItemValues.Megalixir) }, { MagicAddresses.Fire, new Tuple<int, int>(QuickSlotValues.FireQuickSlotValue, MagicValues.Fire) },
                { MagicAddresses.Blizzard, new Tuple<int, int>(QuickSlotValues.BlizzardQuickSlotValue, MagicValues.Blizzard) }, { MagicAddresses.Thunder, new Tuple<int, int>(QuickSlotValues.ThunderQuickSlotValue, MagicValues.Thunder) },
                { MagicAddresses.Cure, new Tuple<int, int>(QuickSlotValues.CureQuickSlotValue, MagicValues.Cure) }, { MagicAddresses.Reflect, new Tuple<int, int>(QuickSlotValues.ReflectQuickSlotValue, MagicValues.Reflect) },
                { MagicAddresses.Magnet, new Tuple<int, int>(QuickSlotValues.MagnetQuickSlotValue, MagicValues.Magnet) }
            };

        private static ushort shortcut1;
        private static ushort shortcut2;
        private static ushort shortcut3;
        private static ushort shortcut4;

        private static ulong shortcut1_set;
        private static ulong shortcut2_set;
        private static ulong shortcut3_set;
        private static ulong shortcut4_set;

        private (int, bool) CheckQuickSlot(IPS2Connector Connector, int key, Tuple<int, int> value, int shortcutNumber)
        {
            bool success = true;
            if (key != MagicAddresses.Fire && key != MagicAddresses.Blizzard && key != MagicAddresses.Thunder &&
                key != MagicAddresses.Cure && key != MagicAddresses.Reflect && key != MagicAddresses.Magnet)
            {
                success &= Connector.Read16LE((ulong)key, out ushort itemValue);

                success &= Connector.Write16LE((ulong)key, (ushort)(itemValue + 1));

                switch (shortcutNumber)
                {
                    case 1:
                        shortcut1_set = (ulong)key;
                        success &= Connector.Write16LE(EquipmentAddresses.SoraItemSlot1, (ushort)(value.Item2));
                        break;
                    case 2:
                        shortcut2_set = (ulong)key;
                        success &= Connector.Write16LE(EquipmentAddresses.SoraItemSlot2, (ushort)(value.Item2));
                        break;
                    case 3:
                        shortcut3_set = (ulong)key;
                        success &= Connector.Write16LE(EquipmentAddresses.SoraItemSlot3, (ushort)(value.Item2));
                        break;
                    case 4:
                        shortcut4_set = (ulong)key;
                        success &= Connector.Write16LE(EquipmentAddresses.SoraItemSlot4, (ushort)(value.Item2));
                        break;
                }

                return (value.Item1, success);
            }

            success &= Connector.Read8((ulong)key, out byte byteValue);

            if (byteValue == 0)
            {
                success &= Connector.Write8((ulong)key, (byte)value.Item2);

                switch (shortcutNumber)
                {
                    case 1:
                        shortcut1_set = (ulong)key;
                        break;
                    case 2:
                        shortcut2_set = (ulong)key;
                        break;
                    case 3:
                        shortcut3_set = (ulong)key;
                        break;
                    case 4:
                        shortcut4_set = (ulong)key;
                        break;
                }
            }

            if (key == MagicAddresses.Fire)
                return (QuickSlotValues.FireQuickSlotValue, success);
            if (key == MagicAddresses.Blizzard)
                return (QuickSlotValues.BlizzardQuickSlotValue, success);
            if (key == MagicAddresses.Thunder)
                return (QuickSlotValues.ThunderQuickSlotValue, success);
            if (key == MagicAddresses.Cure)
                return (QuickSlotValues.CureQuickSlotValue, success);
            if (key == MagicAddresses.Reflect)
                return (QuickSlotValues.ReflectQuickSlotValue, success);
            if (key == MagicAddresses.Magnet)
                return (QuickSlotValues.MagnetQuickSlotValue, success);

            return (MiscValues.None, success);
        }
        
        

        public override bool StartAction()
        {
            bool success = true;
            // Save the values before the shuffle
            success &= Connector.Read16LE(EquipmentAddresses.SoraQuickMenuSlot1, out shortcut1);
            success &= Connector.Read16LE(EquipmentAddresses.SoraQuickMenuSlot2, out shortcut2);
            success &= Connector.Read16LE(EquipmentAddresses.SoraQuickMenuSlot3, out shortcut3);
            success &= Connector.Read16LE(EquipmentAddresses.SoraQuickMenuSlot4, out shortcut4);

            int key1 = values.Keys.ToList()[random.Next(values.Keys.Count)];
            int key2 = values.Keys.ToList()[random.Next(values.Keys.Count)];
            int key3 = values.Keys.ToList()[random.Next(values.Keys.Count)];
            int key4 = values.Keys.ToList()[random.Next(values.Keys.Count)];

            (int value1, bool success1) = CheckQuickSlot(Connector, key1, values[key1], 1);
            (int value2, bool success2) = CheckQuickSlot(Connector, key2, values[key2], 2);
            (int value3, bool success3) = CheckQuickSlot(Connector, key3, values[key3], 3);
            (int value4, bool success4) = CheckQuickSlot(Connector, key4, values[key4], 4);

            success &= success1 && success2 && success3 && success4;

            success &= Connector.Write16LE(EquipmentAddresses.SoraQuickMenuSlot1, (ushort)value1);
            success &= Connector.Write16LE(EquipmentAddresses.SoraQuickMenuSlot2, (ushort)value2);
            success &= Connector.Write16LE(EquipmentAddresses.SoraQuickMenuSlot3, (ushort)value3);
            success &= Connector.Write16LE(EquipmentAddresses.SoraQuickMenuSlot4, (ushort)value4);

            return success;
        }

        public override bool StopAction() {
            bool success = true;
            success &= Connector.Write16LE(EquipmentAddresses.SoraQuickMenuSlot1, shortcut1);
            success &= Connector.Write16LE(EquipmentAddresses.SoraQuickMenuSlot2, shortcut2);
            success &= Connector.Write16LE(EquipmentAddresses.SoraQuickMenuSlot3, shortcut3);
            success &= Connector.Write16LE(EquipmentAddresses.SoraQuickMenuSlot4, shortcut4);

            if (shortcut1_set != 0)
            {
                success &= Connector.Write8(shortcut1_set, 0);
                shortcut1_set = 0;
            }
            if (shortcut2_set != 0)
            {
                success &= Connector.Write8(shortcut2_set, 0);
                shortcut2_set = 0;
            }
            if (shortcut3_set != 0)
            {
                success &= Connector.Write8(shortcut3_set, 0);
                shortcut3_set = 0;
            }
            if (shortcut4_set != 0)
            {
                success &= Connector.Write8(shortcut4_set, 0);
                shortcut4_set = 0;
            }

            return success;
        }
    }
}