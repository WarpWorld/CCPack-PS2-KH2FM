using ConnectorLib;
using CrowdControl.Games.SmartEffects;

namespace CrowdControl.Games.Packs.KH2FM;

public partial class KH2FM {
    [EffectHandler("itemaholic")]
    public class Itemaholic : BaseEffect
    {
        public Itemaholic(KH2FM pack) : base(pack) { }

        public override EffectHandlerType Type => EffectHandlerType.Durational;

        public override IList<String> Codes { get; } = [EffectIds.Itemaholic];

        public override Mutex Mutexes { get; } =
        [
            EffectIds.Itemaholic,
            EffectIds.SpringCleaning,
            EffectIds.HeroSora,
            EffectIds.ZeroSora
        ];

        private readonly Dictionary<uint, byte> items = MiscAddresses.MakeInventoryDictionary();

        private readonly Dictionary<uint, ushort> slots = EquipmentAddresses.MakeSoraInventorySlotsDictionary();

        public override bool StartAction()
        {
            bool success = true;
            // Save all current items, before writing max value to them
            foreach (var (itemAddress, _) in items)
            {
                success &= Connector.Read8(itemAddress, out byte itemCount);

                items[itemAddress] = itemCount;

                success &= Connector.Write8(itemAddress, byte.MaxValue);
            }

            // Save all current slots
            foreach (var (slotAddress, _) in slots)
            {
                success &= Connector.Read16LE(slotAddress, out ushort slotValue);

                slots[slotAddress] = slotValue;
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
                success &= Connector.Write16LE(slotAddress, slotValue);
            }

            return success;
        }
    }
}