using Celeste.Mod.SpeedrunTool.SaveLoad;
using Celeste.Mod.TASHelper.Entities;
using Celeste.Mod.TASHelper.Module.Menu;
using Microsoft.Xna.Framework.Input;
using Monocle;
using System.Reflection;
using TAS;
using TAS.EverestInterop;
using TAS.EverestInterop.Hitboxes;
using TAS.EverestInterop.InfoHUD;
using TAS.Input.Commands;
using SRT = Celeste.Mod.SpeedrunTool.SaveLoad.SaveLoadAction;
using TH = Celeste.Mod.TASHelper.TinySRT.TH_SaveLoadAction;


namespace Celeste.Mod.TASHelper.TinySRT;

public static class SlActionsExtended {

    // if there's mod which add SlAction by itself, instead of by SRT, then we also add it to our SaveLoadActions

    public static readonly List<TH> TH_Actions = new();

    public static List<SRT> SRT_Actions = new();

    // we want to ensure these actions are added lastly
    [Initialize]
    public static void Load() {
        TH_Actions.Add(TasModSL.Create());
        // tas mod already adds to SRT itself
        TH_Actions.Add(TasHelperSL.Create());
        SRT_Actions.Add(TasHelperSL.CreateSRT());
        foreach (TH action in TH_Actions) {
            TH.Add(action);
        }
        foreach (SRT action in SRT_Actions) {
            SRT.Add(action);
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

public static class Converter {
    public static TH.SlAction Convert(this SRT.SlAction action) {
        return (savedValues, level) => { action(savedValues, level); };
    }

    public static SRT.SlAction Convert(this TH.SlAction action) {
        return (savedValues, level) => { action(savedValues, level); };
    }

    public static TH Convert(this SRT action) {
        return new TH(action.saveState.Convert(), action.loadState.Convert(), action.clearState, action.beforeSaveState, action.beforeLoadState, action.preCloneEntities);
    }
    // only works if these action are quite simple, didn't use the corresponding DeepCloneUtils

    public static SRT Convert(this TH action) {
        return new SRT(action.saveState.Convert(), action.loadState.Convert(), action.clearState, action.beforeSaveState, action.beforeLoadState, action.preCloneEntities);
    }
}

internal static class TasModSL {
    private static TH saveLoadAction;
    private static Dictionary<Entity, EntityData> savedEntityData;
    private static int groupCounter;
    private static bool simulatePauses;
    private static bool pauseOnCurrentFrame;
    private static int skipFrames;
    private static int waitingFrames;
    private static StunPauseCommand.StunPauseMode? localMode;
    private static StunPauseCommand.StunPauseMode? globalModeRuntime;
    private static HashSet<Keys> pressKeys;
    private static long? tasStartFileTime;
    private static MouseState mouseState;
    private static Dictionary<Follower, bool> followers;

    public static TH Create() {
        TH.SlAction save = (_, _) => {
            savedEntityData = EntityDataHelper.CachedEntityData.TH_DeepCloneShared();
            InfoWatchEntity.SavedRequireWatchEntities = InfoWatchEntity.RequireWatchEntities.TH_DeepCloneShared();
            groupCounter = CycleHitboxColor.GroupCounter;
            simulatePauses = StunPauseCommand.SimulatePauses;
            pauseOnCurrentFrame = StunPauseCommand.PauseOnCurrentFrame;
            skipFrames = StunPauseCommand.SkipFrames;
            waitingFrames = StunPauseCommand.WaitingFrames;
            localMode = StunPauseCommand.LocalMode;
            globalModeRuntime = StunPauseCommand.GlobalModeRuntime;
            pressKeys = PressCommand.PressKeys.TH_DeepCloneShared();
            tasStartFileTime = MetadataCommands.TasStartFileTime;
            mouseState = MouseCommand.CurrentState;
            followers = HitboxSimplified.Followers.TH_DeepCloneShared();
        };
        TH.SlAction load = (_, _) => {
            EntityDataHelper.CachedEntityData = savedEntityData.TH_DeepCloneShared();
            InfoWatchEntity.RequireWatchEntities = InfoWatchEntity.SavedRequireWatchEntities.TH_DeepCloneShared();
            CycleHitboxColor.GroupCounter = groupCounter;
            StunPauseCommand.SimulatePauses = simulatePauses;
            StunPauseCommand.PauseOnCurrentFrame = pauseOnCurrentFrame;
            StunPauseCommand.SkipFrames = skipFrames;
            StunPauseCommand.WaitingFrames = waitingFrames;
            StunPauseCommand.LocalMode = localMode;
            StunPauseCommand.GlobalModeRuntime = globalModeRuntime;
            PressCommand.PressKeys.Clear();
            foreach (Keys keys in pressKeys) {
                PressCommand.PressKeys.Add(keys);
            }

            MetadataCommands.TasStartFileTime = tasStartFileTime;
            MouseCommand.CurrentState = mouseState;
            HitboxSimplified.Followers = followers.TH_DeepCloneShared();
        };
        Action clear = () => {
            savedEntityData = null;
            pressKeys = null;
            followers = null;
            InfoWatchEntity.SavedRequireWatchEntities.Clear();
        };

        saveLoadAction = new TH(save, load, clear, null, null);
        return saveLoadAction;
    }
}

internal static class TasHelperSL {

    private static HashSet<Entity> TH_pauseUpdaterEntities;
    private static HashSet<Entity> SRT_pauseUpdaterEntities;
    private static float DashTime;
    private static bool Frozen;
    private static int TransitionFrames;
    private static float TH_freezeTimerBeforeUpdateBeforePredictLoops;
    private static float SRT_freezeTimerBeforeUpdateBeforePredictLoops;
    public static TH Create() {
        TH.SlAction save = (_, _) => {
            TH_pauseUpdaterEntities = PauseUpdater.entities.TH_DeepCloneShared();
            DashTime = GameInfo.DashTime;
            Frozen = GameInfo.Frozen;
            TransitionFrames = GameInfo.TransitionFrames;
            TH_freezeTimerBeforeUpdateBeforePredictLoops = Predictor.Core.FreezeTimerBeforeUpdate;
        };
        TH.SlAction load = (_, _) => {
            PauseUpdater.entities = TH_pauseUpdaterEntities.TH_DeepCloneShared();
            GameInfo.DashTime = DashTime;
            GameInfo.Frozen = Frozen;
            GameInfo.TransitionFrames = TransitionFrames;
            Predictor.Core.FreezeTimerBeforeUpdate = TH_freezeTimerBeforeUpdateBeforePredictLoops;
            TH_Hotkeys.HotkeyInitialize();
        };
        Action clear = () => {
            TH_pauseUpdaterEntities = null;
        };
        return new TH(save, load, clear, null, null);
    }

    public static SRT CreateSRT() {
        SRT.SlAction save = (_, _) => {
            SRT_pauseUpdaterEntities = PauseUpdater.entities.DeepCloneShared();
            SRT_freezeTimerBeforeUpdateBeforePredictLoops = Predictor.Core.FreezeTimerBeforeUpdate;
        };
        SRT.SlAction load = (_, _) => {
            PauseUpdater.entities = SRT_pauseUpdaterEntities.DeepCloneShared();
            Predictor.Core.FreezeTimerBeforeUpdate = SRT_freezeTimerBeforeUpdateBeforePredictLoops;
            TH_Hotkeys.HotkeyInitialize();
        };
        Action clear = () => {
            SRT_pauseUpdaterEntities = null;
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
