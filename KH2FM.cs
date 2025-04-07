using ConnectorLib;
using CrowdControl.Common;
using JetBrains.Annotations;
using ConnectorType = CrowdControl.Common.ConnectorType;
using Log = CrowdControl.Common.Log;
using Timer = System.Timers.Timer;
using CrowdControl.Games.SmartEffects;
// ReSharper disable CommentTypo

//ccpragma { "include" : [ ".\\Effects\\*.cs", ".\\Common\\*.cs", ".\\Common\\Addresses\\*.cs", ".\\Common\\Values\\*.cs" ] }
namespace CrowdControl.Games.Packs.KH2FM;

[UsedImplicitly]
public partial class KH2FM : PS2EffectPack, IHandlerCollection
{
    public override Game Game { get; } = new("Kingdom Hearts II: Final Mix", "KH2FM", "PS2", ConnectorType.PS2Connector);

    public override EffectList Effects { get; } = new Effect[] {
        new("Heal Sora", EffectIds.HealSora) {
            Price = 50,
            Description = "Heal Sora to Max HP.",
        },
        new ("One Hit KO Sora", EffectIds.OneShotSora) {
            Price = 50,
            Description = "Sora only has 1 HP until the effect is over.",
            Duration = SITimeSpan.FromSeconds(30)
        },
        new ("Invulnerability", EffectIds.Invulnerability) {
            Price = 50,
            Description = "Continuously restore Sora's HP, making him nearly impossible to kill.",
            Duration = SITimeSpan.FromSeconds(30)
        },
        new("Give Munny", EffectIds.GiveMunny) {
            Price = 50,
            Description = "Give Munny to Sora",
            Quantity = 1000,
        },
        new("Take Munny", EffectIds.TakeMunny) {
            Price = 50,
            Description = "Take Munny from Sora",
            Quantity = 1000,
        },
        new ("Tiny Weapon", EffectIds.TinyWeapon) {
            Price = 50,
            Description = "Set Sora's Weapon size to be tiny.",
            Duration = SITimeSpan.FromSeconds(60)
        },
        new ("Giant Weapon", EffectIds.GiantWeapon) {
            Price = 50,
            Description = "Set Sora's Weapon size to be huge.",
            Duration = SITimeSpan.FromSeconds(60)
        },
        new ("Struggling", EffectIds.Struggling) {
            Price = 50,
            Description = "Change Sora's weapon to the Struggle Bat.",
            Duration = SITimeSpan.FromSeconds(60)
        },
        // new ("Ultima", EffectIds.Ultima) {
        //     Price = 50,
        //     Description = "Change Sora's weapon to the Ultima Weapon",
        //     Duration = SITimeSpan.FromSeconds(60)
        // },
        new ("I Am Darkness", EffectIds.IAmDarkness) {
            Price = 50,
            Description = "Force Sora into Anti Form.",
        },
        new ("Backseat Driver", EffectIds.BackseatDriver) {
            Price = 50,
            Description = "Trigger a random Drive form.",
        },
        new ("Valor Form", EffectIds.ValorForm) {
            Price = 50,
            Description = "Trigger Valor Form.",
        },
        new ("Wisdom Form", EffectIds.WisdomForm) {
            Price = 50,
            Description = "Trigger Wisdom Form.",
        },
        new ("Limit Form", EffectIds.LimitForm) {
            Price = 50,
            Description = "Trigger Limit Form.",
        },
        new ("Master Form", EffectIds.MasterForm) {
            Price = 50,
            Description = "Trigger Master Form.",
        },
        new ("Final Form", EffectIds.FinalForm) {
            Price = 50,
            Description = "Trigger Final Form.",
        },
        new ("Growth Spurt", EffectIds.GrowthSpurt) {
            Price = 50,
            Description = "Give Sora Max Growth Abilities.",
            Duration = SITimeSpan.FromSeconds(60)
        },
        new ("Hastega", EffectIds.Hastega) {
            Price = 50,
            Description = "Set Sora's Speed to be super fast.",
            Duration = SITimeSpan.FromSeconds(60)
        },
        new ("Slowga", EffectIds.Slowga) {
            Price = 50,
            Description = "Set Sora's Speed to be super slow.",
            Duration = SITimeSpan.FromSeconds(60)
        },
        new ("Who Am I?", EffectIds.WhoAmI) {
            Price = 50,
            Description = "Set Sora to a random different character or outfit.",
            Duration = SITimeSpan.FromSeconds(60)
        },
        new ("Who Are They?", EffectIds.WhoAreThey) {
            Price = 50,
            Description = "Set Donald and Goofy to random allies or oufits.",
            Duration = SITimeSpan.FromSeconds(60)
        },
        new ("Hostile Party", EffectIds.HostileParty) {
            Price = 50,
            Description = "Set Donald and Goofy to random enemies.",
            Duration = SITimeSpan.FromSeconds(60)
        },
        new ("Shuffle Shortcuts", EffectIds.ShuffleShortcuts) {
            Price = 50,
            Description = "Set Sora's Shortcuts to random commands.",
            Duration = SITimeSpan.FromSeconds(60)
        },
        // new ("Kill Sora", EffectIds.KillSora) {
        //     Price = 50,
        //     Description = "Instantly Kill Sora.",
        // },
        // new ("Randomize Controls", EffectIds.RandomizeControls) {
        //     Price = 50,
        //     Description = "Randomize the controls to the game.",
        //     Duration = SITimeSpan.FromSeconds(60)
        // },
        new ("Expert Magician", EffectIds.ExpertMagician) {
            Price = 50,
            Description = "Give Sora Max Magic and lower the cost of Magic.",
            Duration = SITimeSpan.FromSeconds(30)
        },
        new ("Amnesiac Magician", EffectIds.AmnesiacMagician) {
            Price = 50,
            Description = "Take away all of Sora's Magic.",
            Duration = SITimeSpan.FromSeconds(60)
        },
        new ("Itemaholic", EffectIds.Itemaholic) {
            Price = 50,
            Description = "Fill Sora's inventory with all items, accessories, armor and weapons.",
            Duration = SITimeSpan.FromSeconds(60)
        },
        new ("Spring Cleaning", EffectIds.SpringCleaning) {
            Price = 50,
            Description = "Remove all items, accessories, armor and weapons from Sora's inventory.",
            Duration = SITimeSpan.FromSeconds(60)
        },
        new ("Summon Chauffeur", EffectIds.SummonChauffeur) {
            Price = 50,
            Description = "Give all Drives and Summons to Sora.",
            Duration = SITimeSpan.FromSeconds(60)
        },
        new ("Summon Trainer", EffectIds.SummonTrainer) {
            Price = 50,
            Description = "Remove all Drives and Summons from Sora.",
            Duration = SITimeSpan.FromSeconds(60)
        },
        new ("Pro Codes", EffectIds.ProCodes) {
            Price = 50,
            Description = "Set Sora to consistently lose HP, MP and Drive Gauges",
            Duration = SITimeSpan.FromSeconds(60)
        },
        new ("EZ Codes", EffectIds.EzCodes) {
            Price = 50,
            Description = "Set Sora to consistently gain HP, MP and Drive Gauges",
            Duration = SITimeSpan.FromSeconds(60)
        },
    };

    public KH2FM(UserRecord player, Func<CrowdControlBlock, bool> responseHandler, Action<object> statusUpdateHandler) : base(player, responseHandler, statusUpdateHandler)
    {
        Log.FileOutput = true;

        Timer timer = new(1000.0);
        timer.Elapsed += (_, _) =>
        {
            if (Utils.CheckTPose(Connector))
            {
                Utils.FixTPose(Connector);
            }
        };

        timer.Start();
    }

    #region Game State Checks

    public bool IsGameInPlay() => IsReady(null);

    // do all actual statechecking here - kat
    protected override GameState GetGameState()
    {
        bool success = true;
        string gameStateString = string.Empty;

        success &= Connector.Read32LE(0x2035F314, out uint gameState);
        success &= Connector.Read32LE(0x20341708, out uint animationStateOffset);
        success &= Connector.Read32LE(0x2033CC38, out uint cameraLockState);
        success &= Connector.Read32LE(0x21C60CE0, out uint transitionState);

        if (!success)
        {
            return GameState.Unknown;
        }

        // Set the state

        if (gameState == 1 && cameraLockState == 0 && transitionState == 1 && animationStateOffset != 0)
        {
            gameStateString = "Ready";
        }
        else if (gameState == 1 && cameraLockState == 1 && transitionState == 1 && animationStateOffset != 0)
        {
            gameStateString = "Uncontrollable";
        }
        else if (gameState == 0 && cameraLockState == 0 && transitionState == 0 && animationStateOffset == 0)
        {
            gameStateString = "Dead";
        }
        else if (gameState == 0 && cameraLockState == 0 && transitionState == 1 && animationStateOffset != 0)
        {
            gameStateString = "Pause";
        }
        else if (gameState == 1 && cameraLockState == 1 && transitionState == 0 && animationStateOffset == 0)
        {
            gameStateString = "Cutscene";
        }
        else
        {
            gameStateString = "Unknown";
        }

        if (gameStateString != "Ready")
        {
            return GameState.WrongMode;
        }
        // it would be awesome if someone could fill this in a bit more - kat

        return GameState.Ready;
    }

    #endregion

    public override bool StopAllEffects()
    {
        return base.StopAllEffects();
    }
}
