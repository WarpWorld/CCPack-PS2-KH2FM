using ConnectorLib;
using CrowdControl.Games.SmartEffects;
using System;
using System.Collections.Generic;
using System.Threading;

namespace CrowdControl.Games.Packs.KH2FM;

// Does not work, at least not while playing as Roxas

public partial class KH2FM {
    [EffectHandler("who_am_i")]
    public class WhoAmI : BaseEffect
    {
        private readonly List<int> values =
        [
            CharacterValues.KH1Sora,
            CharacterValues.CardSora,
            CharacterValues.DieSora,
            CharacterValues.LionSora,
            CharacterValues.ChristmasSora,
            CharacterValues.SpaceParanoidsSora,
            CharacterValues.TimelessRiverSora,
            CharacterValues.Roxas,
            CharacterValues.DualwieldRoxas,
            CharacterValues.MickeyRobed,
            CharacterValues.Mickey,
            CharacterValues.Minnie,
            CharacterValues.Donald,
            CharacterValues.Goofy,
            CharacterValues.BirdDonald,
            CharacterValues.TortoiseGoofy,
            // CharacterValues.HalloweenDonald, CharacterValues.HalloweenGoofy, - Causes crash?
            // CharacterValues.ChristmasDonald, CharacterValues.ChristmasGoofy,
            CharacterValues.SpaceParanoidsDonald,
            CharacterValues.SpaceParanoidsGoofy,
            CharacterValues.TimelessRiverDonald,
            CharacterValues.TimelessRiverGoofy,
            CharacterValues.Beast,
            CharacterValues.Mulan,
            CharacterValues.Ping,
            CharacterValues.Hercules,
            CharacterValues.Auron,
            CharacterValues.Aladdin,
            CharacterValues.JackSparrow,
            CharacterValues.HalloweenJack,
            CharacterValues.ChristmasJack,
            CharacterValues.Simba,
            CharacterValues.Tron,
            CharacterValues.ValorFormSora,
            CharacterValues.WisdomFormSora,
            CharacterValues.LimitFormSora,
            CharacterValues.MasterFormSora,
            CharacterValues.FinalFormSora,
            CharacterValues.AntiFormSora
        ];
        
        public WhoAmI(KH2FM pack) : base(pack) { }

        public override EffectHandlerType Type => EffectHandlerType.Durational;

        public override IList<String> Codes { get; } = [EffectIds.WhoAmI];

        public override Mutex Mutexes { get; } = new [] { 
            EffectIds.WhoAmI, 
            EffectIds.WhoAreThey, 
            EffectIds.HostileParty,

            EffectIds.IAmDarkness,
            EffectIds.BackseatDriver,
            EffectIds.ValorForm,
            EffectIds.WisdomForm,
            EffectIds.LimitForm,
            EffectIds.MasterForm,
            EffectIds.FinalForm,
            EffectIds.HeroSora,
            EffectIds.ZeroSora
        };

        public override bool StartAction()
        {
            bool success = true;

            ushort randomModel = (ushort)values[new Random().Next(values.Count)];

            success &= Connector.Read16LE(CharacterAddresses.Sora, out ushort currentSora);

            success &= Connector.Write16LE(CharacterAddresses.Sora, randomModel);
            success &= Connector.Write16LE(CharacterAddresses.LionSora, randomModel);
            success &= Connector.Write16LE(CharacterAddresses.ChristmasSora, randomModel);
            success &= Connector.Write16LE(CharacterAddresses.SpaceParanoidsSora, randomModel);
            success &= Connector.Write16LE(CharacterAddresses.TimelessRiverSora, randomModel);

            //int randomIndex = new Random().Next(values.Count);

            //// Set Valor Form to Random Character so we can activate form
            //success &= Connector.Write16LE(CharacterAddresses.ValorFormSora, (ushort)values[randomIndex]);

            //success &= Connector.Write16LE(DriveAddresses.ReactionPopup, (ushort)MiscValues.None);
            //success &= Connector.Write16LE(DriveAddresses.ReactionOption, (ushort)ReactionValues.ReactionValor);
            //success &= Connector.Write16LE(DriveAddresses.ReactionEnable, (ushort)MiscValues.None);

            //Thread.Sleep(200);

            //Utils.TriggerReaction(Connector);

            return success;
        }

        public override bool StopAction() {
            bool success = true;

            success &= Connector.Write16LE(CharacterAddresses.Sora, (ushort)CharacterValues.Sora);
            success &= Connector.Write16LE(CharacterAddresses.LionSora, (ushort)CharacterValues.LionSora);
            success &= Connector.Write16LE(CharacterAddresses.ChristmasSora, (ushort)CharacterValues.ChristmasSora);
            success &= Connector.Write16LE(CharacterAddresses.SpaceParanoidsSora, (ushort)CharacterValues.SpaceParanoidsSora);
            success &= Connector.Write16LE(CharacterAddresses.TimelessRiverSora, (ushort)CharacterValues.TimelessRiverSora);

            //success &= Connector.Write16LE(CharacterAddresses.ValorFormSora, (ushort)CharacterValues.ValorFormSora);
            
            return success;
        }
    }
}