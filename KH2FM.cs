using ConnectorLib;
using ConnectorType = CrowdControl.Common.ConnectorType;
using CrowdControl.Common;
using JetBrains.Annotations;
using Log = CrowdControl.Common.Log;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Timer = System.Timers.Timer;
// ReSharper disable CommentTypo

namespace CrowdControl.Games.Packs.KH2FM;

[UsedImplicitly]
public class KH2FM : PS2EffectPack
{
    public override Game Game { get; } = new(name: "Kingdom Hearts II: Final Mix", id: "KH2FM", path: "PS2", ConnectorType.PS2Connector);

    private readonly KH2FMCrowdControl kh2FMCrowdControl;

    public override EffectList Effects { get; }

    public KH2FM(UserRecord player, Func<CrowdControlBlock, bool> responseHandler, Action<object> statusUpdateHandler) : base(player, responseHandler, statusUpdateHandler)
    {
        Log.FileOutput = true;

        kh2FMCrowdControl = new KH2FMCrowdControl();
        Effects = kh2FMCrowdControl.Options.Select(x => new Effect(x.Value.Name, x.Value.Id)
        {
            Price = (uint)x.Value.Cost,
            Description = x.Value.Description,
            Duration = SITimeSpan.FromSeconds(x.Value.DurationSeconds),
            Category = x.Value.GetEffectCategory(),
            Group = x.Value.GetEffectGroup(),
        }).ToList();

        Timer timer = new(1000.0);
        timer.Elapsed += (_, _) =>
        {
            if (kh2FMCrowdControl.CheckTPose(Connector))
            {
                kh2FMCrowdControl.FixTPose(Connector);
            }
        };

        timer.Start();
    }

    private bool GetOptionForRequest(EffectRequest request, [MaybeNullWhen(false)] out Option option)
    {
        string effectId = FinalCode(request);
        Log.Debug($"Requested Effect Id (FinalCode): {effectId}");
        var availableEffectIds = kh2FMCrowdControl.Options.Select((pair) => pair.Key).ToList();
        Log.Debug("Available Effect Ids: " + string.Join(", ", availableEffectIds));
        return kh2FMCrowdControl.Options.TryGetValue(effectId, out option);
    }

    #region Game State Checks

    private bool IsGameInPlay() => IsReady(null);

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

    protected override void StartEffect(EffectRequest request)
    {
        if (!GetOptionForRequest(request, out Option? option))
        {
            Respond(request, EffectStatus.FailPermanent, StandardErrors.UnknownEffect, request);
            return;
        }

        switch (option.effectFunction)
        {
            case EffectFunction.StartTimed:
                var timed = StartTimed(
                    request: request,
                    startCondition: () => IsGameInPlay(),
                    continueCondition: () => IsGameInPlay(),
                    continueConditionInterval: TimeSpan.FromMilliseconds(500),
                    action: () => option.StartEffect(Connector),
                    mutex: kh2FMCrowdControl.OptionConflicts[option.Id]
                );
                timed.WhenCompleted.Then(_ => option.StopEffect(Connector));
                break;
            case EffectFunction.RepeatAction:
                var action = RepeatAction(
                    request: request,
                    startCondition: () => IsGameInPlay(),
                    startAction: () => option.StartEffect(Connector),
                    startRetry: TimeSpan.FromSeconds(1),
                    refreshCondition: () => IsGameInPlay(),
                    refreshRetry: TimeSpan.FromMilliseconds(500),
                    refreshAction: () => option.DoEffect(Connector),
                    refreshInterval: TimeSpan.FromMilliseconds(option.RefreshInterval),
                    extendOnFail: true,
                    mutex: kh2FMCrowdControl.OptionConflicts[option.Id]
                );
                action.WhenCompleted.Then(_ => option.StopEffect(Connector));
                break;
            default:
                TryEffect(
                    request: request,
                    condition: () => IsGameInPlay(),
                    action: () => option.StartEffect(Connector),
                    followUp: () => option.StopEffect(Connector),
                    retryDelay: TimeSpan.FromMilliseconds(500),
                    retryOnFail: true,
                    mutex: kh2FMCrowdControl.OptionConflicts[option.Id],
                    holdMutex: TimeSpan.FromMilliseconds(500)
                );
                break;

        }
    }

    protected override bool StopEffect(EffectRequest request)
    {
        Log.Message($"[StopEffect] request.EffectId = {request.EffectID}");

        if (GetOptionForRequest(request, out Option? option)) return option.StopEffect(Connector);
        return base.StopEffect(request);
    }

    public override bool StopAllEffects()
    {
        bool success = base.StopAllEffects();
        try
        {
            foreach (Option o in kh2FMCrowdControl.Options.Values)
            {
                success &= o.StopEffect(Connector);
            }
        }
        catch
        {
            success = false;
        }
        return success;
    }
}

public enum EffectFunction
{
    TryEffect,
    StartTimed,
    RepeatAction
}

public abstract class Option
{
    public static string ToId(Category category, SubCategory subCategory, string objectName)
    {
        string modifiedCategory = category.ToString().Replace(" ", "_").Replace("'", "").ToLower();
        string modifiedSubCategory = subCategory.ToString().Replace(" ", "_").Replace("'", "").ToLower();
        string modifiedObjectName = objectName.Replace(" ", "_").Replace("'", "").ToLower();

        return $"{modifiedCategory}_{modifiedSubCategory}_{modifiedObjectName}";
    }

    private bool isActive = false;
    protected Category category = Category.None;
    protected SubCategory subCategory = SubCategory.None;

    public EffectFunction effectFunction;

    public string Name { get; set; }
    public string Id
    {
        get
        {
            return ToId(category, subCategory, Name);
        }
    }

    public string Description { get; set; }
    public int Cost { get; set; }

    public int DurationSeconds { get; set; }
    public int RefreshInterval { get; set; } // In milliseconds

    public Option(string name, string description, Category category, SubCategory subCategory, EffectFunction effectFunction, int cost = 50, int durationSeconds = 0, int refreshInterval = 500)
    {
        Name = name;
        this.category = category;
        this.subCategory = subCategory;
        this.effectFunction = effectFunction;
        Cost = cost;
        Description = description;
        DurationSeconds = durationSeconds;
        RefreshInterval = refreshInterval;
    }

    public EffectGrouping? GetEffectCategory()
    {
        return category == Category.None ? null : new EffectGrouping(category.ToString());
    }

    public EffectGrouping? GetEffectGroup()
    {
        return subCategory == SubCategory.None ? null : new EffectGrouping(subCategory.ToString());
    }

    public abstract bool StartEffect(IPS2Connector connector);
    public virtual bool DoEffect(IPS2Connector connector) => true;
    public virtual bool StopEffect(IPS2Connector connector) => true;
}

public class KH2FMCrowdControl
{
    public Dictionary<string, Option> Options;
    public Dictionary<string, string[]> OptionConflicts;

    public KH2FMCrowdControl()
    {
        OneShotSora oneShotSora = new OneShotSora();
        HealSora healSora = new HealSora();
        Invulnerability invulnerability = new Invulnerability();
        MoneybagsSora moneybagsSora = new MoneybagsSora();
        RobSora robSora = new RobSora();
        GrowthSpurt growthSpurt = new GrowthSpurt();
        SlowgaSora slowgaSora = new SlowgaSora();
        TinyWeapon tinyWeapon = new TinyWeapon();
        GiantWeapon giantWeapon = new GiantWeapon();
        Struggling struggling = new Struggling();
        WhoAreThey whoAreThey = new WhoAreThey();
        HostileParty hostileParty = new HostileParty();
        ShuffleShortcuts shuffleShortcuts = new ShuffleShortcuts();
        HastegaSora hastegaSora = new HastegaSora();
        IAmDarkness iAmDarkness = new IAmDarkness();
        BackseatDriver backseatDriver = new BackseatDriver();
        ExpertMagician expertMagician = new ExpertMagician();
        AmnesiacMagician amnesiacMagician = new AmnesiacMagician();
        Itemaholic itemaholic = new Itemaholic();
        SpringCleaning springCleaning = new SpringCleaning();
        SummonChauffeur summonChauffeur = new SummonChauffeur();
        SummonTrainer summonTrainer = new SummonTrainer();
        HeroSora heroSora = new HeroSora();
        ZeroSora zeroSora = new ZeroSora();
        ProCodes proCodes = new ProCodes();
        EZCodes ezCodes = new EZCodes();

        Options = new List<Option>
            {
                oneShotSora,
                healSora,
                invulnerability,
                moneybagsSora,
                robSora,
                growthSpurt,
                tinyWeapon,
                giantWeapon,
                struggling,
                shuffleShortcuts,
                hastegaSora,
                iAmDarkness,
                backseatDriver,
                expertMagician,
                amnesiacMagician,
                itemaholic,
                springCleaning,
                summonChauffeur,
                summonTrainer,
                heroSora,
                zeroSora
            }.ToDictionary(x => x.Id, x => x);

        // Used to populate mutexes
        OptionConflicts = new Dictionary<string, string[]>
            {
                { oneShotSora.Id, [oneShotSora.Id, healSora.Id, invulnerability.Id] },
                { healSora.Id, [healSora.Id, oneShotSora.Id, invulnerability.Id] },
                { tinyWeapon.Id, [tinyWeapon.Id, giantWeapon.Id] },
                { giantWeapon.Id, [tinyWeapon.Id, giantWeapon.Id] },
                { iAmDarkness.Id, [iAmDarkness.Id, backseatDriver.Id, heroSora.Id, zeroSora.Id] },
                { backseatDriver.Id, [backseatDriver.Id, iAmDarkness.Id, heroSora.Id, zeroSora.Id] },
                { expertMagician.Id, [expertMagician.Id, amnesiacMagician.Id, heroSora.Id, zeroSora.Id] },
                { amnesiacMagician.Id, [amnesiacMagician.Id, expertMagician.Id, heroSora.Id, zeroSora.Id] },
                { itemaholic.Id, [itemaholic.Id, springCleaning.Id, heroSora.Id, zeroSora.Id] },
                { springCleaning.Id, [springCleaning.Id, itemaholic.Id, heroSora.Id, zeroSora.Id] },
                { summonChauffeur.Id, [summonChauffeur.Id, summonTrainer.Id, heroSora.Id, zeroSora.Id] },
                { summonTrainer.Id, [summonTrainer.Id, summonChauffeur.Id, heroSora.Id, zeroSora.Id] },
                { heroSora.Id, [heroSora.Id, zeroSora.Id, itemaholic.Id, springCleaning.Id, summonChauffeur.Id, summonTrainer.Id, expertMagician.Id, amnesiacMagician.Id
                    ]
                },
            };
    }

    public bool CheckTPose(IPS2Connector connector)
    {
        if (connector == null) return false;

        connector.Read64LE(0x20341708, out ulong animationStateOffset);
        connector.Read32LE(animationStateOffset + 0x2000014C, out uint animationState);

        return animationState == 0;
    }

    public void FixTPose(IPS2Connector connector)
    {
        if (connector == null) return;

        connector.Read64LE(0x20341708, out ulong animationStateOffset);

        connector.Read8(0x2033CC38, out byte cameraLock);

        if (cameraLock == 0)
        {
            connector.Read16LE(animationStateOffset + 0x2000000C, out ushort animationState);

            // 0x8001 is Idle state
            if (animationState != 0x8001)
            {
                connector.Write16LE(animationStateOffset + 0x2000000C, 0x40);
            }
        }
    }

    #region Option Implementations
    private class OneShotSora : Option
    {
        public OneShotSora() : base("1 Shot Sora", "Set Sora's Max and Current HP to 1.",
            Category.Sora, SubCategory.Stats,
            EffectFunction.StartTimed, durationSeconds: 60)
        { }

        private uint currentHP;
        private uint maxHP;

        public override bool StopEffect(IPS2Connector connector)
        {
            return connector.Write32LE(ConstantAddresses.HP, currentHP)
                && connector.Write32LE(ConstantAddresses.MaxHP, maxHP);
        }

        public override bool StartEffect(IPS2Connector connector)
        {
            bool success = true;
            // Capture the original data so it can be reset
            success &= connector.Read32LE(ConstantAddresses.HP, out currentHP);
            success &= connector.Read32LE(ConstantAddresses.MaxHP, out maxHP);

            success &= connector.Write32LE(ConstantAddresses.HP, 1);
            success &= connector.Write32LE(ConstantAddresses.MaxHP, 1);

            return success;
        }
    }

    private class HealSora : Option
    {
        public HealSora() : base("Heal Sora", "Heal Sora to Max HP.", Category.Sora, SubCategory.Stats, EffectFunction.TryEffect) { }

        public override bool StartEffect(IPS2Connector connector)
        {
            return connector.Read32LE(ConstantAddresses.MaxHP, out uint maxHP)
                && connector.Write32LE(ConstantAddresses.HP, maxHP);
        }
    }

    private class Invulnerability : Option
    {
        private uint currentHP;
        private uint maxHP;

        public Invulnerability() : base("Invulnerability", "Set Sora to be invulnerable.",
            Category.Sora, SubCategory.Stats,
            EffectFunction.RepeatAction, durationSeconds: 60)
        { }

        public override bool StartEffect(IPS2Connector connector)
        {
            return connector.Read32LE(ConstantAddresses.HP, out currentHP)
                && connector.Read32LE(ConstantAddresses.MaxHP, out maxHP);
        }

        public override bool DoEffect(IPS2Connector connector)
        {
            return connector.Write32LE(ConstantAddresses.HP, 999)
                && connector.Write32LE(ConstantAddresses.MaxHP, 999);
        }

        public override bool StopEffect(IPS2Connector connector)
        {
            return connector.Write32LE(ConstantAddresses.HP, currentHP)
                && connector.Write32LE(ConstantAddresses.MaxHP, maxHP);
        }
    }

    private class MoneybagsSora : Option
    {
        public MoneybagsSora() : base("Munnybags Sora", "Give Sora 9999 Munny.", Category.Sora, SubCategory.Munny, EffectFunction.TryEffect) { }
        public override bool StartEffect(IPS2Connector connector)
        {
            bool success = connector.Read32LE(ConstantAddresses.Munny, out uint munny);

            int newAmount = (int)munny + 9999;

            return success && connector.Write32LE(ConstantAddresses.Munny, (uint)newAmount);
        }
    }

    private class RobSora : Option
    {
        public RobSora() : base("Rob Sora", "Take all of Sora's Munny.", Category.Sora, SubCategory.Stats, EffectFunction.TryEffect) { }

        public override bool StartEffect(IPS2Connector connector)
        {
            return connector.Write32LE(ConstantAddresses.Munny, 0);
        }
    }

    private class WhoAmI : Option
    {
        public WhoAmI() : base("Who Am I?", "Set Sora to a different character.",
            Category.ModelSwap, SubCategory.None,
            EffectFunction.StartTimed, durationSeconds: 60)
        { }

        private readonly List<int> values =
        [
            ConstantValues.KH1Sora,
            ConstantValues.CardSora,
            ConstantValues.DieSora,
            ConstantValues.LionSora,
            ConstantValues.ChristmasSora,
            ConstantValues.SpaceParanoidsSora,
            ConstantValues.TimelessRiverSora,
            ConstantValues.Roxas,
            ConstantValues.DualwieldRoxas,
            ConstantValues.MickeyRobed,
            ConstantValues.Mickey,
            ConstantValues.Minnie,
            ConstantValues.Donald,
            ConstantValues.Goofy,
            ConstantValues.BirdDonald,
            ConstantValues.TortoiseGoofy,
            // ConstantValues.HalloweenDonald, ConstantValues.HalloweenGoofy, - Causes crash?
            // ConstantValues.ChristmasDonald, ConstantValues.ChristmasGoofy,
            ConstantValues.SpaceParanoidsDonald,
            ConstantValues.SpaceParanoidsGoofy,
            ConstantValues.TimelessRiverDonald,
            ConstantValues.TimelessRiverGoofy,
            ConstantValues.Beast,
            ConstantValues.Mulan,
            ConstantValues.Ping,
            ConstantValues.Hercules,
            ConstantValues.Auron,
            ConstantValues.Aladdin,
            ConstantValues.JackSparrow,
            ConstantValues.HalloweenJack,
            ConstantValues.ChristmasJack,
            ConstantValues.Simba,
            ConstantValues.Tron,
            ConstantValues.ValorFormSora,
            ConstantValues.WisdomFormSora,
            ConstantValues.LimitFormSora,
            ConstantValues.MasterFormSora,
            ConstantValues.FinalFormSora,
            ConstantValues.AntiFormSora
        ];

        public override bool StartEffect(IPS2Connector connector)
        {
            bool success = true;
            ushort randomModel = (ushort)values[new Random().Next(values.Count)];
            Log.Message($"Random Model value: {randomModel}");

            success &= connector.Read16LE(ConstantAddresses.Sora, out ushort currentSora);
            Log.Message($"Sora's current model value: {currentSora}");


            success &= connector.Write16LE(ConstantAddresses.Sora, randomModel);

            success &= connector.Read16LE(ConstantAddresses.Sora, out ushort newSora);
            Log.Message($"Sora's current model value: {newSora}");


            success &= connector.Write16LE(ConstantAddresses.LionSora, randomModel);
            success &= connector.Write16LE(ConstantAddresses.ChristmasSora, randomModel);
            success &= connector.Write16LE(ConstantAddresses.SpaceParanoidsSora, randomModel);
            success &= connector.Write16LE(ConstantAddresses.TimelessRiverSora, randomModel);

            // TODO Figure out how to swap to Sora
            //int randomIndex = new Random().Next(values.Count);

            //// Set Valor Form to Random Character so we can activate form
            //connector.Write16LE(ConstantAddresses.ValorFormSora, (ushort)values[randomIndex]);

            //// NEEDS ADDITIONAL WORK AND TESTING

            //connector.Write16LE(ConstantAddresses.ReactionPopup, (ushort)ConstantValues.None);

            //connector.Write16LE(ConstantAddresses.ReactionOption, (ushort)ConstantValues.ReactionValor);

            //connector.Write16LE(ConstantAddresses.ReactionEnable, (ushort)ConstantValues.None);

            //Timer timer = new Timer(250);
            //timer.Elapsed += (obj, args) =>
            //{
            //    connector.Read16LE(ConstantAddresses.ReactionEnable, out ushort value);

            //    if (value == 5) timer.Stop();

            //    connector.Write8(ConstantAddresses.ButtonPress, (byte)ConstantValues.Triangle);
            //};
            //timer.Start();

            return success;
        }

        public override bool StopEffect(IPS2Connector connector)
        {
            bool success = true;
            success &= connector.Write16LE(ConstantAddresses.Sora, (ushort)ConstantValues.Sora);
            success &= connector.Write16LE(ConstantAddresses.LionSora, (ushort)ConstantValues.LionSora);
            success &= connector.Write16LE(ConstantAddresses.ChristmasSora, (ushort)ConstantValues.ChristmasSora);
            success &= connector.Write16LE(ConstantAddresses.SpaceParanoidsSora, (ushort)ConstantValues.SpaceParanoidsSora);
            success &= connector.Write16LE(ConstantAddresses.TimelessRiverSora, (ushort)ConstantValues.TimelessRiverSora);

            //connector.Write16LE(ConstantAddresses.ValorFormSora, ConstantValues.ValorFormSora);
            return success;
        }
    }

    private class BackseatDriver : Option
    {
        public BackseatDriver() : base("Backseat Driver", "Trigger one of Sora's different forms.",
            Category.Sora, SubCategory.Drive, EffectFunction.TryEffect)
        { }

        private ushort currentKeyblade;
        private ushort currentValorKeyblade;
        private ushort currentMasterKeyblade;
        private ushort currentFinalKeyblade;

        private readonly List<uint> values =
        [
            ConstantValues.ReactionValor,
            ConstantValues.ReactionWisdom,
            ConstantValues.ReactionLimit,
            ConstantValues.ReactionMaster,
            ConstantValues.ReactionFinal //ConstantValues.ReactionAnti
        ];

        public override bool StartEffect(IPS2Connector connector)
        {
            bool success = true;
            // Get us out of a Drive first if we are in one
            success &= connector.WriteFloat(ConstantAddresses.DriveTime, ConstantValues.None);

            Thread.Sleep(200);

            int randomIndex = new Random().Next(values.Count);

            success &= connector.Read16LE(ConstantAddresses.SoraWeaponSlot, out currentKeyblade);

            // Set the current keyblade in the slot for the drive form
            if (randomIndex == 0) // Valor
            {
                success &= connector.Read16LE(ConstantAddresses.SoraValorWeaponSlot, out currentValorKeyblade);
                success &= connector.Write16LE(ConstantAddresses.SoraValorWeaponSlot, currentKeyblade);
            }
            else if (randomIndex == 3) // Master
            {
                success &= connector.Read16LE(ConstantAddresses.SoraMasterWeaponSlot, out currentMasterKeyblade);
                success &= connector.Write16LE(ConstantAddresses.SoraMasterWeaponSlot, currentKeyblade);
            }
            else if (randomIndex == 4) // Final
            {
                success &= connector.Read16LE(ConstantAddresses.SoraValorWeaponSlot, out currentFinalKeyblade);
                success &= connector.Write16LE(ConstantAddresses.SoraFinalWeaponSlot, currentKeyblade);
            }

            success &= connector.Write16LE(ConstantAddresses.ReactionPopup, (ushort)ConstantValues.None);
            success &= connector.Write16LE(ConstantAddresses.ReactionOption, (ushort)values[randomIndex]);
            success &= connector.Write16LE(ConstantAddresses.ReactionEnable, (ushort)ConstantValues.None);

            // Might be able to move this to RepeatAction?
            Timer timer = new(100);
            timer.Elapsed += (_, _) =>
            {
                connector.Read16LE(ConstantAddresses.ReactionEnable, out ushort value);

                if (value == 5) timer.Stop();

                connector.Write8(ConstantAddresses.ButtonPress, (byte)ConstantValues.Triangle);

                // Set the current keyblade in the slot back to the original for the drive form
                if (randomIndex == 0) // Valor
                {
                    success &= connector.Write16LE(ConstantAddresses.SoraValorWeaponSlot, currentValorKeyblade);
                }
                else if (randomIndex == 3) // Master
                {
                    success &= connector.Write16LE(ConstantAddresses.SoraMasterWeaponSlot, currentMasterKeyblade);
                }
                else if (randomIndex == 4) // Final
                {
                    success &= connector.Write16LE(ConstantAddresses.SoraFinalWeaponSlot, currentFinalKeyblade);
                }
            };
            timer.Start();

            return success;
        }
    }

    private class WhoAreThey : Option
    {
        public WhoAreThey() : base("Who Are They?", "Set Donald and Goofy to different characters.",
            Category.ModelSwap, SubCategory.None,
            EffectFunction.StartTimed, durationSeconds: 60)
        { }

        private readonly List<int> values =
        [
            ConstantValues.Minnie,
            ConstantValues.Donald,
            ConstantValues.Goofy,
            ConstantValues.BirdDonald,
            ConstantValues.TortoiseGoofy,
            //ConstantValues.HalloweenDonald, ConstantValues.HalloweenGoofy, - Causes crash?
            //ConstantValues.ChristmasDonald, ConstantValues.ChristmasGoofy, 
            ConstantValues.SpaceParanoidsDonald,
            ConstantValues.SpaceParanoidsGoofy,
            ConstantValues.TimelessRiverDonald,
            ConstantValues.TimelessRiverGoofy,
            ConstantValues.Beast,
            ConstantValues.Mulan,
            ConstantValues.Ping,
            ConstantValues.Hercules,
            ConstantValues.Auron,
            ConstantValues.Aladdin,
            ConstantValues.JackSparrow,
            ConstantValues.HalloweenJack,
            ConstantValues.ChristmasJack,
            ConstantValues.Simba,
            ConstantValues.Tron,
            ConstantValues.Riku,
            ConstantValues.AxelFriend,
            ConstantValues.LeonFriend,
            ConstantValues.YuffieFriend,
            ConstantValues.TifaFriend,
            ConstantValues.CloudFriend
        ];

        public override bool StartEffect(IPS2Connector connector)
        {
            bool success = true;
            ushort donald = (ushort)values[new Random().Next(values.Count)];
            ushort goofy = (ushort)values[new Random().Next(values.Count)];

            success &= connector.Write16LE(ConstantAddresses.Donald, donald);
            success &= connector.Write16LE(ConstantAddresses.BirdDonald, donald);
            success &= connector.Write16LE(ConstantAddresses.ChristmasDonald, donald);
            success &= connector.Write16LE(ConstantAddresses.SpaceParanoidsDonald, donald);
            success &= connector.Write16LE(ConstantAddresses.TimelessRiverDonald, donald);


            success &= connector.Write16LE(ConstantAddresses.Goofy, goofy);
            success &= connector.Write16LE(ConstantAddresses.TortoiseGoofy, goofy);
            success &= connector.Write16LE(ConstantAddresses.ChristmasGoofy, goofy);
            success &= connector.Write16LE(ConstantAddresses.SpaceParanoidsGoofy, goofy);
            success &= connector.Write16LE(ConstantAddresses.TimelessRiverGoofy, goofy);

            return success;
        }

        public override bool StopEffect(IPS2Connector connector)
        {
            bool success = true;
            success &= connector.Write16LE(ConstantAddresses.Donald, ConstantValues.Donald);
            success &= connector.Write16LE(ConstantAddresses.BirdDonald, ConstantValues.BirdDonald);
            success &= connector.Write16LE(ConstantAddresses.ChristmasDonald, ConstantValues.ChristmasDonald);
            success &= connector.Write16LE(ConstantAddresses.SpaceParanoidsDonald, ConstantValues.SpaceParanoidsDonald);
            success &= connector.Write16LE(ConstantAddresses.TimelessRiverDonald, ConstantValues.TimelessRiverDonald);


            success &= connector.Write16LE(ConstantAddresses.Goofy, ConstantValues.Goofy);
            success &= connector.Write16LE(ConstantAddresses.TortoiseGoofy, ConstantValues.TortoiseGoofy);
            success &= connector.Write16LE(ConstantAddresses.ChristmasGoofy, ConstantValues.ChristmasGoofy);
            success &= connector.Write16LE(ConstantAddresses.SpaceParanoidsGoofy, ConstantValues.SpaceParanoidsGoofy);
            success &= connector.Write16LE(ConstantAddresses.TimelessRiverGoofy, ConstantValues.TimelessRiverGoofy);

            return success;
        }
    }

    private class SlowgaSora : Option
    {
        public SlowgaSora() : base("Slowga Sora", "Set Sora's Speed to be super slow.",
            Category.Sora, SubCategory.None,
            EffectFunction.StartTimed, durationSeconds: 30)
        { }

        private uint speed;
        private uint speedAlt = 0;

        public override bool StartEffect(IPS2Connector connector)
        {
            return connector.Read32LE(ConstantAddresses.Speed, out speed)
                && connector.Write32LE(ConstantAddresses.Speed, ConstantValues.SlowDownx2);
        }

        public override bool StopEffect(IPS2Connector connector)
        {
            return connector.Write32LE(ConstantAddresses.Speed, speed);
        }
    }

    private class HastegaSora : Option
    {
        public HastegaSora() : base("Hastega Sora", "Set Sora's Speed to be super fast.",
            Category.Sora, SubCategory.Stats,
            EffectFunction.StartTimed, durationSeconds: 30)
        { }

        private uint speed;
        private uint speedAlt = 0;

        public override bool StartEffect(IPS2Connector connector)
        {
            return connector.Read32LE(ConstantAddresses.Speed, out speed)
                && connector.Write32LE(ConstantAddresses.Speed, ConstantValues.SpeedUpx2);
        }

        public override bool StopEffect(IPS2Connector connector)
        {
            return connector.Write32LE(ConstantAddresses.Speed, speed);
        }
    }

    // NEEDS WORK -- DOESNT SEEM TO DO ANYTHING
    private class SpaceJump : Option
    {
        public SpaceJump() : base("Space Jump", "Give Sora the ability to Space Jump.",
            Category.Sora, SubCategory.None,
            EffectFunction.StartTimed, durationSeconds: 60)
        { }

        private uint jump;

        public override bool StartEffect(IPS2Connector connector)
        {
            // Store original jump amount for the reset
            return connector.Read32LE(ConstantAddresses.JumpAmount, out jump)
                && connector.Write32LE(ConstantAddresses.JumpAmount, 0);
        }

        public override bool StopEffect(IPS2Connector connector)
        {
            return connector.Write32LE(ConstantAddresses.JumpAmount, jump);
        }
    }

    private class TinyWeapon : Option
    {
        public TinyWeapon() : base("Tiny Weapon", "Set Sora's Weapon size to be tiny.",
            Category.Sora, SubCategory.None,
            EffectFunction.StartTimed, durationSeconds: 60)
        { }

        private uint currentWeaponSize;
        private uint currentWeaponSizeAlt = 0;

        public override bool StartEffect(IPS2Connector connector)
        {
            bool success = true;
            success &= connector.Read32LE(ConstantAddresses.WeaponSize, out currentWeaponSize);
            // The WeaponSizeAlt address seems to be some sort of transform value for the player character.
            // Modifying it moves the player further away from the camera.
            //connector.Read32LE(ConstantAddresses.WeaponSizeAlt, out currentWeaponSizeAlt);

            success &= connector.Write32LE(ConstantAddresses.WeaponSize, ConstantValues.TinyWeapon);
            //connector.Write32LE(ConstantAddresses.WeaponSizeAlt, ConstantValues.TinyWeapon);

            return success;
        }

        public override bool StopEffect(IPS2Connector connector)
        {
            return connector.Write32LE(ConstantAddresses.WeaponSize, currentWeaponSize);
            //connector.Write32LE(ConstantAddresses.WeaponSizeAlt, currentWeaponSizeAlt);
        }
    }

    private class GiantWeapon : Option
    {
        public GiantWeapon() : base("Giant Weapon", "Set Sora's Weapon size to be huge.",
            Category.Sora, SubCategory.None,
            EffectFunction.StartTimed, durationSeconds: 60)
        { }

        private uint currentWeaponSize;
        private uint currentWeaponSizeAlt = 0;

        public override bool StartEffect(IPS2Connector connector)
        {
            bool success = true;
            success &= connector.Read32LE(ConstantAddresses.WeaponSize, out currentWeaponSize);
            //connector.Read32LE(ConstantAddresses.WeaponSizeAlt, out currentWeaponSizeAlt);

            success &= connector.Write32LE(ConstantAddresses.WeaponSize, ConstantValues.BigWeapon);
            //connector.Write32LE(ConstantAddresses.WeaponSizeAlt, ConstantValues.TinyWeapon);

            return success;
        }

        public override bool StopEffect(IPS2Connector connector)
        {
            return connector.Write32LE(ConstantAddresses.WeaponSize, currentWeaponSize);
            //connector.Write32LE(ConstantAddresses.WeaponSizeAlt, currentWeaponSizeAlt);
        }
    }

    private class Struggling : Option
    {
        public Struggling() : base("Struggling", "Change Sora's weapon to the Struggle Bat.",
            Category.Sora, SubCategory.Weapon,
            EffectFunction.StartTimed, durationSeconds: 60)
        { }

        private ushort currentKeyblade;

        public override bool StartEffect(IPS2Connector connector)
        {
            return connector.Read16LE(ConstantAddresses.SoraWeaponSlot, out currentKeyblade) &&
                connector.Write16LE(ConstantAddresses.SoraWeaponSlot, ConstantValues.StruggleBat);
        }

        public override bool StopEffect(IPS2Connector connector)
        {
            return connector.Write16LE(ConstantAddresses.SoraWeaponSlot, currentKeyblade);
        }
    }

    private class HostileParty : Option
    {
        public HostileParty() : base("Hostile Party", "Set Donald and Goofy to random enemies.",
            Category.ModelSwap, SubCategory.Enemy,
            EffectFunction.StartTimed, durationSeconds: 60)
        { }

        private readonly List<int> values =
        [
            ConstantValues.LeonEnemy,
            ConstantValues.YuffieEnemy,
            ConstantValues.TifaEnemy,
            ConstantValues.CloudEnemy,
            ConstantValues.Xemnas,
            ConstantValues.Xigbar,
            ConstantValues.Xaldin,
            ConstantValues.Vexen,
            ConstantValues.VexenAntiSora,
            ConstantValues.Lexaeus,
            ConstantValues.Zexion,
            ConstantValues.Saix,
            ConstantValues.AxelEnemy,
            ConstantValues.Demyx,
            ConstantValues.DemyxWaterClone,
            ConstantValues.Luxord,
            ConstantValues.Marluxia,
            ConstantValues.Larxene,
            ConstantValues.RoxasEnemy,
            ConstantValues.RoxasShadow,
            ConstantValues.Sephiroth,
            ConstantValues.LingeringWill
        ];

        public override bool StartEffect(IPS2Connector connector)
        {
            bool success = true;

            ushort donald = (ushort)values[new Random().Next(values.Count)];
            ushort goofy = (ushort)values[new Random().Next(values.Count)];

            success &= connector.Write16LE(ConstantAddresses.Donald, donald);
            connector.Write16LE(ConstantAddresses.BirdDonald, donald);
            connector.Write16LE(ConstantAddresses.ChristmasDonald, donald);
            success &= connector.Write16LE(ConstantAddresses.SpaceParanoidsDonald, donald);
            success &= connector.Write16LE(ConstantAddresses.TimelessRiverDonald, donald);


            success &= connector.Write16LE(ConstantAddresses.Goofy, goofy);
            success &= connector.Write16LE(ConstantAddresses.TortoiseGoofy, goofy);
            success &= connector.Write16LE(ConstantAddresses.ChristmasGoofy, goofy);
            success &= connector.Write16LE(ConstantAddresses.SpaceParanoidsGoofy, goofy);
            success &= connector.Write16LE(ConstantAddresses.TimelessRiverGoofy, goofy);

            return success;
        }

        public override bool StopEffect(IPS2Connector connector)
        {
            bool success = true;
            success &= connector.Write16LE(ConstantAddresses.Donald, ConstantValues.Donald);
            success &= connector.Write16LE(ConstantAddresses.BirdDonald, ConstantValues.BirdDonald);
            success &= connector.Write16LE(ConstantAddresses.ChristmasDonald, ConstantValues.ChristmasDonald);
            success &= connector.Write16LE(ConstantAddresses.SpaceParanoidsDonald, ConstantValues.SpaceParanoidsDonald);
            success &= connector.Write16LE(ConstantAddresses.TimelessRiverDonald, ConstantValues.TimelessRiverDonald);


            success &= connector.Write16LE(ConstantAddresses.Goofy, ConstantValues.Goofy);
            success &= connector.Write16LE(ConstantAddresses.TortoiseGoofy, ConstantValues.TortoiseGoofy);
            success &= connector.Write16LE(ConstantAddresses.ChristmasGoofy, ConstantValues.ChristmasGoofy);
            success &= connector.Write16LE(ConstantAddresses.SpaceParanoidsGoofy, ConstantValues.SpaceParanoidsGoofy);
            success &= connector.Write16LE(ConstantAddresses.TimelessRiverGoofy, ConstantValues.TimelessRiverGoofy);

            return success;
        }
    }

    private class IAmDarkness : Option
    {
        public IAmDarkness() : base("I Am Darkness", "Change Sora to Antiform Sora.",
            Category.ModelSwap, SubCategory.None, EffectFunction.TryEffect)
        { }

        public override bool StartEffect(IPS2Connector connector)
        {
            bool success = true;

            // Get us out of a Drive first if we are in one
            success &= connector.WriteFloat(ConstantAddresses.DriveTime, ConstantValues.None);
            Thread.Sleep(200);

            success &= connector.Write16LE(ConstantAddresses.ReactionPopup, (ushort)ConstantValues.None);

            success &= connector.Write16LE(ConstantAddresses.ReactionOption, (ushort)ConstantValues.ReactionAnti);

            success &= connector.Write16LE(ConstantAddresses.ReactionEnable, (ushort)ConstantValues.None);

            Timer timer = new(100);
            timer.Elapsed += (_, _) =>
            {
                connector.Read16LE(ConstantAddresses.ReactionEnable, out ushort value);

                if (value == 5) timer.Stop();

                connector.Write8(ConstantAddresses.ButtonPress, (byte)ConstantValues.Triangle);
            };
            timer.Start();

            return success;

            //connector.Write16LE(ConstantAddresses.Sora, (ushort)ConstantValues.AntiFormSora);
            ////connector.Write16LE(ConstantAddresses.HalloweenSora, (ushort)ConstantValues.AntiFormSora);
            //connector.Write16LE(ConstantAddresses.ChristmasSora, (ushort)ConstantValues.AntiFormSora);
            //connector.Write16LE(ConstantAddresses.LionSora, (ushort)ConstantValues.AntiFormSora);
            //connector.Write16LE(ConstantAddresses.SpaceParanoidsSora, (ushort)ConstantValues.AntiFormSora);
            //connector.Write16LE(ConstantAddresses.TimelessRiverSora, (ushort)ConstantValues.AntiFormSora);
        }

        public override bool StopEffect(IPS2Connector connector)
        {
            //connector.Write16LE(ConstantAddresses.Sora, (ushort)ConstantValues.Sora);
            ////connector.Write16LE(ConstantAddresses.HalloweenSora, (ushort)ConstantValues.HalloweenSora);
            //connector.Write16LE(ConstantAddresses.ChristmasSora, (ushort)ConstantValues.ChristmasSora);
            //connector.Write16LE(ConstantAddresses.LionSora, (ushort)ConstantValues.LionSora);
            //connector.Write16LE(ConstantAddresses.SpaceParanoidsSora, (ushort)ConstantValues.SpaceParanoidsSora);
            //connector.Write16LE(ConstantAddresses.TimelessRiverSora, (ushort)ConstantValues.TimelessRiverSora);
            return true;
        }
    }

    // NEEDS IMPLEMENTATION
    private class KillSora : Option
    {
        public KillSora() : base("Kill Sora", "Instantly Kill Sora.",
            Category.Sora, SubCategory.Stats, EffectFunction.TryEffect)
        { }

        public override bool StartEffect(IPS2Connector connector)
        {
            throw new NotImplementedException();
        }
    }

    // NEEDS IMPLEMENTATION
    private class RandomizeControls : Option
    {
        public RandomizeControls() : base("Randomize Controls", "Randomize the controls to the game.",
            Category.None, SubCategory.None, EffectFunction.StartTimed)
        { }

        private Dictionary<uint, uint> controls = new()
        {
            //{ ConstantAddresses.Control, 0 },
        };

        public override bool StartEffect(IPS2Connector connector)
        {
            throw new NotImplementedException();
        }

        public override bool StopEffect(IPS2Connector connector)
        {
            throw new NotImplementedException();
        }
    }

    private class ShuffleShortcuts : Option
    {
        public ShuffleShortcuts() : base("Shuffle Shortcuts", "Set Sora's Shortcuts to random commands.",
            Category.Sora, SubCategory.None,
            EffectFunction.StartTimed, durationSeconds: 60)
        { }

        private readonly Random random = new();
        private readonly Dictionary<int, Tuple<int, int>> values = new()
            {
                { ConstantAddresses.Potion, new Tuple<int, int>(ConstantValues.PotionQuickSlotValue, ConstantValues.Potion) }, { ConstantAddresses.HiPotion, new Tuple<int, int>(ConstantValues.HiPotionQuickSlotValue, ConstantValues.HiPotion) },
                { ConstantAddresses.MegaPotion, new Tuple<int, int>(ConstantValues.MegaPotionQuickSlotValue, ConstantValues.MegaPotion) }, { ConstantAddresses.Ether, new Tuple<int, int>(ConstantValues.EtherQuickSlotValue, ConstantValues.Ether) },
                { ConstantAddresses.MegaEther, new Tuple<int, int>(ConstantValues.MegaEtherQuickSlotValue, ConstantValues.MegaEther) }, { ConstantAddresses.Elixir, new Tuple<int, int>(ConstantValues.ElixirQuickSlotValue, ConstantValues.Elixir) },
                { ConstantAddresses.Megalixir, new Tuple<int, int>(ConstantValues.MegalixirQuickSlotValue, ConstantValues.Megalixir) }, { ConstantAddresses.Fire, new Tuple<int, int>(ConstantValues.FireQuickSlotValue, ConstantValues.Fire) },
                { ConstantAddresses.Blizzard, new Tuple<int, int>(ConstantValues.BlizzardQuickSlotValue, ConstantValues.Blizzard) }, { ConstantAddresses.Thunder, new Tuple<int, int>(ConstantValues.ThunderQuickSlotValue, ConstantValues.Thunder) },
                { ConstantAddresses.Cure, new Tuple<int, int>(ConstantValues.CureQuickSlotValue, ConstantValues.Cure) }, { ConstantAddresses.Reflect, new Tuple<int, int>(ConstantValues.ReflectQuickSlotValue, ConstantValues.Reflect) },
                { ConstantAddresses.Magnet, new Tuple<int, int>(ConstantValues.MagnetQuickSlotValue, ConstantValues.Magnet) }
            };

        private ushort shortcut1;
        private ushort shortcut2;
        private ushort shortcut3;
        private ushort shortcut4;

        private ulong shortcut1_set;
        private ulong shortcut2_set;
        private ulong shortcut3_set;
        private ulong shortcut4_set;

        private (int, bool) CheckQuickSlot(IPS2Connector connector, int key, Tuple<int, int> value, int shortcutNumber)
        {
            bool success = true;
            if (key != ConstantAddresses.Fire && key != ConstantAddresses.Blizzard && key != ConstantAddresses.Thunder &&
                key != ConstantAddresses.Cure && key != ConstantAddresses.Reflect && key != ConstantAddresses.Magnet)
            {
                success &= connector.Read16LE((ulong)key, out ushort itemValue);

                success &= connector.Write16LE((ulong)key, (ushort)(itemValue + 1));

                switch (shortcutNumber)
                {
                    case 1:
                        shortcut1_set = (ulong)key;
                        success &= connector.Write16LE(ConstantAddresses.SoraItemSlot1, (ushort)(value.Item2));
                        break;
                    case 2:
                        shortcut2_set = (ulong)key;
                        success &= connector.Write16LE(ConstantAddresses.SoraItemSlot2, (ushort)(value.Item2));
                        break;
                    case 3:
                        shortcut3_set = (ulong)key;
                        success &= connector.Write16LE(ConstantAddresses.SoraItemSlot3, (ushort)(value.Item2));
                        break;
                    case 4:
                        shortcut4_set = (ulong)key;
                        success &= connector.Write16LE(ConstantAddresses.SoraItemSlot4, (ushort)(value.Item2));
                        break;
                }

                return (value.Item1, success);
            }

            success &= connector.Read8((ulong)key, out byte byteValue);

            if (byteValue == 0)
            {
                success &= connector.Write8((ulong)key, (byte)value.Item2);

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

            if (key == ConstantAddresses.Fire)
                return (ConstantValues.FireQuickSlotValue, success);
            if (key == ConstantAddresses.Blizzard)
                return (ConstantValues.BlizzardQuickSlotValue, success);
            if (key == ConstantAddresses.Thunder)
                return (ConstantValues.ThunderQuickSlotValue, success);
            if (key == ConstantAddresses.Cure)
                return (ConstantValues.CureQuickSlotValue, success);
            if (key == ConstantAddresses.Reflect)
                return (ConstantValues.ReflectQuickSlotValue, success);
            if (key == ConstantAddresses.Magnet)
                return (ConstantValues.MagnetQuickSlotValue, success);

            return (ConstantValues.None, success);
        }

        public override bool StartEffect(IPS2Connector connector)
        {
            bool success = true;
            // Save the values before the shuffle
            success &= connector.Read16LE(ConstantAddresses.SoraQuickMenuSlot1, out shortcut1);
            success &= connector.Read16LE(ConstantAddresses.SoraQuickMenuSlot2, out shortcut2);
            success &= connector.Read16LE(ConstantAddresses.SoraQuickMenuSlot3, out shortcut3);
            success &= connector.Read16LE(ConstantAddresses.SoraQuickMenuSlot4, out shortcut4);

            int key1 = values.Keys.ToList()[random.Next(values.Keys.Count)];
            int key2 = values.Keys.ToList()[random.Next(values.Keys.Count)];
            int key3 = values.Keys.ToList()[random.Next(values.Keys.Count)];
            int key4 = values.Keys.ToList()[random.Next(values.Keys.Count)];

            (int value1, bool success1) = CheckQuickSlot(connector, key1, values[key1], 1);
            (int value2, bool success2) = CheckQuickSlot(connector, key2, values[key2], 2);
            (int value3, bool success3) = CheckQuickSlot(connector, key3, values[key3], 3);
            (int value4, bool success4) = CheckQuickSlot(connector, key4, values[key4], 4);

            success &= success1 && success2 && success3 && success4;

            success &= connector.Write16LE(ConstantAddresses.SoraQuickMenuSlot1, (ushort)value1);
            success &= connector.Write16LE(ConstantAddresses.SoraQuickMenuSlot2, (ushort)value2);
            success &= connector.Write16LE(ConstantAddresses.SoraQuickMenuSlot3, (ushort)value3);
            success &= connector.Write16LE(ConstantAddresses.SoraQuickMenuSlot4, (ushort)value4);

            return success;
        }

        public override bool StopEffect(IPS2Connector connector)
        {
            bool success = true;
            success &= connector.Write16LE(ConstantAddresses.SoraQuickMenuSlot1, shortcut1);
            success &= connector.Write16LE(ConstantAddresses.SoraQuickMenuSlot2, shortcut2);
            success &= connector.Write16LE(ConstantAddresses.SoraQuickMenuSlot3, shortcut3);
            success &= connector.Write16LE(ConstantAddresses.SoraQuickMenuSlot4, shortcut4);

            if (shortcut1_set != 0)
            {
                success &= connector.Write8(shortcut1_set, 0);
                shortcut1_set = 0;
            }
            if (shortcut2_set != 0)
            {
                success &= connector.Write8(shortcut2_set, 0);
                shortcut2_set = 0;
            }
            if (shortcut3_set != 0)
            {
                success &= connector.Write8(shortcut3_set, 0);
                shortcut3_set = 0;
            }
            if (shortcut4_set != 0)
            {
                success &= connector.Write8(shortcut4_set, 0);
                shortcut4_set = 0;
            }

            return success;
        }
    }

    private class GrowthSpurt : Option
    {
        public GrowthSpurt() : base("Growth Spurt", "Give Sora Max Growth abilities.", Category.Sora, SubCategory.Stats,
            EffectFunction.StartTimed, durationSeconds: 60)
        { }

        private uint startAddress;

        public override bool StartEffect(IPS2Connector connector)
        {
            bool success = true;
            Log.Message("GrowthSpurt");
            // Sora has 148 (maybe divided by 2?) slots available for abilities
            for (uint i = ConstantAddresses.SoraAbilityStart; i < (ConstantAddresses.SoraAbilityStart + 148); i += 2)
            {
                success &= connector.Read8(i, out byte value);

                if (value != 0) continue;

                startAddress = i;

                success &= connector.Write8(startAddress, (byte)ConstantValues.HighJumpMax);
                success &= connector.Write8(startAddress + 1, 0x80);

                success &= connector.Write8(startAddress + 2, (byte)ConstantValues.QuickRunMax);
                success &= connector.Write8(startAddress + 3, 0x80);

                success &= connector.Write8(startAddress + 4, (byte)ConstantValues.DodgeRollMax);
                success &= connector.Write8(startAddress + 5, 0x82);

                success &= connector.Write8(startAddress + 6, (byte)ConstantValues.AerialDodgeMax);
                success &= connector.Write8(startAddress + 7, 0x80);

                success &= connector.Write8(startAddress + 8, (byte)ConstantValues.GlideMax);
                success &= connector.Write8(startAddress + 9, 0x80);

                break;
            }

            return success;
        }

        public override bool StopEffect(IPS2Connector connector)
        {
            bool success = true;

            success &= connector.Write8(startAddress, 0);
            success &= connector.Write8(startAddress + 1, 0);

            success &= connector.Write8(startAddress + 2, 0);
            success &= connector.Write8(startAddress + 3, 0);

            success &= connector.Write8(startAddress + 4, 0);
            success &= connector.Write8(startAddress + 5, 0);

            success &= connector.Write8(startAddress + 6, 0);
            success &= connector.Write8(startAddress + 7, 0);

            success &= connector.Write8(startAddress + 8, 0);
            success &= connector.Write8(startAddress + 9, 0);

            return success;
        }
    }

    private class ExpertMagician : Option
    {
        public ExpertMagician() : base("Expert Magician", "Give Sora Max Magic and lower the cost of Magic.",
            Category.Sora, SubCategory.Stats,
            EffectFunction.StartTimed, durationSeconds: 30)
        { }

        private byte fire;
        private byte blizzard;
        private byte thunder;
        private byte cure;
        private byte reflect;
        private byte magnet;

        private byte fireCost;
        private byte blizzardCost;
        private byte thunderCost;
        private byte cureCost;
        private byte reflectCost;
        private byte magnetCost;

        public override bool StartEffect(IPS2Connector connector)
        {
            bool success = true;

            // Save Magic
            success &= connector.Read8((ulong)ConstantAddresses.Fire, out fire);
            success &= connector.Read8((ulong)ConstantAddresses.Blizzard, out blizzard);
            success &= connector.Read8((ulong)ConstantAddresses.Thunder, out thunder);
            success &= connector.Read8((ulong)ConstantAddresses.Cure, out cure);
            success &= connector.Read8((ulong)ConstantAddresses.Reflect, out reflect);
            success &= connector.Read8((ulong)ConstantAddresses.Magnet, out magnet);

            // Write Max Magic
            success &= connector.Write8((ulong)ConstantAddresses.Fire, (byte)ConstantValues.Firaga);
            success &= connector.Write8((ulong)ConstantAddresses.Blizzard, (byte)ConstantValues.Blizzaga);
            success &= connector.Write8((ulong)ConstantAddresses.Thunder, (byte)ConstantValues.Thundaga);
            success &= connector.Write8((ulong)ConstantAddresses.Cure, (byte)ConstantValues.Curaga);
            success &= connector.Write8((ulong)ConstantAddresses.Reflect, (byte)ConstantValues.Reflega);
            success &= connector.Write8((ulong)ConstantAddresses.Magnet, (byte)ConstantValues.Magnega);

            // Save Magic Costs
            success &= connector.Read8(ConstantAddresses.FiragaCost, out fireCost);
            success &= connector.Read8(ConstantAddresses.BlizzagaCost, out blizzardCost);
            success &= connector.Read8(ConstantAddresses.ThundagaCost, out thunderCost);
            success &= connector.Read8(ConstantAddresses.CuragaCost, out cureCost);
            success &= connector.Read8(ConstantAddresses.ReflegaCost, out reflectCost);
            success &= connector.Read8(ConstantAddresses.MagnegaCost, out magnetCost);

            // Write Magic Costs
            success &= connector.Write8(ConstantAddresses.FiragaCost, 0x1);
            success &= connector.Write8(ConstantAddresses.BlizzagaCost, 0x2);
            success &= connector.Write8(ConstantAddresses.ThundagaCost, 0x3);
            success &= connector.Write8(ConstantAddresses.CuragaCost, 0x10);
            success &= connector.Write8(ConstantAddresses.ReflegaCost, 0x6);
            success &= connector.Write8(ConstantAddresses.MagnegaCost, 0x5);

            return success;
        }

        public override bool StopEffect(IPS2Connector connector)
        {
            bool success = true;
            // Write back saved Magic
            success &= connector.Write8((ulong)ConstantAddresses.Fire, fire);
            success &= connector.Write8((ulong)ConstantAddresses.Blizzard, blizzard);
            success &= connector.Write8((ulong)ConstantAddresses.Thunder, thunder);
            success &= connector.Write8((ulong)ConstantAddresses.Cure, cure);
            success &= connector.Write8((ulong)ConstantAddresses.Reflect, reflect);
            success &= connector.Write8((ulong)ConstantAddresses.Magnet, magnet);

            // Write back saved Magic Costs
            success &= connector.Write8(ConstantAddresses.FiragaCost, fireCost);
            success &= connector.Write8(ConstantAddresses.BlizzagaCost, blizzardCost);
            success &= connector.Write8(ConstantAddresses.ThundagaCost, thunderCost);
            success &= connector.Write8(ConstantAddresses.CuragaCost, cureCost);
            success &= connector.Write8(ConstantAddresses.ReflegaCost, reflectCost);
            success &= connector.Write8(ConstantAddresses.MagnegaCost, magnetCost);

            return success;
        }
    }

    private class AmnesiacMagician : Option
    {
        public AmnesiacMagician() : base("Amnesiac Magician", "Take away all of Sora's Magic.",
            Category.Sora, SubCategory.Stats,
            EffectFunction.StartTimed, durationSeconds: 60)
        { }

        private byte fire;
        private byte blizzard;
        private byte thunder;
        private byte cure;
        private byte reflect;
        private byte magnet;

        public override bool StartEffect(IPS2Connector connector)
        {
            bool success = true;
            success &= connector.Read8((ulong)ConstantAddresses.Fire, out fire);
            success &= connector.Read8((ulong)ConstantAddresses.Blizzard, out blizzard);
            success &= connector.Read8((ulong)ConstantAddresses.Thunder, out thunder);
            success &= connector.Read8((ulong)ConstantAddresses.Cure, out cure);
            success &= connector.Read8((ulong)ConstantAddresses.Reflect, out reflect);
            success &= connector.Read8((ulong)ConstantAddresses.Magnet, out magnet);

            success &= connector.Write8((ulong)ConstantAddresses.Fire, (byte)ConstantValues.None);
            success &= connector.Write8((ulong)ConstantAddresses.Blizzard, (byte)ConstantValues.None);
            success &= connector.Write8((ulong)ConstantAddresses.Thunder, (byte)ConstantValues.None);
            success &= connector.Write8((ulong)ConstantAddresses.Cure, (byte)ConstantValues.None);
            success &= connector.Write8((ulong)ConstantAddresses.Reflect, (byte)ConstantValues.None);
            success &= connector.Write8((ulong)ConstantAddresses.Magnet, (byte)ConstantValues.None);

            return success;
        }

        public override bool StopEffect(IPS2Connector connector)
        {
            bool success = true;
            success &= connector.Write8((ulong)ConstantAddresses.Fire, fire);
            success &= connector.Write8((ulong)ConstantAddresses.Blizzard, blizzard);
            success &= connector.Write8((ulong)ConstantAddresses.Thunder, thunder);
            success &= connector.Write8((ulong)ConstantAddresses.Cure, cure);
            success &= connector.Write8((ulong)ConstantAddresses.Reflect, reflect);
            success &= connector.Write8((ulong)ConstantAddresses.Magnet, magnet);
            return success;
        }
    }

    private class Itemaholic : Option
    {
        public Itemaholic() : base("Itemaholic", "Fill Sora's inventory with all items, accessories, armor and weapons.",
            Category.Sora, SubCategory.Stats,
            EffectFunction.StartTimed, durationSeconds: 60)
        { }

        // Used to store all the information about what held items Sora had before
        private readonly Dictionary<uint, byte> items = new()
            {
                { (uint)ConstantAddresses.Potion, 0 }, { (uint)ConstantAddresses.HiPotion, 0 }, { (uint)ConstantAddresses.Ether, 0 },
                { (uint)ConstantAddresses.MegaPotion, 0 }, { (uint)ConstantAddresses.MegaEther, 0 }, { (uint)ConstantAddresses.Elixir, 0 },
                { (uint)ConstantAddresses.Megalixir, 0 }, { (uint)ConstantAddresses.Tent, 0 }, { (uint)ConstantAddresses.DriveRecovery, 0 },
                { (uint)ConstantAddresses.HighDriveRecovery, 0 }, { (uint)ConstantAddresses.PowerBoost, 0 }, { (uint)ConstantAddresses.MagicBoost, 0 },
                { (uint)ConstantAddresses.DefenseBoost, 0 }, { (uint)ConstantAddresses.APBoost, 0 },

                { ConstantAddresses.AbilityRing, 0 }, { ConstantAddresses.EngineersRing, 0 }, { ConstantAddresses.TechniciansRing, 0 },
                { ConstantAddresses.ExpertsRing, 0 }, { ConstantAddresses.SardonyxRing, 0 }, { ConstantAddresses.TourmalineRing, 0 },
                { ConstantAddresses.AquamarineRing, 0 }, { ConstantAddresses.GarnetRing, 0 }, { ConstantAddresses.DiamondRing, 0 },
                { ConstantAddresses.SilverRing, 0 },{ ConstantAddresses.GoldRing, 0 }, { ConstantAddresses.PlatinumRing, 0 },
                { ConstantAddresses.MythrilRing, 0 }, { ConstantAddresses.OrichalcumRing, 0 }, { ConstantAddresses.MastersRing, 0 },
                { ConstantAddresses.MoonAmulet, 0 }, { ConstantAddresses.StarCharm, 0 }, { ConstantAddresses.SkillRing, 0 },
                { ConstantAddresses.SkillfulRing, 0 }, { ConstantAddresses.SoldierEarring, 0 }, { ConstantAddresses.FencerEarring, 0 },
                { ConstantAddresses.MageEarring, 0 }, { ConstantAddresses.SlayerEarring, 0 }, { ConstantAddresses.CosmicRing, 0 },
                { ConstantAddresses.Medal, 0 }, { ConstantAddresses.CosmicArts, 0 }, { ConstantAddresses.ShadowArchive, 0 },
                { ConstantAddresses.ShadowArchivePlus, 0 }, { ConstantAddresses.LuckyRing, 0 }, { ConstantAddresses.FullBloom, 0 },
                { ConstantAddresses.FullBloomPlus, 0 }, { ConstantAddresses.DrawRing, 0 }, { ConstantAddresses.ExecutivesRing, 0 },

                { ConstantAddresses.ElvenBandana, 0 }, { ConstantAddresses.DivineBandana, 0 }, { ConstantAddresses.PowerBand, 0 },
                { ConstantAddresses.BusterBand, 0 }, { ConstantAddresses.ProtectBelt, 0 }, { ConstantAddresses.GaiaBelt, 0 },
                { ConstantAddresses.CosmicBelt, 0 }, { ConstantAddresses.ShockCharm, 0 }, { ConstantAddresses.ShockCharmPlus, 0 },
                { ConstantAddresses.FireBangle, 0 }, { ConstantAddresses.FiraBangle, 0 }, { ConstantAddresses.FiragaBangle, 0 },
                { ConstantAddresses.FiragunBangle, 0 }, { ConstantAddresses.BlizzardArmlet, 0 }, { ConstantAddresses.BlizzaraArmlet, 0 },
                { ConstantAddresses.BlizzagaArmlet, 0 }, { ConstantAddresses.BlizzagunArmlet, 0 }, { ConstantAddresses.ThunderTrinket, 0 },
                { ConstantAddresses.ThundaraTrinket, 0 }, { ConstantAddresses.ThundagaTrinket, 0 }, { ConstantAddresses.ThundagunTrinket, 0 },
                { ConstantAddresses.ShadowAnklet, 0 }, { ConstantAddresses.DarkAnklet, 0 }, { ConstantAddresses.MidnightAnklet, 0 },
                { ConstantAddresses.ChaosAnklet, 0 }, { ConstantAddresses.AbasChain, 0 }, { ConstantAddresses.AegisChain, 0 },
                { ConstantAddresses.CosmicChain, 0 }, { ConstantAddresses.Acrisius, 0 }, { ConstantAddresses.AcrisiusPlus, 0 },
                { ConstantAddresses.PetiteRibbon, 0 }, { ConstantAddresses.Ribbon, 0 }, { ConstantAddresses.GrandRibbon, 0 },
                { ConstantAddresses.ChampionBelt, 0 },

                { ConstantAddresses.KingdomKey, 0 }, { ConstantAddresses.Oathkeeper, 0 }, { ConstantAddresses.Oblivion, 0 },
                { ConstantAddresses.DetectionSaber, 0 }, { ConstantAddresses.FrontierOfUltima, 0 }, { ConstantAddresses.StarSeeker, 0 },
                { ConstantAddresses.HiddenDragon, 0 }, { ConstantAddresses.HerosCrest, 0 }, { ConstantAddresses.Monochrome, 0 },
                { ConstantAddresses.FollowTheWind, 0 }, { ConstantAddresses.CircleOfLife, 0 }, { ConstantAddresses.PhotonDebugger, 0 },
                { ConstantAddresses.GullWing, 0 }, { ConstantAddresses.RumblingRose, 0 }, { ConstantAddresses.GuardianSoul, 0 },
                { ConstantAddresses.WishingLamp, 0 }, { ConstantAddresses.DecisivePumpkin, 0 }, { ConstantAddresses.SleepingLion, 0 },
                { ConstantAddresses.SweetMemories, 0 }, { ConstantAddresses.MysteriousAbyss, 0 }, { ConstantAddresses.BondOfFlame, 0 },
                { ConstantAddresses.FatalCrest, 0 }, { ConstantAddresses.Fenrir, 0 }, { ConstantAddresses.UltimaWeapon, 0 },
                { ConstantAddresses.TwoBecomeOne, 0 }, { ConstantAddresses.WinnersProof, 0 },
            };

        private readonly Dictionary<uint, ushort> slots = new()
            {
                { ConstantAddresses.SoraWeaponSlot, 0 }, { ConstantAddresses.SoraValorWeaponSlot, 0 }, { ConstantAddresses.SoraMasterWeaponSlot, 0 },
                { ConstantAddresses.SoraFinalWeaponSlot, 0 }, { ConstantAddresses.SoraArmorSlot1, 0 }, { ConstantAddresses.SoraArmorSlot2, 0 },
                { ConstantAddresses.SoraArmorSlot3, 0 }, { ConstantAddresses.SoraArmorSlot4, 0 }, { ConstantAddresses.SoraAccessorySlot1, 0 },
                { ConstantAddresses.SoraAccessorySlot2, 0 }, { ConstantAddresses.SoraAccessorySlot3, 0 }, { ConstantAddresses.SoraAccessorySlot4, 0 },
                { ConstantAddresses.SoraItemSlot1, 0 }, { ConstantAddresses.SoraItemSlot2, 0 }, { ConstantAddresses.SoraItemSlot3, 0 },
                { ConstantAddresses.SoraItemSlot4, 0 }, { ConstantAddresses.SoraItemSlot5, 0 }, { ConstantAddresses.SoraItemSlot6, 0 },
                { ConstantAddresses.SoraItemSlot7, 0 }, { ConstantAddresses.SoraItemSlot8, 0 }
            };

        public override bool StartEffect(IPS2Connector connector)
        {
            bool success = true;
            // Save all current items, before writing max value to them
            foreach (var (itemAddress, _) in items)
            {
                success &= connector.Read8(itemAddress, out byte itemCount);

                items[itemAddress] = itemCount;

                success &= connector.Write8(itemAddress, byte.MaxValue);
            }

            // Save all current slots
            foreach (var (slotAddress, _) in slots)
            {
                success &= connector.Read16LE(slotAddress, out ushort slotValue);

                slots[slotAddress] = slotValue;
            }

            return success;
        }

        // DO WE WANT TO REMOVE THESE AFTER OR JUST HAVE THIS AS A ONE TIME REDEEM?
        // We'll put things back to how they were after a timer
        public override bool StopEffect(IPS2Connector connector)
        {
            bool success = true;
            // Write back all saved items
            foreach (var (itemAddress, itemCount) in items)
            {
                success &= connector.Write8(itemAddress, itemCount);
            }

            // Write back all saved slots
            foreach (var (slotAddress, slotValue) in slots)
            {
                success &= connector.Write16LE(slotAddress, slotValue);
            }

            return success;
        }
    }

    private class SpringCleaning : Option
    {
        public SpringCleaning() : base("Spring Cleaning", "Remove all items, accessories, armor and weapons from Sora's inventory.",
            Category.Sora, SubCategory.Stats,
            EffectFunction.TryEffect, durationSeconds: 60)
        { }

        // Used to store all the information about what held items Sora had before
        private readonly Dictionary<uint, byte> items = new()
            {
                { (uint)ConstantAddresses.Potion, 0 }, { (uint)ConstantAddresses.HiPotion, 0 }, { (uint)ConstantAddresses.Ether, 0 },
                { (uint)ConstantAddresses.MegaPotion, 0 }, { (uint)ConstantAddresses.MegaEther, 0 }, { (uint)ConstantAddresses.Elixir, 0 },
                { (uint)ConstantAddresses.Megalixir, 0 }, { (uint)ConstantAddresses.Tent, 0 }, { (uint)ConstantAddresses.DriveRecovery, 0 },
                { (uint)ConstantAddresses.HighDriveRecovery, 0 }, { (uint)ConstantAddresses.PowerBoost, 0 }, { (uint)ConstantAddresses.MagicBoost, 0 },
                { (uint)ConstantAddresses.DefenseBoost, 0 }, { (uint)ConstantAddresses.APBoost, 0 },

                { ConstantAddresses.AbilityRing, 0 }, { ConstantAddresses.EngineersRing, 0 }, { ConstantAddresses.TechniciansRing, 0 },
                { ConstantAddresses.ExpertsRing, 0 }, { ConstantAddresses.SardonyxRing, 0 }, { ConstantAddresses.TourmalineRing, 0 },
                { ConstantAddresses.AquamarineRing, 0 }, { ConstantAddresses.GarnetRing, 0 }, { ConstantAddresses.DiamondRing, 0 },
                { ConstantAddresses.SilverRing, 0 },{ ConstantAddresses.GoldRing, 0 }, { ConstantAddresses.PlatinumRing, 0 },
                { ConstantAddresses.MythrilRing, 0 }, { ConstantAddresses.OrichalcumRing, 0 }, { ConstantAddresses.MastersRing, 0 },
                { ConstantAddresses.MoonAmulet, 0 }, { ConstantAddresses.StarCharm, 0 }, { ConstantAddresses.SkillRing, 0 },
                { ConstantAddresses.SkillfulRing, 0 }, { ConstantAddresses.SoldierEarring, 0 }, { ConstantAddresses.FencerEarring, 0 },
                { ConstantAddresses.MageEarring, 0 }, { ConstantAddresses.SlayerEarring, 0 }, { ConstantAddresses.CosmicRing, 0 },
                { ConstantAddresses.Medal, 0 }, { ConstantAddresses.CosmicArts, 0 }, { ConstantAddresses.ShadowArchive, 0 },
                { ConstantAddresses.ShadowArchivePlus, 0 }, { ConstantAddresses.LuckyRing, 0 }, { ConstantAddresses.FullBloom, 0 },
                { ConstantAddresses.FullBloomPlus, 0 }, { ConstantAddresses.DrawRing, 0 }, { ConstantAddresses.ExecutivesRing, 0 },

                { ConstantAddresses.ElvenBandana, 0 }, { ConstantAddresses.DivineBandana, 0 }, { ConstantAddresses.PowerBand, 0 },
                { ConstantAddresses.BusterBand, 0 }, { ConstantAddresses.ProtectBelt, 0 }, { ConstantAddresses.GaiaBelt, 0 },
                { ConstantAddresses.CosmicBelt, 0 }, { ConstantAddresses.ShockCharm, 0 }, { ConstantAddresses.ShockCharmPlus, 0 },
                { ConstantAddresses.FireBangle, 0 }, { ConstantAddresses.FiraBangle, 0 }, { ConstantAddresses.FiragaBangle, 0 },
                { ConstantAddresses.FiragunBangle, 0 }, { ConstantAddresses.BlizzardArmlet, 0 }, { ConstantAddresses.BlizzaraArmlet, 0 },
                { ConstantAddresses.BlizzagaArmlet, 0 }, { ConstantAddresses.BlizzagunArmlet, 0 }, { ConstantAddresses.ThunderTrinket, 0 },
                { ConstantAddresses.ThundaraTrinket, 0 }, { ConstantAddresses.ThundagaTrinket, 0 }, { ConstantAddresses.ThundagunTrinket, 0 },
                { ConstantAddresses.ShadowAnklet, 0 }, { ConstantAddresses.DarkAnklet, 0 }, { ConstantAddresses.MidnightAnklet, 0 },
                { ConstantAddresses.ChaosAnklet, 0 }, { ConstantAddresses.AbasChain, 0 }, { ConstantAddresses.AegisChain, 0 },
                { ConstantAddresses.CosmicChain, 0 }, { ConstantAddresses.Acrisius, 0 }, { ConstantAddresses.AcrisiusPlus, 0 },
                { ConstantAddresses.PetiteRibbon, 0 }, { ConstantAddresses.Ribbon, 0 }, { ConstantAddresses.GrandRibbon, 0 },
                { ConstantAddresses.ChampionBelt, 0 },

                { ConstantAddresses.KingdomKey, 0 }, { ConstantAddresses.Oathkeeper, 0 }, { ConstantAddresses.Oblivion, 0 },
                { ConstantAddresses.DetectionSaber, 0 }, { ConstantAddresses.FrontierOfUltima, 0 }, { ConstantAddresses.StarSeeker, 0 },
                { ConstantAddresses.HiddenDragon, 0 }, { ConstantAddresses.HerosCrest, 0 }, { ConstantAddresses.Monochrome, 0 },
                { ConstantAddresses.FollowTheWind, 0 }, { ConstantAddresses.CircleOfLife, 0 }, { ConstantAddresses.PhotonDebugger, 0 },
                { ConstantAddresses.GullWing, 0 }, { ConstantAddresses.RumblingRose, 0 }, { ConstantAddresses.GuardianSoul, 0 },
                { ConstantAddresses.WishingLamp, 0 }, { ConstantAddresses.DecisivePumpkin, 0 }, { ConstantAddresses.SleepingLion, 0 },
                { ConstantAddresses.SweetMemories, 0 }, { ConstantAddresses.MysteriousAbyss, 0 }, { ConstantAddresses.BondOfFlame, 0 },
                { ConstantAddresses.FatalCrest, 0 }, { ConstantAddresses.Fenrir, 0 }, { ConstantAddresses.UltimaWeapon, 0 },
                { ConstantAddresses.TwoBecomeOne, 0 }, { ConstantAddresses.WinnersProof, 0 },
            };

        private readonly Dictionary<uint, ushort> slots = new()
            {
                { ConstantAddresses.SoraWeaponSlot, 0 }, { ConstantAddresses.SoraValorWeaponSlot, 0 }, { ConstantAddresses.SoraMasterWeaponSlot, 0 },
                { ConstantAddresses.SoraFinalWeaponSlot, 0 }, { ConstantAddresses.SoraArmorSlot1, 0 }, { ConstantAddresses.SoraArmorSlot2, 0 },
                { ConstantAddresses.SoraArmorSlot3, 0 }, { ConstantAddresses.SoraArmorSlot4, 0 }, { ConstantAddresses.SoraAccessorySlot1, 0 },
                { ConstantAddresses.SoraAccessorySlot2, 0 }, { ConstantAddresses.SoraAccessorySlot3, 0 }, { ConstantAddresses.SoraAccessorySlot4, 0 },
                { ConstantAddresses.SoraItemSlot1, 0 }, { ConstantAddresses.SoraItemSlot2, 0 }, { ConstantAddresses.SoraItemSlot3, 0 },
                { ConstantAddresses.SoraItemSlot4, 0 }, { ConstantAddresses.SoraItemSlot5, 0 }, { ConstantAddresses.SoraItemSlot6, 0 },
                { ConstantAddresses.SoraItemSlot7, 0 }, { ConstantAddresses.SoraItemSlot8, 0 }
            };

        public override bool StartEffect(IPS2Connector connector)
        {
            bool success = true;
            // Save all current items, before writing max value to them
            foreach (var (itemAddress, _) in items)
            {
                success &= connector.Read8(itemAddress, out byte itemCount);

                items[itemAddress] = itemCount;

                success &= connector.Write8(itemAddress, byte.MinValue);
            }

            // Save all current slots
            foreach (var (slotAddress, _) in slots)
            {
                success &= connector.Read16LE(slotAddress, out ushort slotValue);

                slots[slotAddress] = slotValue;

                if (slotAddress != ConstantAddresses.SoraWeaponSlot && slotAddress != ConstantAddresses.SoraValorWeaponSlot &&
                    slotAddress != ConstantAddresses.SoraMasterWeaponSlot && slotAddress != ConstantAddresses.SoraFinalWeaponSlot)
                {
                    success &= connector.Write16LE(slotAddress, ushort.MinValue);
                }
            }

            return success;
        }

        // DO WE WANT TO REMOVE THESE AFTER OR JUST HAVE THIS AS A ONE TIME REDEEM?
        // We'll put things back to how they were after a timer
        public override bool StopEffect(IPS2Connector connector)
        {
            bool success = true;
            // Write back all saved items
            foreach (var (itemAddress, itemCount) in items)
            {
                success &= connector.Write8(itemAddress, itemCount);
            }

            // Write back all saved slots
            foreach (var (slotAddress, slotValue) in slots)
            {
                success &= connector.Write16LE(slotAddress, slotValue);
            }
            return success;
        }
    }

    private class SummonChauffeur : Option
    {
        public SummonChauffeur() : base("Summon Chauffeur", "Give all Drives and Summons to Sora.",
            Category.Sora, SubCategory.Summon,
            EffectFunction.StartTimed, durationSeconds: 60)
        { }

        // Used to store all the information about what held items Sora had before
        private readonly Dictionary<uint, byte> drivesSummons = new()
            {
                { ConstantAddresses.DriveForms, 0 }, { ConstantAddresses.DriveLimitForm, 0 },
                //{ (uint)ConstantAddresses.UkeleleBaseballCharm, 0 }, 
                { ConstantAddresses.LampFeatherCharm, 0 },

                { ConstantAddresses.Drive, 0 }, { ConstantAddresses.MaxDrive, 0 }
            };

        public override bool StartEffect(IPS2Connector connector)
        {
            bool success = true;
            // Save all current items, before writing max value to them
            foreach (var (driveSummon, _) in drivesSummons)
            {
                success &= connector.Read8(driveSummon, out byte value);

                drivesSummons[driveSummon] = value;

                if (driveSummon == ConstantAddresses.DriveForms)
                {
                    success &= connector.Write8(driveSummon, 127);
                }
                else if (driveSummon == ConstantAddresses.DriveLimitForm)
                {
                    success &= connector.Write8(driveSummon, 8);
                }
                //else if (driveSummon == ConstantAddresses.UkeleleBaseballCharm)
                //{
                //    connector.Write8(value, 9);
                //}
                else if (driveSummon == ConstantAddresses.LampFeatherCharm)
                {
                    success &= connector.Write8(driveSummon, 48);
                }
                else
                {
                    success &= connector.Write8(driveSummon, byte.MaxValue);
                }
            }
            return success;
        }

        // DO WE WANT TO REMOVE THESE AFTER OR JUST HAVE THIS AS A ONE TIME REDEEM?
        // We'll put things back to how they were after a timer
        public override bool StopEffect(IPS2Connector connector)
        {
            bool success = true;
            // Write back all saved items
            foreach (var (driveSummon, value) in drivesSummons)
            {
                success &= connector.Write8(driveSummon, value);
            }
            return success;
        }
    }

    private class SummonTrainer : Option
    {
        public SummonTrainer() : base("Summon Trainer", "Remove all Drives and Summons from Sora.",
            Category.Sora, SubCategory.Summon,
            EffectFunction.StartTimed, durationSeconds: 60)
        { }

        // Used to store all the information about what held items Sora had before
        private readonly Dictionary<uint, byte> drivesSummons = new()
            {
                { ConstantAddresses.DriveForms, 0 }, { ConstantAddresses.DriveLimitForm, 0 },
                //{ (uint)ConstantAddresses.UkeleleBaseballCharm, 0 }, 
                { ConstantAddresses.LampFeatherCharm, 0 },

                { ConstantAddresses.Drive, 0 }, { ConstantAddresses.MaxDrive, 0 }
            };

        public override bool StartEffect(IPS2Connector connector)
        {
            bool success = true;
            // Save all current items, before writing max value to them
            foreach (var (driveSummon, _) in drivesSummons)
            {
                success &= connector.Read8(driveSummon, out byte value);

                drivesSummons[driveSummon] = value;

                success &= connector.Write8(driveSummon, byte.MinValue);
            }

            return success;
        }

        // DO WE WANT TO REMOVE THESE AFTER OR JUST HAVE THIS AS A ONE TIME REDEEM?
        // Adding in timer
        public override bool StopEffect(IPS2Connector connector)
        {
            bool success = true;
            // Write back all saved items
            foreach (var (driveSummon, value) in drivesSummons)
            {
                success &= connector.Write8(driveSummon, value);
            }
            return success;
        }
    }

    private class HeroSora : Option
    {
        public HeroSora() : base("Hero Sora", "Set Sora to HERO mode, including Stats, Items, Magic, Drives and Summons.",
            Category.Sora, SubCategory.Stats,
            EffectFunction.StartTimed, durationSeconds: 30)
        {
            expertMagician = new ExpertMagician();
            itemaholic = new Itemaholic();
            summonChauffeur = new SummonChauffeur();
        }

        private byte level;
        private uint hp;
        private uint maxHp;
        private uint mp;
        private uint maxMp;
        private byte strength;
        private byte magic;
        private byte defense;
        private byte ap;

        private readonly ExpertMagician expertMagician;
        private readonly Itemaholic itemaholic;
        private readonly SummonChauffeur summonChauffeur;

        public override bool StartEffect(IPS2Connector connector)
        {
            bool success = true;
            success &= connector.Read8(ConstantAddresses.Level, out level);
            success &= connector.Read32LE(ConstantAddresses.HP, out hp);
            success &= connector.Read32LE(ConstantAddresses.MaxHP, out maxHp);
            success &= connector.Read32LE(ConstantAddresses.MP, out mp);
            success &= connector.Read32LE(ConstantAddresses.MaxMP, out maxMp);
            success &= connector.Read8(ConstantAddresses.Strength, out strength);
            success &= connector.Read8(ConstantAddresses.Magic, out magic);
            success &= connector.Read8(ConstantAddresses.Defense, out defense);
            success &= connector.Read8(ConstantAddresses.AP, out ap);

            success &= connector.Write8(ConstantAddresses.Level, 99);
            success &= connector.Write32LE(ConstantAddresses.HP, 160);
            success &= connector.Write32LE(ConstantAddresses.MaxHP, 160);
            success &= connector.Write32LE(ConstantAddresses.MP, byte.MaxValue);
            success &= connector.Write32LE(ConstantAddresses.MaxMP, byte.MaxValue);
            success &= connector.Write8(ConstantAddresses.Strength, byte.MaxValue);
            success &= connector.Write8(ConstantAddresses.Magic, byte.MaxValue);
            success &= connector.Write8(ConstantAddresses.Defense, byte.MaxValue);
            success &= connector.Write8(ConstantAddresses.AP, byte.MaxValue);

            success &= expertMagician.StartEffect(connector);
            success &= itemaholic.StartEffect(connector);
            success &= summonChauffeur.StartEffect(connector);

            return success;
        }

        public override bool StopEffect(IPS2Connector connector)
        {
            bool success = true;

            success &= connector.Write8(ConstantAddresses.Level, level);
            success &= connector.Write32LE(ConstantAddresses.HP, hp);
            success &= connector.Write32LE(ConstantAddresses.MaxHP, maxHp);
            success &= connector.Write32LE(ConstantAddresses.MP, mp);
            success &= connector.Write32LE(ConstantAddresses.MaxMP, maxMp);
            success &= connector.Write8(ConstantAddresses.Strength, strength);
            success &= connector.Write8(ConstantAddresses.Magic, magic);
            success &= connector.Write8(ConstantAddresses.Defense, defense);
            success &= connector.Write8(ConstantAddresses.AP, ap);

            success &= expertMagician.StopEffect(connector);
            success &= itemaholic.StopEffect(connector);
            success &= summonChauffeur.StopEffect(connector);

            return success;
        }
    }

    private class ZeroSora : Option
    {
        public ZeroSora() : base("Zero Sora", "Set Sora to ZERO mode, including Stats, Items, Magic, Drives and Summons.",
            Category.Sora, SubCategory.Stats,
            EffectFunction.StartTimed, durationSeconds: 30)
        {
            amnesiacMagician = new AmnesiacMagician();
            springCleaning = new SpringCleaning();
            summonTrainer = new SummonTrainer();
        }

        private byte level;
        private uint hp;
        private uint maxHp;
        private uint mp;
        private uint maxMp;
        private byte strength;
        private byte magic;
        private byte defense;
        private byte ap;

        private readonly AmnesiacMagician amnesiacMagician;
        private readonly SpringCleaning springCleaning;
        private readonly SummonTrainer summonTrainer;

        public override bool StartEffect(IPS2Connector connector)
        {
            bool success = true;

            success &= connector.Read8(ConstantAddresses.Level, out level);
            success &= connector.Read32LE(ConstantAddresses.HP, out hp);
            success &= connector.Read32LE(ConstantAddresses.MaxHP, out maxHp);
            success &= connector.Read32LE(ConstantAddresses.MP, out mp);
            success &= connector.Read32LE(ConstantAddresses.MaxMP, out maxMp);
            success &= connector.Read8(ConstantAddresses.Strength, out strength);
            success &= connector.Read8(ConstantAddresses.Magic, out magic);
            success &= connector.Read8(ConstantAddresses.Defense, out defense);
            success &= connector.Read8(ConstantAddresses.AP, out ap);

            success &= connector.Write8(ConstantAddresses.Level, byte.MinValue + 1);
            success &= connector.Write32LE(ConstantAddresses.HP, uint.MinValue + 1);
            success &= connector.Write32LE(ConstantAddresses.MaxHP, uint.MinValue + 1);
            success &= connector.Write32LE(ConstantAddresses.MP, uint.MinValue);
            success &= connector.Write32LE(ConstantAddresses.MaxMP, uint.MinValue);
            success &= connector.Write8(ConstantAddresses.Strength, byte.MinValue);
            success &= connector.Write8(ConstantAddresses.Magic, byte.MinValue);
            success &= connector.Write8(ConstantAddresses.Defense, byte.MinValue);
            success &= connector.Write8(ConstantAddresses.AP, byte.MinValue);

            success &= amnesiacMagician.StartEffect(connector);
            success &= springCleaning.StartEffect(connector);
            success &= summonTrainer.StartEffect(connector);

            return success;
        }

        public override bool StopEffect(IPS2Connector connector)
        {
            bool success = true;
            success &= connector.Write8(ConstantAddresses.Level, level);
            success &= connector.Write32LE(ConstantAddresses.HP, hp);
            success &= connector.Write32LE(ConstantAddresses.MaxHP, maxHp);
            success &= connector.Write32LE(ConstantAddresses.MP, mp);
            success &= connector.Write32LE(ConstantAddresses.MaxMP, maxMp);
            success &= connector.Write8(ConstantAddresses.Strength, strength);
            success &= connector.Write8(ConstantAddresses.Magic, magic);
            success &= connector.Write8(ConstantAddresses.Defense, defense);
            success &= connector.Write8(ConstantAddresses.AP, ap);

            success &= amnesiacMagician.StopEffect(connector);
            success &= springCleaning.StopEffect(connector);
            success &= summonTrainer.StopEffect(connector);

            return success;
        }
    }

    private class ProCodes : Option
    {
        public ProCodes() : base("Pro-Codes", "Set Sora to consistently lose HP, MP and Drive Gauges",
            Category.Sora, SubCategory.Stats,
            EffectFunction.RepeatAction, durationSeconds: 60, refreshInterval: 1000)
        {
        }

        private uint hp;
        private uint mp;
        private uint drive;

        public override bool StartEffect(IPS2Connector connector)
        {
            bool success = true;

            success &= connector.Read32LE(ConstantAddresses.HP, out hp);
            success &= connector.Read32LE(ConstantAddresses.MP, out mp);
            success &= connector.Read32LE(ConstantAddresses.Drive, out drive);

            return success;
        }

        public override bool DoEffect(IPS2Connector connector)
        {
            bool success = true;

            success &= connector.Write32LE(ConstantAddresses.HP, hp - 1);
            success &= connector.Write32LE(ConstantAddresses.MP, mp - 1);

            // Limit this to drop only 6 at most
            if (hp % 10 == 0)
            {
                success &= connector.Write32LE(ConstantAddresses.Drive, drive - 1);
            }

            return success;
        }
    }

    private class EZCodes : Option
    {
        public EZCodes() : base("EZ-Codes", "Set Sora to consistently gain HP, MP and Drive Gauges",
            Category.Sora, SubCategory.Stats,
            EffectFunction.RepeatAction, durationSeconds: 60, refreshInterval: 1000)
        {
        }

        private uint hp;
        private uint mp;
        private uint drive;

        public override bool StartEffect(IPS2Connector connector)
        {
            bool success = true;

            success &= connector.Read32LE(ConstantAddresses.HP, out hp);
            success &= connector.Read32LE(ConstantAddresses.MP, out mp);
            success &= connector.Read32LE(ConstantAddresses.Drive, out drive);

            return success;
        }

        public override bool DoEffect(IPS2Connector connector)
        {
            bool success = true;

            success &= connector.Write32LE(ConstantAddresses.HP, hp + 1);
            success &= connector.Write32LE(ConstantAddresses.MP, mp + 1);

            // Limit this to gain only 6 at most
            if (hp % 10 == 0)
            {
                success &= connector.Write32LE(ConstantAddresses.Drive, drive + 1);
            }

            return success;
        }
    }
    #endregion
}



#region Enums
public enum Category
{
    None = 0,
    Enemy = 1,
    Environment = 2,
    Item = 3,
    ModelSwap = 4,
    Party = 5,
    Sora = 6,
    Equipment = 6,
}

public enum DataType
{
    None = 0,

    Binary = 1,
    Byte = 2,
    TwoBytes = 3,
    FourBytes = 4,
    EightBytes = 5,
    Float = 6,
    Double = 7,
    String = 8,
    ByteArray = 9
}

public enum ManipulationType
{
    None = 0,

    Set = 1,
    Add = 2,
    Subtract = 3
}

public enum SubCategory
{
    None = 0,

    Accessory = 1,
    Armor = 2,
    BaseItem = 3,
    Munny = 4,
    Weapon = 5, // Weapon to be used for Party Members - Moved down to be more specific - Keyblade, Staff, Shield, Weapon (Party)
    Ability = 6,
    Drive = 7,
    QuickMenu = 8,
    Magic = 9,
    Stats = 10,
    Summon = 11,
    Keyblade = 12,
    Staff = 13,
    Shield = 14,
    Friend = 15,
    Enemy = 16
}
#endregion Enums

#region Constants
public static class ConstantAddresses
{
    public static uint None = 0x0;

    #region Munny
    public static uint Munny = 0x2032DF70;
    #endregion Munny

    #region Accessory
    public static uint AbilityRing = 0x2032F0B7;
    public static uint EngineersRing = 0x2032F0B8;
    public static uint TechniciansRing = 0x2032F0B9;
    public static uint ExpertsRing = 0x2032F0BA;
    public static uint SardonyxRing = 0x2032F0BB;
    public static uint TourmalineRing = 0x2032F0BC;
    public static uint AquamarineRing = 0x2032F0BD;
    public static uint GarnetRing = 0x2032F0BE;
    public static uint DiamondRing = 0x2032F0BF;
    public static uint SilverRing = 0x2032F0C0;
    public static uint GoldRing = 0x2032F0C1;
    public static uint PlatinumRing = 0x2032F0C2;
    public static uint MythrilRing = 0x2032F0C3;
    public static uint OrichalcumRing = 0x2032F0CA;
    public static uint MastersRing = 0x2032F0CB;
    public static uint MoonAmulet = 0x2032F0CC;
    public static uint StarCharm = 0x2032F0CE;
    public static uint SkillRing = 0x2032F0CF;
    public static uint SkillfulRing = 0x2032F0D0;
    public static uint SoldierEarring = 0x2032F0D6;
    public static uint FencerEarring = 0x2032F0D7;
    public static uint MageEarring = 0x2032F0D8;
    public static uint SlayerEarring = 0x2032F0DC;
    public static uint CosmicRing = 0x2032F0DD;
    public static uint Medal = 0x2032F0E0;
    public static uint CosmicArts = 0x2032F0E1;
    public static uint ShadowArchive = 0x2032F0E2;
    public static uint ShadowArchivePlus = 0x2032F0E7;
    public static uint LuckyRing = 0x2032F0E8;
    public static uint FullBloom = 0x2032F0E9;
    public static uint FullBloomPlus = 0x2032F0EB;
    public static uint DrawRing = 0x2032F0EA;
    public static uint ExecutivesRing = 0x2032F1E5;
    #endregion Accessory

    #region Armor
    public static uint ElvenBandana = 0x2032F0EC;
    public static uint DivineBandana = 0x2032F0ED;
    public static uint PowerBand = 0x2032F0EE;
    public static uint BusterBand = 0x2032F0F6;
    public static uint ProtectBelt = 0x2032F0F7;
    public static uint GaiaBelt = 0x2032F0FA;
    public static uint CosmicBelt = 0x2032F101;
    public static uint ShockCharm = 0x2032F102;
    public static uint ShockCharmPlus = 0x2032F103;
    public static uint FireBangle = 0x2032F107;
    public static uint FiraBangle = 0x2032F108;
    public static uint FiragaBangle = 0x2032F109;
    public static uint FiragunBangle = 0x2032F10A;
    public static uint BlizzardArmlet = 0x2032F10C;
    public static uint BlizzaraArmlet = 0x2032F10D;
    public static uint BlizzagaArmlet = 0x2032F10E;
    public static uint BlizzagunArmlet = 0x2032F10F;
    public static uint ThunderTrinket = 0x2032F112;
    public static uint ThundaraTrinket = 0x2032F113;
    public static uint ThundagaTrinket = 0x2032F114;
    public static uint ThundagunTrinket = 0x2032F115;
    public static uint ShadowAnklet = 0x2032F129;
    public static uint DarkAnklet = 0x2032F12B;
    public static uint MidnightAnklet = 0x2032F12C;
    public static uint ChaosAnklet = 0x2032F12D;
    public static uint AbasChain = 0x2032F12F;
    public static uint AegisChain = 0x2032F130;
    public static uint CosmicChain = 0x2032F136;
    public static uint Acrisius = 0x2032F131;
    public static uint AcrisiusPlus = 0x2032F135;
    public static uint PetiteRibbon = 0x2032F134;
    public static uint Ribbon = 0x2032F132;
    public static uint GrandRibbon = 0x2032F104;
    public static uint ChampionBelt = 0x2032F133;
    #endregion Armor

    #region Base Item
    public static int Potion = 0x2032F0B0;
    public static int HiPotion = 0x2032F0B1;
    public static int Ether = 0x2032F0B2;
    public static int Elixir = 0x2032F0B3;
    public static int MegaPotion = 0x2032F0B4;
    public static int MegaEther = 0x2032F0B5;
    public static int Megalixir = 0x2032F0B6;
    public static int Tent = 0x2032F111;
    public static int DriveRecovery = 0x2032F194;
    public static int HighDriveRecovery = 0x2032F195;
    public static int PowerBoost = 0x2032F196;
    public static int MagicBoost = 0x2032F197;
    public static int DefenseBoost = 0x2032F198;
    public static int APBoost = 0x2032F199;
    #endregion Base Item

    #region Keyblade
    public static uint KingdomKey = 0x2032F0D1;
    public static uint Oathkeeper = 0x2032F0D2;
    public static uint Oblivion = 0x2032F0D3;
    public static uint DetectionSaber = 0x2032F0D4;
    public static uint FrontierOfUltima = 0x2032F0D5;
    public static uint StarSeeker = 0x2032F1AB;
    public static uint HiddenDragon = 0x2032F1AC;
    public static uint HerosCrest = 0x2032F1AF;
    public static uint Monochrome = 0x2032F1B0;
    public static uint FollowTheWind = 0x2032F1B1;
    public static uint CircleOfLife = 0x2032F1B2;
    public static uint PhotonDebugger = 0x2032F1B3;
    public static uint GullWing = 0x2032F1B4;
    public static uint RumblingRose = 0x2032F1B5;
    public static uint GuardianSoul = 0x2032F1B6;
    public static uint WishingLamp = 0x2032F1B7;
    public static uint DecisivePumpkin = 0x2032F1B8;
    public static uint SleepingLion = 0x2032F1B9;
    public static uint SweetMemories = 0x2032F1BA;
    public static uint MysteriousAbyss = 0x2032F1BB;
    public static uint BondOfFlame = 0x2032F1BC;
    public static uint FatalCrest = 0x2032F1BD;
    public static uint Fenrir = 0x2032F1BE;
    public static uint UltimaWeapon = 0x2032F1BF;
    public static uint TwoBecomeOne = 0x2032F1C8;
    public static uint WinnersProof = 0x2032F1C9;
    #endregion Keyblade

    #region Staff
    public static uint MagesStaff = 0x2032F0F3;
    public static uint HammerStaff = 0x2032F11F;
    public static uint VictoryBell = 0x2032F120;
    public static uint MeteorStaff = 0x2032F121;
    public static uint CometStaff = 0x2032F122;
    public static uint LordsBroom = 0x2032F123;
    public static uint WisdomWand = 0x2032F124;
    public static uint RisingDragon = 0x2032F125;
    public static uint NobodyLance = 0x2032F126;
    public static uint ShamansRelic = 0x2032F127;
    public static uint ShamansRelicPlus = 0x2032F1E6;
    public static uint StaffOfDetection = 0x2032F12A;
    public static uint SaveTheQueen = 0x2032F12D;
    public static uint SaveTheQueenPlus = 0x2032F1C2;
    public static uint Centurion = 0x2032F1CA;
    public static uint CenturionPlus = 0x2032F1CB;
    public static uint PlainMushroom = 0x2032F1CC;
    public static uint PlainMushroomPlus = 0x2032F1CD;
    public static uint PreciousMushroom = 0x2032F1CE;
    public static uint PreciousMushroomPlus = 0x2032F1CF;
    public static uint PremiumMushroom = 0x2032F1D0;
    #endregion Staff

    #region Shield
    public static uint KnightsShield = 0x2032F0D9;
    public static uint DetectionShield = 0x2032F0DA;
    public static uint AdamantShield = 0x2032F116;
    public static uint ChainGear = 0x2032F117;
    public static uint OgreShield = 0x2032F118;
    public static uint FallingStar = 0x2032F119;
    public static uint Dreamcloud = 0x2032F11A;
    public static uint KnightDefender = 0x2032F11B;
    public static uint GenjiShield = 0x2032F11C;
    public static uint AkashicRecord = 0x2032F11D;
    public static uint AkashicRecordPlus = 0x2032F1E7;
    public static uint NobodyGuard = 0x2032F11E;
    public static uint SaveTheKing = 0x2032F1AE;
    public static uint SaveTheKingPlus = 0x2032F1C3;
    public static uint FrozenPride = 0x2032F1D1;
    public static uint FrozenPridePlus = 0x2032F1D2;
    public static uint JoyousMushroom = 0x2032F1D3;
    public static uint JoyousMushroomPlus = 0x2032F1D4;
    public static uint MajesticMushroom = 0x2032F1D5;
    public static uint MajesticMushroomPlus = 0x2032F1D6;
    public static uint UltimateMushroom = 0x2032F1D7;
    #endregion Shield

    #region Magic
    public static int Fire = 0x2032F0C4;
    public static int Blizzard = 0x2032F0C5;
    public static int Thunder = 0x2032F0C6;
    public static int Cure = 0x2032F0C7;
    public static int Magnet = 0x2032F0FF;
    public static int Reflect = 0x2032F100;
    #endregion Magic

    #region MP Cost
    public static uint FireCost = 0x21CCBCE0;
    public static uint FiraCost = 0x21CCC8E0;
    public static uint FiragaCost = 0x21CCC910;
    public static uint BlizzardCost = 0x21CCBD40;
    public static uint BlizzaraCost = 0x21CCC940;
    public static uint BlizzagaCost = 0x21CCC970;
    public static uint ThunderCost = 0x21CCBD10;
    public static uint ThundaraCost = 0x21CCC9A0;
    public static uint ThundagaCost = 0x21CCC9D0;
    public static uint CureCost = 0x21CCBD70;
    public static uint CuraCost = 0x21CCCA00;
    public static uint CuragaCost = 0x21CCCA30;
    public static uint MagnetCost = 0x21CCD240;
    public static uint MagneraCost = 0x21CCD270;
    public static uint MagnegaCost = 0x21CCD2A0;
    public static uint ReflectCost = 0x21CCD2D0;
    public static uint RefleraCost = 0x21CCD300;
    public static uint ReflegaCost = 0x21CCD330;

    public static uint TrinityLimitCost = 0x21CD0B40;
    public static uint DuckFlareCost = 0x21CCF160;
    public static uint CometCost = 0x21CCE620;
    public static uint WhirliGoofCost = 0x21CCE110;
    public static uint KnocksmashCost = 0x21CCF040;
    public static uint RedRocketCost = 0x21CCCC40;
    public static uint TwinHowlCost = 0x21CCC130;
    public static uint BushidoCost = 0x21CCC2B0;
    public static uint BluffCost = 0x21CCF3A0;
    public static uint DanceCallCost = 0x21CCFCA0;
    public static uint SpeedsterCost = 0x21CCF280;
    public static uint WildcatCost = 0x21CCF730;
    public static uint SetupCost = 0x21CCFE80;
    public static uint SessionCost = 0x21CD1AD0;

    public static uint StrikeRaidCost = 0x21CD3150;
    public static uint SonicBladeCost = 0x21CD3030;
    public static uint RagnarokCost = 0x21CD2F10;
    public static uint ArsArcanumCost = 0x21CD30C0;
    #endregion MP Cost

    #region Characters
    public static uint Sora = 0x21CE0B68;
    public static uint LionSora = 0x21CE1250;
    public static uint TimelessRiverSora = 0x21CE121C;
    public static uint HalloweenSora = 0x21CE0FAC;
    public static uint ChristmasSora = 0x21CE0FE0;
    public static uint SpaceParanoidsSora = 0x21CE11E8;

    public static uint ValorFormSora = 0x21CE0B70;
    public static uint WisdomFormSora = 0x21CE0B72;
    public static uint LimitFormSora = 0x21CE0B74;
    public static uint MasterFormSora = 0x21CE0B76;
    public static uint FinalFormSora = 0x21CE0B78;
    public static uint AntiFormSora = 0x21CE0B7A;

    public static uint Donald = 0x21CE0B6A;
    public static uint BirdDonald = 0x21CE1252;
    public static uint TimelessRiverDonald = 0x21CE121E;
    public static uint HalloweenDonald = 0x21CE0FAE;
    public static uint ChristmasDonald = 0x21CE0FE2;
    public static uint SpaceParanoidsDonald = 0x21CE11EA;

    public static uint Goofy = 0x21CE0B6C;
    public static uint TortoiseGoofy = 0x21CE1254;
    public static uint TimelessRiverGoofy = 0x21CE1220;
    public static uint HalloweenGoofy = 0x21CE0FB0;
    public static uint ChristmasGoofy = 0x21CE0FE4;
    public static uint SpaceParanoidsGoofy = 0x21CE11EC;

    public static uint Mulan = 0x21CE10B6;
    public static uint Beast = 0x21CE104E;
    public static uint Auron = 0x21CE0EE2;
    public static uint CaptainJackSparrow = 0x21CE0DDE;
    public static uint Aladdin = 0x21CE0F7E;
    public static uint JackSkellington = 0x21CE101A;
    public static uint Simba = 0x21CE1256;
    public static uint Tron = 0x21CE11EE;
    public static uint Riku = 0x21CE10EA;
    #endregion Characters

    #region Stats
    public static uint Level = 0x2032E02F;
    public static uint HP = 0x21C6C750;
    public static uint MaxHP = 0x21C6C754;

    public static uint Invincibility_1 = 0x200F7000;
    public static uint Invincibility_2 = 0x200F7004;
    public static uint Invincibility_3 = 0x200F7008;
    public static uint Invincibility_4 = 0x201666F8;

    public static uint MP = 0x21C6C8D0;
    public static uint MaxMP = 0x21C6C8D4;
    public static uint RechargeRate = 0x21C6C90C;
    public static uint Strength = 0x21C6C8D8;
    public static uint Magic = 0x21C6C8DA;
    public static uint Defense = 0x21C6C8DC;
    public static uint AP = 0x2032E028;
    public static uint StrengthBoostStat = 0x2032E029;
    public static uint MagicBoostStat = 0x2032E02A;
    public static uint DefenseBoostStat = 0x2032E02B;
    public static uint APBoostStat = 0x2032E028;
    public static uint Speed = 0x21ACDDE4;
    #endregion Stats

    #region Summons
    // This shares the same as our Drive Forms - 1 is Ukelele and 8 is Baseball
    //public static uint UkeleleBaseballCharm = 0x2032F1F0;
    public static uint LampFeatherCharm = 0x2032F1F4;
    #endregion Summons

    #region Drives
    public static uint Drive = 0x21C6C901;
    public static uint MaxDrive = 0x21C6C902;
    public static uint DriveTime = 0x21C6C904;
    public static uint DriveAvailable = 0x20351EB8;
    public static uint DriveForms = 0x2032F1F0;
    public static uint DriveLimitForm = 0x2032F1FA;

    public static uint ReactionPopup = 0x21C5FF48;
    public static uint ReactionOption = 0x21C5FF4E;
    public static uint ReactionEnable = 0x21C5FF51;
    public static uint ButtonPress = 0x2034D45D;
    #endregion Drives

    #region Equipment
    public static uint SoraWeaponSlot = 0x2032E020;
    public static uint SoraValorWeaponSlot = 0x2032EE24;
    public static uint SoraMasterWeaponSlot = 0x2032EECC;
    public static uint SoraFinalWeaponSlot = 0x2032EF04;
    public static uint DonaldWeaponSlot = 0x2032E134;
    public static uint GoofyWeaponSlot = 0x2032E248;


    public static uint SoraArmorSlotCount = 0x2032E030;
    public static uint SoraArmorSlot1 = 0x2032E034;
    public static uint SoraArmorSlot2 = 0x2032E036;
    public static uint SoraArmorSlot3 = 0x2032E038;
    public static uint SoraArmorSlot4 = 0x2032E03A;
    public static uint SoraAccessorySlotCount = 0x2032E031;
    public static uint SoraAccessorySlot1 = 0x2032E044;
    public static uint SoraAccessorySlot2 = 0x2032E046;
    public static uint SoraAccessorySlot3 = 0x2032E048;
    public static uint SoraAccessorySlot4 = 0x2032E04A;
    public static uint SoraItemSlotCount = 0x2032E032;
    public static uint SoraItemSlot1 = 0x2032E054;
    public static uint SoraItemSlot2 = 0x2032E056;
    public static uint SoraItemSlot3 = 0x2032E058;
    public static uint SoraItemSlot4 = 0x2032E05A;
    public static uint SoraItemSlot5 = 0x2032E05C;
    public static uint SoraItemSlot6 = 0x2032E05E;
    public static uint SoraItemSlot7 = 0x2032E060;
    public static uint SoraItemSlot8 = 0x2032E062;
    public static uint SoraQuickMenuSlot1 = 0x2032F228;
    public static uint SoraQuickMenuSlot2 = 0x2032F22A;
    public static uint SoraQuickMenuSlot3 = 0x2032F22C;
    public static uint SoraQuickMenuSlot4 = 0x2032F22E;
    public static uint SoraAbilityStart = 0x2032E074;

    public static uint DonaldArmorSlotCount = 0x2032E144;
    public static uint DonaldArmorSlot1 = 0x2032E148;
    public static uint DonaldArmorSlot2 = 0x2032E14A;
    public static uint DonaldAccessorySlotCount = 0x2032E145;
    public static uint DonaldAccessorySlot1 = 0x2032E158;
    public static uint DonaldAccessorySlot2 = 0x2032E15A;
    public static uint DonaldAccessorySlot3 = 0x2032E15C;
    public static uint DonaldItemSlotCount = 0x2032E146;
    public static uint DonaldItemSlot1 = 0x2032E168;
    public static uint DonaldItemSlot2 = 0x2032E16A;
    public static uint DonaldAbilityStart = 0x2032E188;

    public static uint GoofyArmorSlotCount = 0x2032E258;
    public static uint GoofyArmorSlot1 = 0x2032E25C;
    public static uint GoofyArmorSlot2 = 0x2032E25E;
    public static uint GoofyArmorSlot3 = 0x2032E260;
    public static uint GoofyAccessorySlotCount = 0x2032E259;
    public static uint GoofyAccessorySlot1 = 0x2032E26C;
    public static uint GoofyAccessorySlot2 = 0x2032E26E;
    public static uint GoofyItemSlotCount = 0x2032E25A;
    public static uint GoofyItemSlot1 = 0x2032E27C;
    public static uint GoofyItemSlot2 = 0x2032E27E;
    public static uint GoofyItemSlot3 = 0x2032E280;
    public static uint GoofyItemSlot4 = 0x2032E282;
    public static uint GoofyAbilityStart = 0x2032E29C;

    public static uint MulanArmorSlotCount = 0x2032E594;
    public static uint MulanArmorSlot1 = 0x2032E598;
    public static uint MulanAccessorySlotCount = 0x2032E595;
    public static uint MulanAccessorySlot1 = 0x2032E5A8;
    public static uint MulanItemSlotCount = 0x2032E596;
    public static uint MulanItemSlot1 = 0x2032E5B8;
    public static uint MulanItemSlot2 = 0x2032E5BA;
    public static uint MulanItemSlot3 = 0x2032E5BC;
    public static uint MulanAbilityStart = 0x2032E5D8;

    public static uint BeastAccessorySlotCount = 0x2032E8D1;
    public static uint BeastAccessorySlot1 = 0x2032E8E4;
    public static uint BeastItemSlotCount = 0x2032E8D2;
    public static uint BeastItemSlot1 = 0x2032E8F4;
    public static uint BeastItemSlot2 = 0x2032E8F6;
    public static uint BeastItemSlot3 = 0x2032E8F8;
    public static uint BeastItemSlot4 = 0x2032E8FA;
    public static uint BeastAbilityStart = 0x2032E914;

    public static uint AuronArmorSlotCount = 0x2032E480;
    public static uint AuronArmorSlot1 = 0x2032E484;
    public static uint AuronItemSlotCount = 0x2032E482;
    public static uint AuronItemSlot1 = 0x2032E4A4;
    public static uint AuronItemSlot2 = 0x2032E4A6;
    public static uint AuronAbilityStart = 0x2032E4C4;

    public static uint CaptainJackSparrowArmorSlotCount = 0x2032E7BC;
    public static uint CaptainJackSparrowArmorSlot1 = 0x2032E7C0;
    public static uint CaptainJackSparrowAccessorySlotCount = 0x2032E7BD;
    public static uint CaptainJackSparrowAccessorySlot1 = 0x2032E7D0;
    public static uint CaptainJackSparrowItemSlotCount = 0x2032E7BE;
    public static uint CaptainJackSparrowItemSlot1 = 0x2032E7E0;
    public static uint CaptainJackSparrowItemSlot2 = 0x2032E7E2;
    public static uint CaptainJackSparrowItemSlot3 = 0x2032E7E4;
    public static uint CaptainJackSparrowItemSlot4 = 0x2032E7E6;
    public static uint CaptainJackSparrowAbilityStart = 0x2032E800;

    public static uint AladdinArmorSlotCount = 0x2032E6A8;
    public static uint AladdinArmorSlot1 = 0x2032E6AC;
    public static uint AladdinArmorSlot2 = 0x2032E6AE;
    public static uint AladdinItemSlotCount = 0x2032E6AA;
    public static uint AladdinItemSlot1 = 0x2032E6CC;
    public static uint AladdinItemSlot2 = 0x2032E6CE;
    public static uint AladdinItemSlot3 = 0x2032E6D0;
    public static uint AladdinItemSlot4 = 0x2032E6D2;
    public static uint AladdinItemSlot5 = 0x2032E6D4;
    public static uint AladdinAbilityStart = 0x2032E6EC;

    public static uint JackSkellingtonAccessorySlotCount = 0x2032E9E5;
    public static uint JackSkellingtonAccessorySlot1 = 0x2032E9F8;
    public static uint JackSkellingtonAccessorySlot2 = 0x2032E9FA;
    public static uint JackSkellingtonItemSlotCount = 0x2032E9E6;
    public static uint JackSkellingtonItemSlot1 = 0x2032EA08;
    public static uint JackSkellingtonItemSlot2 = 0x2032EA0A;
    public static uint JackSkellingtonItemSlot3 = 0x2032EA0C;
    public static uint JackSkellingtonAbilityStart = 0x2032EA28;

    public static uint SimbaAccessorySlotCount = 0x2032EAF9;
    public static uint SimbaAccessorySlot1 = 0x2032EB0C;
    public static uint SimbaAccessorySlot2 = 0x2032EB0E;
    public static uint SimbaItemSlotCount = 0x2032EAFA;
    public static uint SimbaItemSlot1 = 0x2032EB1C;
    public static uint SimbaItemSlot2 = 0x2032EB1E;
    public static uint SimbaItemSlot3 = 0x2032EB20;
    public static uint SimbaAbilityStart = 0x2032EB3C;

    public static uint TronArmorSlotCount = 0x2032EC0C;
    public static uint TronArmorSlot1 = 0x2032EC10;
    public static uint TronAccessorySlotCount = 0x2032EC0D;
    public static uint TronAccessorySlot1 = 0x2032EC20;
    public static uint TronItemSlotCount = 0x2032EC0E;
    public static uint TronItemSlot1 = 0x2032EC30;
    public static uint TronItemSlot2 = 0x2032EC32;
    public static uint TronAbilityStart = 0x2032EC50;

    public static uint RikuArmorSlotCount = 0x2032ED20;
    public static uint RikuArmorSlot1 = 0x2032ED24;
    public static uint RikuArmorSlot2 = 0x2032ED26;
    public static uint RikuAccessorySlotCount = 0x2032ED21;
    public static uint RikuAccessorySlot1 = 0x2032ED34;
    public static uint RikuItemSlotCount = 0x2032ED22;
    public static uint RikuItemSlot1 = 0x2032ED44;
    public static uint RikuItemSlot2 = 0x2032ED46;
    public static uint RikuItemSlot3 = 0x2032ED48;
    public static uint RikuItemSlot4 = 0x2032ED4A;
    public static uint RikuItemSlot5 = 0x2032ED4C;
    public static uint RikuItemSlot6 = 0x2032ED4E;
    public static uint RikuAbilityStart = 0x2032ED64;
    #endregion Equipment

    #region Abilities
    public static uint SoraAbilitySlots = 0x2032E074;
    public static uint DonaldAbilitySlots = 0x2032E188;
    public static uint GoofyAbilitySlots = 0x2032E29C;
    public static uint MulanAbilitySlots = 0x2032E5D8;
    public static uint BeastAbilitySlots = 0x2032E914;
    public static uint AuronAbilitySlots = 0x2032E4C4;
    public static uint CaptainJackSparrowAbilitySlots = 0x2032E800;
    public static uint AladdinAbilitySlots = 0x2032E6EC;
    public static uint JackSkellingtonAbilitySlots = 0x2032EA28;
    public static uint SimbaAbilitySlots = 0x2032EB3C;
    public static uint TronAbilitySlots = 0x2032EC50;
    public static uint RikuAbilitySlots = 0x2032ED64;
    #endregion Abilities

    public static uint WeaponSize = 0x2036CED0;
    public static uint WeaponSizeAlt = 0x2036CECC; // TODO Is this the right one?
    public static uint JumpAmount = 0x20191C70;
}

public static class ConstantValues
{
    public static int None = 0x0;

    #region Keyblades
    public static int KingdomKey = 0x29;
    public static int Oathkeeper = 0x2A;
    public static int Oblivion = 0x2B;
    public static int DetectionSaber = 0x2C;
    public static int FrontierOfUltima = 0x2D;
    public static int StarSeeker = 0x1E0;
    public static int HiddenDragon = 0x1E1;
    public static int HerosCrest = 0x1E4;
    public static int Monochrome = 0x1E5;
    public static int FollowTheWind = 0x1E6;
    public static int CircleOfLife = 0x1E7;
    public static int PhotonDebugger = 0x1E8;
    public static int GullWing = 0x1E9;
    public static int RumblingRose = 0x1EA;
    public static int GuardianSoul = 0x1EB;
    public static int WishingLamp = 0x1EC;
    public static int DecisivePumpkin = 0x1ED;
    public static int SleepingLion = 0x1EE;
    public static int SweetMemories = 0x1EF;
    public static int MysteriousAbyss = 0x1F0;
    public static int FatalCrest = 0x1F1;
    public static int BondOfFlame = 0x1F2;
    public static int Fenrir = 0x1F3;
    public static int UltimaWeapon = 0x1F4;
    public static int TwoBecomeOne = 0x220;
    public static int WinnersProof = 0x221;
    public static ushort StruggleBat = 0x180;
    #endregion Keyblades

    #region Staffs
    public static int MagesStaff = 0x4B;
    public static int HammerStaff = 0x94;
    public static int VictoryBell = 0x95;
    public static int MeteorStaff = 0x96;
    public static int CometStaff = 0x97;
    public static int LordsBroom = 0x98;
    public static int WisdomWand = 0x99;
    public static int RisingDragon = 0x9A;
    public static int NobodyLance = 0x9B;
    public static int ShamansRelic = 0x9C;
    public static int ShamansRelicPlus = 0x258;
    public static int StaffOfDetection = 0xA1;
    public static int SaveTheQueen = 0x1E2;
    public static int SaveTheQueenPlus = 0x1F7;
    public static int Centurion = 0x221;
    public static int CenturionPlus = 0x222;
    public static int PlainMushroom = 0x223;
    public static int PlainMushroomPlus = 0x224;
    public static int PreciousMushroom = 0x225;
    public static int PreciousMushroomPlus = 0x226;
    public static int PremiumMushroom = 0227;
    #endregion Staffs

    #region Shields
    public static int KnightsShield = 0x31;
    public static int AdamantShield = 0x8B;
    public static int ChainGear = 0x8C;
    public static int OgreShield = 0x8D;
    public static int FallingStar = 0x8E;
    public static int Dreamcloud = 0x8F;
    public static int KnightDefender = 0x90;
    public static int GenjiShield = 0x91;
    public static int AkashicRecord = 0x92;
    public static int AkashicRecordPlus = 0x259;
    public static int NobodyGuard = 0x93;
    public static int DetectionShield = 0x32;
    public static int SaveTheKing = 0x1E3;
    public static int SaveTheKingPlus = 0x1F8;
    public static int FrozenPride = 0x228;
    public static int FrozenPridePlus = 0x229;
    public static int JoyousMushroom = 0x22A;
    public static int JoyousMushroomPlus = 0x22B;
    public static int MajesticMushroom = 0x22C;
    public static int MajesticMushroomPlus = 0x22D;
    public static int UltimateMushroom = 0x22E;
    #endregion Shields

    #region Equipment

    #region Armor
    public static int ElvenBandana = 0x43;
    public static int DivineBandana = 0x44;
    public static int PowerBand = 0x45;
    public static int BusterBand = 0x46;
    public static int ChampionBelt = 0x131;
    public static int ProtectBelt = 0x4E;
    public static int GaiaBelt = 0x4F;
    public static int CosmicBelt = 0x6F;
    public static int FireBangle = 0xAD;
    public static int FiraBangle = 0xAE;
    public static int FiragaBangle = 0xC5;
    public static int FiragunBangle = 0x11C;
    public static int BlizzardArmlet = 0x11E;
    public static int BlizzaraArmlet = 0x11F;
    public static int BlizzaragaArmlet = 0x120;
    public static int BlizzaragunArmlet = 0x121;
    public static int ThunderTrinket = 0x123;
    public static int ThundaraTrinket = 0x124;
    public static int ThundagaTrinket = 0x125;
    public static int ThundagunTrinket = 0x126;
    public static int ShadowAnklet = 0x127;
    public static int DarkAnklet = 0x128;
    public static int MidnightAnklet = 0x129;
    public static int ChaosAnklet = 0x12A;
    public static int AbasChain = 0x12C;
    public static int AegisChain = 0x12D;
    public static int Acrisius = 0x12E;
    public static int AcrisiusPlus = 0x133;
    public static int CosmicChain = 0x134;
    public static int ShockCharm = 0x84;
    public static int ShockCharmPlus = 0x85;
    public static int PetiteRibbon = 0x132;
    public static int Ribbon = 0x130;
    public static int GrandRibbon = 0x9D;
    #endregion Armora

    #region Accessory
    public static int AbilityRing = 0x8;
    public static int EngineersRing = 0x9;
    public static int TechiniciansRing = 0xA;
    public static int ExpertsRing = 0xB;
    public static int MastersRing = 0x22;
    public static int ExecutivesRing = 0x257;
    public static int SkillRing = 0x26;
    public static int SkillfulRing = 0x27;
    public static int CosmicRing = 0x34;
    public static int SardonyxRing = 0xC;
    public static int TourmalineRing = 0xD;
    public static int AquamarineRing = 0xE;
    public static int GarnetRing = 0xF;
    public static int DiamondRing = 0x10;
    public static int SilverRing = 0x11;
    public static int GoldRing = 0x12;
    public static int PlatinumRing = 0x13;
    public static int MythrilRing = 0x14;
    public static int OrichalcumRing = 0x1C;
    public static int SoldierEarring = 0x28;
    public static int FencerEarring = 0x2E;
    public static int MageEarring = 0x2F;
    public static int SlayerEarring = 0x30;
    public static int Medal = 0x53;
    public static int MoonAmulet = 0x23;
    public static int StarCharm = 0x24;
    public static int CosmicArts = 0x56;
    public static int ShadowArchive = 0x57;
    public static int ShadowArchivePlus = 0x58;
    public static int FullBloom = 0x40;
    public static int FullBloomPlus = 0x42;
    public static int DrawRing = 0x41;
    public static int LuckyRing = 0x3F;
    #endregion Accessory

    #region Abilities
    public static int Slapshot = 0x6;
    public static int DodgeSlash = 0x7;
    public static int SlideDash = 0x8;
    public static int GuardBreak = 0x9;
    public static int Explosion = 0xA;
    public static int FinishingLeap = 0xB;
    public static int Counterguard = 0xC;
    public static int AerialSweep = 0xD;
    public static int AerialSpiral = 0xE;
    public static int HorizontalSlash = 0xF;
    public static int AerialFinish = 0x10;
    public static int RetaliatingSlash = 0x11;
    public static int ComboMaster = 0x1B;
    public static int DamageControl = 0x1E;
    public static int FlashStep = 0x2F;
    public static int AerialDive = 0x30;
    public static int MagnetBurst = 0x31;
    public static int VicinityBreak = 0x32;
    public static int DodgeRollLv1 = 0x34;
    public static int DodgeRollLv2 = 0x35;
    public static int DodgeRollLv3 = 0x36;
    public static int DodgeRollMax = 0x37;
    public static int AutoLimit = 0x38;
    public static int Guard = 0x52;
    public static int HighJumpLv1 = 0x5E;
    public static int HighJumpLv2 = 0x5F;
    public static int HighJumpLv3 = 0x60;
    public static int HighJumpMax = 0x61;
    public static int QuickRunLv1 = 0x62;
    public static int QuickRunLv2 = 0x63;
    public static int QuickRunLv3 = 0x64;
    public static int QuickRunMax = 0x65;
    public static int AerialDodgeLv1 = 0x66;
    public static int AerialDodgeLv2 = 0x67;
    public static int AerialDodgeLv3 = 0x68;
    public static int AerialDodgeMax = 0x69;
    public static int GlideLv1 = 0x6A;
    public static int GlideLv2 = 0x6B;
    public static int GlideLv3 = 0x6C;
    public static int GlideMax = 0x6D;
    public static int AutoValor = 0x81; // TODO Find out why the toggle value is different
    public static int SecondChance = 0x81; // TODO Find out why the toggle value is different
    public static int AutoWisdom = 0x82;
    public static int AutoMaster = 0x83;
    public static int AutoFinal = 0x84;
    public static int AutoSummon = 0x85;
    public static int ComboBoost = 0x86;
    public static int AirComboBoost = 0x87;
    public static int ReactionBoost = 0x88;
    public static int FinishingPlus = 0x89; // TODO Find out why the toggle value is different
    public static int UpperSlash = 0x89; // TODO Find out why the toggle value is different
    public static int NegativeCombo = 0x8A; // TODO Find out why the toggle value is different
    public static int Scan = 0x8A; // TODO Find out why the toggle value is different
    public static int BerserkCharge = 0x8B;
    public static int DamageDrive = 0x8C;
    public static int DriveBoost = 0x8D;
    public static int FormBoost = 0x8E;
    public static int SummonBoost = 0x8F;
    public static int CombinationBoost = 0x90;
    public static int ExperienceBoost = 0x91;
    public static int LeafBracer = 0x92;
    public static int MagicLockOn = 0x93;
    public static int NoExperience = 0x94;
    public static int Draw = 0x95;
    public static int Jackpot = 0x96;
    public static int LuckyLucky = 0x97;
    public static int DriveConverter = 0x98;
    public static int FireBoost = 0x98;
    public static int BlizzardBoost = 0x99;
    public static int ThunderBoost = 0x9A;
    public static int ItemBoost = 0x9B;
    public static int MPRage = 0x9C;
    public static int MPHaste = 0x9D;
    public static int MPHastega = 0x9E; // TODO Find out why the toggle value is different
    public static int AerialRecovery = 0x9E; // TODO Find out why the toggle value is different
    public static int Defender = 0x9E; // TODO Find out why the toggle value is different
    public static int OnceMore = 0xA0;
    public static int ComboPlus = 0xA2; // TODO Find out why the toggle value is different
    public static int AutoChange = 0xA2; // TODO Find out why the toggle value is different
    public static int AirComboPlus = 0xA3; // TODO Find out why the toggle value is different
    public static int HyperHealing = 0xA3; // TODO Find out why the toggle value is different
    public static int AutoHealing = 0xA4;
    public static int MPHastera = 0xA5; // TODO Find out why the toggle value is different
    public static int DonaldFire = 0xA5; // TODO Find out why the toggle value is different
    public static int DonaldBlizzard = 0xA6;
    public static int DonaldThunder = 0xA7; // TODO Find out why the toggle value is different
    public static int GoofyTornado = 0xA7; // TODO Find out why the toggle value is different
    public static int DonaldCure = 0xA8;
    public static int GoofyTurbo = 0xA9;
    public static int SlashFrenzy = 0xAA;
    public static int Quickplay = 0xAB;
    public static int Divider = 0xAC;
    public static int GoofyBash = 0xAD;
    public static int FerociousRush = 0xAE;
    public static int BlazingFury = 0xAF;
    public static int IcyTerror = 0xB0; // TODO Find out why the toggle value is different
    public static int HealingWater = 0xB0; // TODO Find out why the toggle value is different
    public static int BoltsOfSorrow = 0xB1; // TODO Find out why the toggle value is different
    public static int FuriousShout = 0xB1; // TODO Find out why the toggle value is different
    public static int MushuFire = 0xB2;
    public static int Flametongue = 0xB3;
    public static int DarkShield = 0xB4;
    public static int Groundshaker = 0xB6; // TODO Find out why the toggle value is different
    public static int DarkAura = 0xB6; // TODO Find out why the toggle value is different
    public static int FierceClaw = 0xB7;
    public static int CurePotion = 0xBB;
    public static int ScoutingDisk = 0xBC;
    public static int HealingHerb = 0xBE; // TODO Find out why the toggle value is different
    public static int NoMercy = 0xBE; // TODO Find out why the toggle value is different
    public static int RainStorm = 0xBF;
    public static int BoneSmash = 0xC0;
    public static int TrinityLimit = 0xC6;
    public static int Fantasia = 0xC7;
    public static int FlareForce = 0xC8;
    public static int TornadoFusion = 0xC9;
    public static int TrickFantasy = 0xCB;
    public static int Overdrive = 0xCC;
    public static int HowlingMoon = 0xCD;
    public static int AplauseAplause = 0xCE;
    public static int Dragonblaze = 0xCF;
    public static int Teamwork = 0xCA;
    public static int EternalSession = 0xD0;
    public static int KingsPride = 0xD1;
    public static int TreasureIsle = 0xD2;
    public static int CompleteCompilment = 0xD3;
    public static int PulsingThunder = 0xD7;

    public static int SoraAbilityCount = 148;
    public static int DonaldAbilityCount = 34;
    public static int GoofyAbilityCount = 34;
    public static int MulanAbilityCount = 16;
    public static int BeastAbilityCount = 16;
    public static int AuronAbilityCount = 14;
    public static int CaptainJackSparrowAbilityCount = 24;
    public static int AladdinAbilityCount = 18;
    public static int JackSkellingtonAbilityCount = 22;
    public static int SimbaAbilityCount = 18;
    public static int TronAbilityCount = 18;
    public static int RikuAbilityCount = 22;
    #endregion Abilities

    #endregion Equipment

    #region Items
    public static int Potion = 0x1;
    public static int HiPotion = 0x2;
    public static int Ether = 0x3;
    public static int Elixir = 0x4;
    public static int MegaPotion = 0x5;
    public static int MegaEther = 0x6;
    public static int Megalixir = 0x7;
    #endregion Items

    #region Abilities

    #endregion Abilities

    #region Characters
    public static int Sora = 0x54;
    public static int KH1Sora = 0x6C1;
    public static int CardSora = 0x601;
    public static int DieSora = 0x602;
    public static int LionSora = 0x28A;
    public static int ChristmasSora = 0x955;
    public static int SpaceParanoidsSora = 0x656;
    public static int TimelessRiverSora = 0x955;

    public static ushort ValorFormSora = 0x55;
    public static int WisdomFormSora = 0x56;
    public static int LimitFormSora = 0x95D;
    public static int MasterFormSora = 0x57;
    public static int FinalFormSora = 0x58;
    public static int AntiFormSora = 0x59;

    public static int Roxas = 0x5A;
    public static int DualwieldRoxas = 0x323;

    public static int MickeyRobed = 0x5B;
    public static int Mickey = 0x318;

    public static ushort Donald = 0x5C;
    public static ushort BirdDonald = 0x5EF;
    public static ushort HalloweenDonald = 0x29E;
    public static ushort ChristmasDonald = 0x95B;
    public static ushort SpaceParanoidsDonald = 0x55A;
    public static ushort TimelessRiverDonald = 0x5CF;

    public static ushort Goofy = 0x5D;
    public static ushort TortoiseGoofy = 0x61B;
    public static ushort HalloweenGoofy = 0x29D;
    public static ushort ChristmasGoofy = 0x95C;
    public static ushort SpaceParanoidsGoofy = 0x554;
    public static ushort TimelessRiverGoofy = 0x4F5;

    public static int Beast = 0x5E;
    public static int Ping = 0x64;
    public static int Mulan = 0x63;
    public static int Auron = 0x65;
    public static int Aladdin = 0x62;
    public static int JackSparrow = 0x66;
    public static int HalloweenJack = 0x5F;
    public static int ChristmasJack = 0x60;
    public static int Simba = 0x61;
    public static int Tron = 0x2D4;
    public static int Hercules = 0x16A;
    public static int Minnie = 0x4BB;
    public static int Riku = 0x819;

    public static int AxelFriend = 0x4DC;
    public static int LeonFriend = 0x61C;
    public static int YuffieFriend = 0x6B0;
    public static int TifaFriend = 0x6B3;
    public static int CloudFriend = 0x688;

    public static int LeonEnemy = 0x8F8;
    public static int YuffieEnemy = 0x8FB;
    public static int TifaEnemy = 0x8FA;
    public static int CloudEnemy = 0x8F9;

    public static int Xemnas = 0x81F;
    public static int Xigbar = 0x622;
    public static int Xaldin = 0x3E5;
    public static int Vexen = 0x933;
    public static int VexenAntiSora = 0x934;
    public static int Lexaeus = 0x935;
    public static int Zexion = 0x97B;
    public static int Saix = 0x6C9;
    public static int AxelEnemy = 0x51;
    public static int Demyx = 0x31B;
    public static int DemyxWaterClone = 0x8F6;
    public static int Luxord = 0x5F8;
    public static int Marluxia = 0x923;
    public static int Larxene = 0x962;
    public static int RoxasEnemy = 0x951;
    public static int RoxasShadow = 0x754;

    public static int Sephiroth = 0x8B6;
    public static int LingeringWill = 0x96F;
    #endregion Characters

    #region Magic
    public static int Fire = 0x1;
    public static int Fira = 0x2;
    public static int Firaga = 0x3;
    public static int Blizzard = 0x1;
    public static int Blizzara = 0x2;
    public static int Blizzaga = 0x3;
    public static int Thunder = 0x1;
    public static int Thundara = 0x2;
    public static int Thundaga = 0x3;
    public static int Cure = 0x1;
    public static int Cura = 0x2;
    public static int Curaga = 0x3;
    public static int Reflect = 0x1;
    public static int Reflera = 0x2;
    public static int Reflega = 0x3;
    public static int Magnet = 0x1;
    public static int Magnera = 0x2;
    public static int Magnega = 0x3;
    #endregion Magic

    #region Speed
    public static uint SlowDownx3 = 0x40C00000;
    public static uint SlowDownx2 = 0x40500000;
    public static uint SlowDownx1 = 0x40000000;
    public static uint NormalSpeed = 0x41000000;
    public static uint SpeedUpx1 = 0x41C00000;
    public static uint SpeedUpx2 = 0x42500000;
    public static uint SpeedUpx3 = 0x42E00000;
    #endregion Speed

    #region Invulnerability
    //public static int Invulnerability_1 = 0x8C820004;
    public static int Invulnerability_2 = 0x0806891E;
    //public static int Invulnerability_3 = 0xAC820000;
    public static int Invulnerability_4 = 0x0C03F800;

    public static int InvulnerabilityFalse = 0x30E7FFFF;
    #endregion Invulnerability

    #region Quick Slot Values
    public static int PotionQuickSlotValue = 0x17;
    public static int HiPotionQuickSlotValue = 0x14;
    public static int MegaPotionQuickSlotValue = 0xF2;
    public static int EtherQuickSlotValue = 0x15;
    public static int MegaEtherQuickSlotValue = 0xF3;
    public static int ElixirQuickSlotValue = 0xF4;
    public static int MegalixirQuickSlotValue = 0xF4;
    public static int FireQuickSlotValue = 0x31;
    public static int BlizzardQuickSlotValue = 0x33;
    public static int ThunderQuickSlotValue = 0x32;
    public static int CureQuickSlotValue = 0x34;
    public static int MagnetQuickSlotValue = 0xAE;
    public static int ReflectQuickSlotValue = 0xB1;
    #endregion Quick Slot Values

    public static uint Triangle = 0xEF;
    public static uint TinyWeapon = 0x3F000000;
    public static uint NormalWeapon = 0x3F800000;
    public static uint BigWeapon = 0xC1000000;

    public static uint ReactionValor = 0x6;
    public static uint ReactionWisdom = 0x7;
    public static uint ReactionLimit = 0x2A2;
    public static uint ReactionMaster = 0xB;
    public static uint ReactionFinal = 0xC;
    public static uint ReactionAnti = 0xD;

    #endregion Constants
}
