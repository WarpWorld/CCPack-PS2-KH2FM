using CrowdControl.Games.SmartEffects;

namespace CrowdControl.Games.Packs.KH2FM;

public partial class KH2FM {
    [EffectHandler("summon_chauffeur")]
    public class SummonChauffeur : BaseEffect
    {
        public SummonChauffeur(KH2FM pack) : base(pack) { }

        public override EffectHandlerType Type => EffectHandlerType.Durational;

        public override IList<String> Codes { get; } = [EffectIds.SummonChauffeur];

        public override Mutex Mutexes { get; } =
        [
            EffectIds.SummonChauffeur,
            EffectIds.SummonTrainer,
            EffectIds.HeroSora,
            EffectIds.ZeroSora
        ];

        // Used to store all the information about what held items Sora had before
        private readonly Dictionary<uint, byte> drivesSummons = new()
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

                if (driveSummon == DriveAddresses.DriveForms)
                {
                    success &= Connector.Write8(driveSummon, 127);
                }
                else if (driveSummon == DriveAddresses.DriveLimitForm)
                {
                    success &= Connector.Write8(driveSummon, 8);
                }
                //else if (driveSummon == ConstantAddresses.UkeleleBaseballCharm)
                //{
                //    Connector.Write8(value, 9);
                //}
                else if (driveSummon == SummonAddresses.LampFeatherCharm)
                {
                    success &= Connector.Write8(driveSummon, 48);
                }
                else
                {
                    success &= Connector.Write8(driveSummon, byte.MaxValue);
                }
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