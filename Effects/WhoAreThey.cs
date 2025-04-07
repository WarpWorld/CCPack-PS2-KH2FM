using ConnectorLib;
using CrowdControl.Games.SmartEffects;

namespace CrowdControl.Games.Packs.KH2FM;

// Does not work, at least not while playing as Roxas

public partial class KH2FM {
    [EffectHandler("who_are_they")]
    public class WhoAreThey : BaseEffect
    {
        private readonly List<int> values =
        [
            CharacterValues.Minnie,
            CharacterValues.Donald,
            CharacterValues.Goofy,
            CharacterValues.BirdDonald,
            CharacterValues.TortoiseGoofy,
            //CharacterValues.HalloweenDonald, CharacterValues.HalloweenGoofy, - Causes crash?
            //CharacterValues.ChristmasDonald, CharacterValues.ChristmasGoofy, 
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
            CharacterValues.Riku,
            CharacterValues.AxelFriend,
            CharacterValues.LeonFriend,
            CharacterValues.YuffieFriend,
            CharacterValues.TifaFriend,
            CharacterValues.CloudFriend
        ];
        
        public WhoAreThey(KH2FM pack) : base(pack) { }

        public override EffectHandlerType Type => EffectHandlerType.Durational;

        public override IList<String> Codes { get; } = [EffectIds.WhoAreThey];

        public override Mutex Mutexes { get; } = [EffectIds.WhoAmI, EffectIds.WhoAreThey, EffectIds.HostileParty];

        public override bool StartAction()
        {
            bool success = true;
            ushort donald = (ushort)values[new Random().Next(values.Count)];
            ushort goofy = (ushort)values[new Random().Next(values.Count)];

            success &= Connector.Write16LE(CharacterAddresses.Donald, donald);
            success &= Connector.Write16LE(CharacterAddresses.BirdDonald, donald);
            success &= Connector.Write16LE(CharacterAddresses.ChristmasDonald, donald);
            success &= Connector.Write16LE(CharacterAddresses.SpaceParanoidsDonald, donald);
            success &= Connector.Write16LE(CharacterAddresses.TimelessRiverDonald, donald);


            success &= Connector.Write16LE(CharacterAddresses.Goofy, goofy);
            success &= Connector.Write16LE(CharacterAddresses.TortoiseGoofy, goofy);
            success &= Connector.Write16LE(CharacterAddresses.ChristmasGoofy, goofy);
            success &= Connector.Write16LE(CharacterAddresses.SpaceParanoidsGoofy, goofy);
            success &= Connector.Write16LE(CharacterAddresses.TimelessRiverGoofy, goofy);

            return success;
        }

        public override bool StopAction() {
            bool success = true;
            success &= Connector.Write16LE(CharacterAddresses.Donald, CharacterValues.Donald);
            success &= Connector.Write16LE(CharacterAddresses.BirdDonald, CharacterValues.BirdDonald);
            success &= Connector.Write16LE(CharacterAddresses.ChristmasDonald, CharacterValues.ChristmasDonald);
            success &= Connector.Write16LE(CharacterAddresses.SpaceParanoidsDonald, CharacterValues.SpaceParanoidsDonald);
            success &= Connector.Write16LE(CharacterAddresses.TimelessRiverDonald, CharacterValues.TimelessRiverDonald);


            success &= Connector.Write16LE(CharacterAddresses.Goofy, CharacterValues.Goofy);
            success &= Connector.Write16LE(CharacterAddresses.TortoiseGoofy, CharacterValues.TortoiseGoofy);
            success &= Connector.Write16LE(CharacterAddresses.ChristmasGoofy, CharacterValues.ChristmasGoofy);
            success &= Connector.Write16LE(CharacterAddresses.SpaceParanoidsGoofy, CharacterValues.SpaceParanoidsGoofy);
            success &= Connector.Write16LE(CharacterAddresses.TimelessRiverGoofy, CharacterValues.TimelessRiverGoofy);

            return success;
        }
    }
}