using Celeste.Mod.SpeedrunTool.SaveLoad;
using Celeste.Mod.TASHelper.Gameplay;
using Celeste.Mod.TASHelper.Gameplay.AutoWatchEntity;
using Celeste.Mod.TASHelper.Gameplay.Spinner;
using Celeste.Mod.TASHelper.Module.Menu;
using Celeste.Mod.TASHelper.Predictor;
using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Monocle;
using System.Reflection;
using TAS;
using TAS.EverestInterop;
using TAS.EverestInterop.Hitboxes;
using TAS.InfoHUD;
using TAS.Input.Commands;
using TAS.Gameplay;
using SRT = Celeste.Mod.SpeedrunTool.SaveLoad.SaveLoadAction;
using TH = Celeste.Mod.TASHelper.TinySRT.TH_SaveLoadAction;

namespace Celeste.Mod.TASHelper.TinySRT;

#pragma warning disable CS8625
public static class ExtraSlActions {

    // if there's mod which add SlAction by itself, instead of by SRT, then we also add it to our SaveLoadActions

    public static readonly List<TH> TH_Actions = new();

    public static List<SRT> SRT_Actions = new();

    // we want to ensure these actions are added lastly

    [Initialize]
    public static void LoadSRT() {
        // game crash if you saved before, hot reload, and load state, (mostly crash for reason like some mod type does not exist in the tracker) so we need to clear state when game reload
        StateManager.Instance.ClearState();
        TH_StateManager.Instance.ClearStateInner();

        SRT_Actions.Add(TasHelperSL.CreateSRT());
        foreach (SRT action in SRT_Actions) {
#pragma warning disable CS0618
            SRT.Add(action);
#pragma warning restore CS0618
        }
    }

    [Initialize]
    private static void Initialize() {
        TH_Actions.Add(TasModSL.Create());
        // tas mod already adds to SRT itself
        TH_Actions.Add(TasHelperSL.Create());
        TH_Actions.Add(GravityHelperSL.Create());
        TH_Actions.Add(BGSwitchSL.Create());
        TH_Actions.Add(GhostModSL.Create());
    }

    internal static void LoadTH() {
        // this is initialized when savestate is first invoked, so that's quite late
        foreach (TH action in TH_Actions) {
            TH.Add(action);
        }
    }

    [Unload]
    public static void Unload() {
        foreach (TH action in TH_Actions) {
            TH.Remove(action);
        }
        TH_Actions.Clear();

        foreach (SRT action in SRT_Actions) {
            SRT.Remove(action);
        }
        SRT_Actions.Clear();
    }
}

internal static class TasModSL {
    private static TH saveLoadAction;

    private static Dictionary<Entity, EntityData>? savedEntityData;
    private static int groupCounter;
    private static bool simulatePauses;
    private static bool pauseOnCurrentFrame;
    private static int skipFrames;
    private static int waitingFrames;
    private static StunPauseCommand.StunPauseMode? localMode;
    private static StunPauseCommand.StunPauseMode? globalModeRuntime;
    private static HashSet<Keys>? pressKeys;
    private static (long, int)? tasStartInfo;
    private static MouseState mouseState;
    private static Dictionary<Follower, bool>? followers;
    private static bool disallowUnsafeInput;
    private static Random? auraRandom;
    private static bool betterInvincible = false;

    public static TH Create() {
        TH.SlAction save = (_, _) => {
            savedEntityData = EntityDataHelper.CachedEntityData.TH_DeepCloneShared();
            InfoWatchEntity.WatchedEntities_Save = InfoWatchEntity.WatchedEntities.TH_DeepCloneShared();
            groupCounter = CycleHitboxColor.GroupCounter;
            simulatePauses = StunPauseCommand.SimulatePauses;
            pauseOnCurrentFrame = StunPauseCommand.PauseOnCurrentFrame;
            skipFrames = StunPauseCommand.SkipFrames;
            waitingFrames = StunPauseCommand.WaitingFrames;
            localMode = StunPauseCommand.LocalMode;
            globalModeRuntime = StunPauseCommand.GlobalModeRuntime;
            pressKeys = PressCommand.PressKeys.TH_DeepCloneShared();
            tasStartInfo = MetadataCommands.TasStartInfo.TH_DeepCloneShared();
            mouseState = MouseCommand.CurrentState;
            followers = HitboxSimplified.Followers.TH_DeepCloneShared();
            disallowUnsafeInput = SafeCommand.DisallowUnsafeInput;
            auraRandom = DesyncFixer.AuraHelperSharedRandom.TH_DeepCloneShared();
            betterInvincible = Manager.Running && BetterInvincible.Invincible;
        };
        TH.SlAction load = (_, _) => {
            EntityDataHelper.CachedEntityData = savedEntityData!.TH_DeepCloneShared();
            InfoWatchEntity.WatchedEntities = InfoWatchEntity.WatchedEntities_Save.TH_DeepCloneShared();
            CycleHitboxColor.GroupCounter = groupCounter;
            StunPauseCommand.SimulatePauses = simulatePauses;
            StunPauseCommand.PauseOnCurrentFrame = pauseOnCurrentFrame;
            StunPauseCommand.SkipFrames = skipFrames;
            StunPauseCommand.WaitingFrames = waitingFrames;
            StunPauseCommand.LocalMode = localMode;
            StunPauseCommand.GlobalModeRuntime = globalModeRuntime;
            PressCommand.PressKeys.Clear();
            foreach (var keys in pressKeys!) {
                PressCommand.PressKeys.Add(keys);
            }

            MetadataCommands.TasStartInfo = tasStartInfo.TH_DeepCloneShared();
            MouseCommand.CurrentState = mouseState;
            HitboxSimplified.Followers = followers!.TH_DeepCloneShared();
            SafeCommand.DisallowUnsafeInput = disallowUnsafeInput;
            DesyncFixer.AuraHelperSharedRandom = auraRandom!.TH_DeepCloneShared();
            BetterInvincible.Invincible = Manager.Running && betterInvincible;
        };
        Action clear = () => {
            savedEntityData = null;
            pressKeys = null;
            followers = null;
            auraRandom = null;
            betterInvincible = false;
        };

        saveLoadAction = new TH(save, load, clear, null, null);
        return saveLoadAction;
    }
}

internal static class TasHelperSL {

    private static float DashTime;
    private static bool Frozen;
    private static int TransitionFrames;
    private static float TH_freezeTimerBeforeUpdateBeforePredictLoops;
    private static float SRT_freezeTimerBeforeUpdateBeforePredictLoops;
    private static List<Vector2[]> TH_CachedNodes;
    private static HashSet<Gameplay.MovingEntityTrack.StartEnd> TH_CachedStartEnd;
    private static Dictionary<Gameplay.MovingEntityTrack.RotateData, int> TH_CachedCircle;
    private static List<Vector2[]> SRT_CachedNodes;
    private static HashSet<Gameplay.MovingEntityTrack.StartEnd> SRT_CachedStartEnd;
    private static Dictionary<Gameplay.MovingEntityTrack.RotateData, int> SRT_CachedCircle;


    private static Dictionary<int, Color> TH_beatColors = new();
    private static Dictionary<int, Color> TH_QMbeatColors = new();
    private static Dictionary<int, List<int>> TH_ColorSwapTime = new();
    private static Dictionary<int, Color> SRT_beatColors = new();
    private static Dictionary<int, Color> SRT_QMbeatColors = new();
    private static Dictionary<int, List<int>> SRT_ColorSwapTime = new();

    private static Dictionary<Entity, Tuple<bool, string>> TH_offsetGroup = new();
    private static Dictionary<Entity, Tuple<bool, string>> SRT_offsetGroup = new();

    private static MovementOvershootAssistant.MOA_Renderer TH_MOA;
    private static MovementOvershootAssistant.MOA_Renderer SRT_MOA;

    private static bool SRT_BetterInvincible = false;

    private static List<AutoWatchRenderer> TH_WhenWatchedRenderers = new List<AutoWatchRenderer>();
    private static List<AutoWatchRenderer> SRT_WhenWatchedRenderers = new List<AutoWatchRenderer>();

    public static TH Create() {
        TH.SlAction save = (_, _) => {
            DashTime = GameInfo.DashTime;
            Frozen = GameInfo.Frozen;
            TransitionFrames = GameInfo.TransitionFrames;
            TH_freezeTimerBeforeUpdateBeforePredictLoops = Predictor.PredictorCore.FreezeTimerBeforeUpdate;
            TH_CachedNodes = Gameplay.MovingEntityTrack.CachedNodes.TH_DeepCloneShared();
            TH_CachedStartEnd = Gameplay.MovingEntityTrack.CachedStartEnd.TH_DeepCloneShared();
            TH_CachedCircle = Gameplay.MovingEntityTrack.CachedCircle.TH_DeepCloneShared();

            TH_beatColors = CassetteBlockHelper.CassetteBlockVisualizer.SJbeatColors.TH_DeepCloneShared();
            TH_QMbeatColors = CassetteBlockHelper.CassetteBlockVisualizer.QMbeatColors.TH_DeepCloneShared();
            TH_ColorSwapTime = CassetteBlockHelper.CassetteBlockVisualizer.ColorSwapTime.TH_DeepCloneShared();
            TH_offsetGroup = ExactSpinnerGroup.offsetGroup.TH_DeepCloneShared();
            TH_MOA = MovementOvershootAssistant.MOA_Renderer.Instance.TH_DeepCloneShared();
            TH_WhenWatchedRenderers = Gameplay.AutoWatchEntity.CoreLogic.WhenWatchedRenderers.TH_DeepCloneShared();
        };
        TH.SlAction load = (_, _) => {
            GameInfo.DashTime = DashTime;
            GameInfo.Frozen = Frozen;
            GameInfo.TransitionFrames = TransitionFrames;
            Predictor.PredictorCore.FreezeTimerBeforeUpdate = TH_freezeTimerBeforeUpdateBeforePredictLoops;
            TH_Hotkeys.HotkeyInitialize();
            Gameplay.MovingEntityTrack.CachedNodes = TH_CachedNodes.TH_DeepCloneShared();
            Gameplay.MovingEntityTrack.CachedStartEnd = TH_CachedStartEnd.TH_DeepCloneShared();
            Gameplay.MovingEntityTrack.CachedCircle = TH_CachedCircle.TH_DeepCloneShared();

            CassetteBlockHelper.CassetteBlockVisualizer.SJbeatColors = TH_beatColors.TH_DeepCloneShared();
            CassetteBlockHelper.CassetteBlockVisualizer.QMbeatColors = TH_QMbeatColors.TH_DeepCloneShared();
            CassetteBlockHelper.CassetteBlockVisualizer.ColorSwapTime = TH_ColorSwapTime.TH_DeepCloneShared();
            ExactSpinnerGroup.offsetGroup = TH_offsetGroup.TH_DeepCloneShared();
            MovementOvershootAssistant.MOA_Renderer.Instance = TH_MOA.TH_DeepCloneShared();
            Gameplay.AutoWatchEntity.CoreLogic.WhenWatchedRenderers = TH_WhenWatchedRenderers.TH_DeepCloneShared();
            AutoWatchRenderer.EverythingOnClone();
            SpinnerCalculateHelper.GroupCounter = CycleHitboxColor.GroupCounter;
        };
        Action clear = () => {
            TH_CachedNodes = null;
            TH_CachedStartEnd = null;
            TH_CachedCircle = null;
            TH_beatColors.Clear();
            TH_QMbeatColors.Clear();
            TH_ColorSwapTime.Clear();
            TH_offsetGroup.Clear();
            TH_MOA = null;
            TH_WhenWatchedRenderers.Clear();
        };
        return new TH(save, load, clear, null, null);
    }

    public static SRT CreateSRT() {
        SRT.SlAction save = (_, _) => {
            SRT_freezeTimerBeforeUpdateBeforePredictLoops = Predictor.PredictorCore.FreezeTimerBeforeUpdate;
            SRT_CachedNodes = Gameplay.MovingEntityTrack.CachedNodes.DeepCloneShared();
            SRT_CachedStartEnd = Gameplay.MovingEntityTrack.CachedStartEnd.DeepCloneShared();
            SRT_CachedCircle = Gameplay.MovingEntityTrack.CachedCircle.DeepCloneShared();

            SRT_beatColors = CassetteBlockHelper.CassetteBlockVisualizer.SJbeatColors.DeepCloneShared();
            SRT_QMbeatColors = CassetteBlockHelper.CassetteBlockVisualizer.QMbeatColors.DeepCloneShared();
            SRT_ColorSwapTime = CassetteBlockHelper.CassetteBlockVisualizer.ColorSwapTime.DeepCloneShared();

            SRT_offsetGroup = ExactSpinnerGroup.offsetGroup.DeepCloneShared();
            SRT_MOA = MovementOvershootAssistant.MOA_Renderer.Instance.DeepCloneShared();
            SRT_WhenWatchedRenderers = Gameplay.AutoWatchEntity.CoreLogic.WhenWatchedRenderers.DeepCloneShared();
        };
        SRT.SlAction load = (_, _) => {
            Predictor.PredictorCore.FreezeTimerBeforeUpdate = SRT_freezeTimerBeforeUpdateBeforePredictLoops;
            Gameplay.MovingEntityTrack.CachedNodes = SRT_CachedNodes.DeepCloneShared();
            Gameplay.MovingEntityTrack.CachedStartEnd = SRT_CachedStartEnd.DeepCloneShared();
            Gameplay.MovingEntityTrack.CachedCircle = SRT_CachedCircle.DeepCloneShared();
            TH_Hotkeys.HotkeyInitialize();
            CassetteBlockHelper.CassetteBlockVisualizer.SJbeatColors = SRT_beatColors.DeepCloneShared();
            CassetteBlockHelper.CassetteBlockVisualizer.QMbeatColors = SRT_QMbeatColors.DeepCloneShared();
            CassetteBlockHelper.CassetteBlockVisualizer.ColorSwapTime = SRT_ColorSwapTime.DeepCloneShared();
            ExactSpinnerGroup.offsetGroup = SRT_offsetGroup.DeepCloneShared();
            MovementOvershootAssistant.MOA_Renderer.Instance = SRT_MOA.DeepCloneShared();


            Gameplay.AutoWatchEntity.CoreLogic.WhenWatchedRenderers = SRT_WhenWatchedRenderers.DeepCloneShared();

            AutoWatchRenderer.EverythingOnClone();
            SpinnerCalculateHelper.GroupCounter = CycleHitboxColor.GroupCounter;

            PredictorCore.delayedClearFutures = true;
            PredictorCore.HasCachedFutures = false;
            PredictorCore.delayedClearState = true;
        };
        Action clear = () => {
            SRT_CachedNodes = null;
            SRT_CachedStartEnd = null;
            SRT_CachedCircle = null;
            SRT_beatColors = null;
            SRT_QMbeatColors = null;
            SRT_ColorSwapTime = null;
            SRT_offsetGroup = null;
            SRT_MOA = null;

            SRT_WhenWatchedRenderers = null;
        };

        ConstructorInfo constructor = typeof(SRT).GetConstructors()[0];
        Type delegateType = constructor.GetParameters()[0].ParameterType;

        return (SRT)constructor.Invoke(new object[] {
                save.Method.CreateDelegate(delegateType, save.Target),
                load.Method.CreateDelegate(delegateType, load.Target),
                clear,
                null,
                null
            }
        );
    }
}

internal static class GravityHelperSL {

    public static bool Installed = false;

    public static object PlayerGravityComponent;
    // dont know why, this become null after SL, so I have to manually clone it
    // while the original SRT doesn't have this issue
    public static TH Create() {
        Installed = ModUtils.GetType("GravityHelper", "Celeste.Mod.GravityHelper.GravityHelperModule")?.GetPropertyInfo("PlayerComponent") is not null;
        TH.SlAction save = (_, _) => {
            if (Installed) {
                PlayerGravityComponent = ModUtils.GetType("GravityHelper", "Celeste.Mod.GravityHelper.GravityHelperModule").GetPropertyValue<object>("PlayerComponent").TH_DeepCloneShared();
            }
        };
        TH.SlAction load = (_, _) => {
            if (Installed) {
                ModUtils.GetType("GravityHelper", "Celeste.Mod.GravityHelper.GravityHelperModule").SetPropertyValue("PlayerComponent", PlayerGravityComponent.TH_DeepCloneShared());
            }
        };
        Action clear = () => {
            PlayerGravityComponent = null;
        };
        return new TH(save, load, clear, null, null);
    }
}

internal static class BGSwitchSL {
    private static bool bgMode;

    private static Solid bgSolidTiles;

    private static Grid bgSolidTilesGrid;

    public static Type type;
    public static TH Create() {
        type = ModUtils.GetType("BGswitch", "Celeste.Mod.BGswitch.BGModeManager");
        TH.SlAction save = (_, _) => {
            if (type is not null) {
                bgMode = type.GetFieldValue<bool>("bgMode");
                bgSolidTiles = type.GetFieldValue<Solid>("bgSolidTiles").TH_DeepCloneShared();
                bgSolidTilesGrid = type.GetFieldValue<Grid>("bgSolidTilesGrid").TH_DeepCloneShared();
            }
        };
        TH.SlAction load = (_, _) => {
            if (type is not null) {
                type.SetFieldValue("bgMode", bgMode);
                type.SetFieldValue("bgSolidTiles", bgSolidTiles.TH_DeepCloneShared());
                type.SetFieldValue("bgSolidTilesGrid", bgSolidTilesGrid.TH_DeepCloneShared());
            }
        };
        Action clear = () => {
            bgSolidTiles = null;
            bgSolidTilesGrid = null;
        };
        return new TH(save, load, clear, null, null);
    }
}

internal static class GhostModSL {
    private static Type GhostRecorder;
    private static Type GhostCompare;
    private static Type GhostReplayer;
    private static bool Installed;
    private static long ghostTime;
    private static long lastGhostTime;
    private static long currentTime;
    private static long lastCurrentTime;
    private static Entity recorder;
    private static Entity replayer;
    public static TH Create() {
        GhostRecorder = ModUtils.GetType("GhostModForTas", "Celeste.Mod.GhostModForTas.Recorder.GhostRecorder");
        GhostCompare = ModUtils.GetType("GhostModForTas", "Celeste.Mod.GhostModForTas.Replayer.GhostCompare");
        GhostReplayer = ModUtils.GetType("GhostModForTas", "Celeste.Mod.GhostModForTas.Replayer.GhostReplayer");
        Installed = GhostRecorder is not null && GhostCompare is not null && GhostReplayer is not null;
        TH.SlAction save = (_, _) => {
            if (Installed) {
                ghostTime = GhostCompare.GetFieldValue<long>("GhostTime");
                lastGhostTime = GhostCompare.GetFieldValue<long>("LastGhostTime");
                currentTime = GhostCompare.GetFieldValue<long>("CurrentTime");
                lastCurrentTime = GhostCompare.GetFieldValue<long>("LastCurrentTime");
                recorder = GhostRecorder.GetFieldValue<Entity>("Recorder").TH_DeepCloneShared();
                replayer = GhostReplayer.GetFieldValue<Entity>("Replayer").TH_DeepCloneShared();
            }
        };
        TH.SlAction load = (_, _) => {
            if (Installed) {
                GhostCompare.SetFieldValue("GhostTime", ghostTime);
                GhostCompare.SetFieldValue("LastGhostTime", lastGhostTime);
                GhostCompare.SetFieldValue("CurrentTime", currentTime);
                GhostCompare.SetFieldValue("LastCurrentTime", lastCurrentTime);
                GhostRecorder.SetFieldValue("Recorder", recorder.TH_DeepCloneShared());
                GhostReplayer.SetFieldValue("Replayer", replayer.TH_DeepCloneShared());
            }
        };
        Action clear = () => {
            recorder = null;
            replayer = null;
        };
        return new TH(save, load, clear, null, null);
    }
}