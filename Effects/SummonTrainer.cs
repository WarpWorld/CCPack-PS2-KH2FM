using CrowdControl.Games.SmartEffects;

namespace CrowdControl.Games.Packs.KH2FM;

public partial class KH2FM {
    [EffectHandler("summon_trainer")]
    public class SummonTrainer : BaseEffect
    {
        public SummonTrainer(KH2FM pack) : base(pack) { }

        public override EffectHandlerType Type => EffectHandlerType.Durational;

        public override IList<String> Codes { get; } = [EffectIds.SummonTrainer];

        public override Mutex Mutexes { get; } =
        [
            EffectIds.SummonChauffeur,
            EffectIds.SummonTrainer,
            EffectIds.HeroSora,
            EffectIds.ZeroSora
        ];

        // Used to store all the information about what held items Sora had before
        private static Dictionary<uint, byte> drivesSummons = new()
        {
            { DriveAddresses.DriveForms, 0 }, { DriveAddresses.DriveLimitForm, 0 },
            //{ (uint)ConstantAddresses.UkeleleBaseballCharm, 0 }, 
            { SummonAddresses.LampFeatherCharm, 0 },

            { DriveAddresses.Drive, 0 }, { DriveAddresses.MaxDrive, 0 }
        };

        public override bool StartAction()
        {
            bool success = true;
            // Save all current items, before writing max value to them
            foreach (var (driveSummon, _) in drivesSummons)
            {
                success &= Connector.Read8(driveSummon, out byte value);

                drivesSummons[driveSummon] = value;

                success &= Connector.Write8(driveSummon, byte.MinValue);
            }

            return success;
        }

        public override bool StopAction() {
            bool success = true;
            // Write back all saved items
            foreach (var (driveSummon, value) in drivesSummons)
            {
                success &= Connector.Write8(driveSummon, value);
            }
            return success;
        }
    }
}