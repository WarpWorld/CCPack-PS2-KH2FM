using ConnectorLib;
using CrowdControl.Games.SmartEffects;

namespace CrowdControl.Games.Packs.KH2FM;

public partial class KH2FM {
    [EffectHandler("backseat_driver", "valor_form", "wisdom_form", "limit_form", "master_form", "final_form")]
    public class BackseatDriver : BaseEffect
    {
        private ushort currentKeyblade;
        private ushort currentValorKeyblade;
        private ushort currentMasterKeyblade;
        private ushort currentFinalKeyblade;
        private readonly List<uint> driveFormValues =
        [
            ReactionValues.ReactionValor,
            ReactionValues.ReactionWisdom,
            ReactionValues.ReactionLimit,
            ReactionValues.ReactionMaster,
            ReactionValues.ReactionFinal //ConstantValues.ReactionAnti
        ];

        public BackseatDriver(KH2FM pack) : base(pack) { }

        public override EffectHandlerType Type => EffectHandlerType.Instant;

        public override IList<String> Codes { get; } =
        [
            EffectIds.BackseatDriver, 
            EffectIds.ValorForm, 
            EffectIds.WisdomForm, 
            EffectIds.LimitForm, 
            EffectIds.MasterForm, 
            EffectIds.FinalForm
        ];

        public override Mutex Mutexes { get; } =
        [
            EffectIds.IAmDarkness, 
            EffectIds.BackseatDriver,
            EffectIds.ValorForm, 
            EffectIds.WisdomForm, 
            EffectIds.LimitForm, 
            EffectIds.MasterForm, 
            EffectIds.FinalForm,
            EffectIds.HeroSora, 
            EffectIds.ZeroSora
        ];

        public override bool StartAction()
        {
            bool success = true;
            // Get us out of a Drive first if we are in one
            success &= Connector.WriteFloat(DriveAddresses.DriveTime, MiscValues.None);

            Thread.Sleep(200);

            int formIndex = Lookup(
                new Random().Next(driveFormValues.Count), // Backseat Driver is a random form
                0, // Valor Form
                1, // Wisdom Form
                2, // Limit Form
                3, // Master Form
                4 // Final Form
            );

            success &= Connector.Read16LE(EquipmentAddresses.SoraWeaponSlot, out currentKeyblade);

            // Set the current keyblade in the slot for the drive form
            if (formIndex == 0) // Valor
            {
                success &= Connector.Read16LE(EquipmentAddresses.SoraValorWeaponSlot, out currentValorKeyblade);
                
                if (currentValorKeyblade < 0x41 || currentValorKeyblade == 0x81) // 0x81 seems to be a default (maybe just randomizer)
                {
                    success &= Connector.Write16LE(EquipmentAddresses.SoraValorWeaponSlot, currentKeyblade);
                }
            }
            else if (formIndex == 3) // Master
            {
                success &= Connector.Read16LE(EquipmentAddresses.SoraMasterWeaponSlot, out currentMasterKeyblade);

                if (currentMasterKeyblade < 0x41 || currentMasterKeyblade == 0x44) // 0x44 seems to be a default (maybe just randomizer)
                {
                    success &= Connector.Write16LE(EquipmentAddresses.SoraMasterWeaponSlot, currentKeyblade);
                }
            }
            else if (formIndex == 4) // Final
            {
                success &= Connector.Read16LE(EquipmentAddresses.SoraFinalWeaponSlot, out currentFinalKeyblade);

                if (currentFinalKeyblade < 0x41 || currentFinalKeyblade == 0x45) // 0x45 seems to be a default (maybe just randomizer)
                {
                    success &= Connector.Write16LE(EquipmentAddresses.SoraFinalWeaponSlot, currentKeyblade);
                }
            }

            success &= Connector.Write16LE(DriveAddresses.ReactionPopup, (ushort)MiscValues.None);
            success &= Connector.Write16LE(DriveAddresses.ReactionOption, (ushort)driveFormValues[formIndex]);
            success &= Connector.Write16LE(DriveAddresses.ReactionEnable, (ushort)MiscValues.None);
            
            Utils.TriggerReaction(Connector);

            return success;
        }
    }
}