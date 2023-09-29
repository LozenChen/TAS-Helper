using Microsoft.Xna.Framework.Input;
using System.Reflection;
using TAS.EverestInterop.Hitboxes;
using TAS.EverestInterop.InfoHUD;
using TAS.EverestInterop;
using TAS.Input.Commands;
using SRT = Celeste.Mod.SpeedrunTool.SaveLoad.SaveLoadAction;
using TH = Celeste.Mod.TASHelper.TinySRT.TH_SaveLoadAction;
using Monocle;
using Celeste.Mod.SpeedrunTool.SaveLoad;
using Celeste.Mod.TASHelper.Entities;
using TAS;

namespace Celeste.Mod.TASHelper.TinySRT;

public static class SlActionsAddedByMods {

    // if there's mod which add SlAction by itself, instead of by SRT, then we also add it to our SaveLoadActions

    public static readonly List<TH> Actions = new();

    // we want to ensure these actions are added lastly
    [Initialize]
    public static void Load() {
        Actions.Add(TasModSL.Create());
        Actions.Add(TasHelperSL.Create());

        foreach (TH action in Actions) {
            TH.Add(action);
        }
    }

    [Unload]
    public static void Unload() {
        foreach (TH action in Actions) {
            TH.Remove(action);
        }
        Actions.Clear();
    }
}

public static class Converter {
    public static TH.SlAction Convert(this SRT.SlAction action) {
        return (savedValues, level) => { action(savedValues, level); };
    }

    public static TH Convert(this SRT action) {
        return new TH(action.saveState.Convert(), action.loadState.Convert(), action.clearState, action.beforeSaveState, action.beforeLoadState, action.preCloneEntities);
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

    private static HashSet<Entity> pauseUpdaterEntities;
    private static float DashTime;
    private static bool Frozen;
    private static int TransitionFrames;
    private static float freezeTimerBeforeUpdateBeforePredictLoops;
    public static TH Create() {
        TH.SlAction save = (_, _) => {
            pauseUpdaterEntities = PauseUpdater.entities.TH_DeepCloneShared();
            DashTime = GameInfo.DashTime;
            Frozen = GameInfo.Frozen;
            TransitionFrames = GameInfo.TransitionFrames;
            freezeTimerBeforeUpdateBeforePredictLoops = Predictor.Core.FreezeTimerBeforeUpdate;
        };
        TH.SlAction load = (_, _) => {
            PauseUpdater.entities = pauseUpdaterEntities.TH_DeepCloneShared();
            GameInfo.DashTime = DashTime;
            GameInfo.Frozen = Frozen;
            GameInfo.TransitionFrames = TransitionFrames;
            Predictor.Core.FreezeTimerBeforeUpdate = freezeTimerBeforeUpdateBeforePredictLoops;
        };
        Action clear = () => {
            pauseUpdaterEntities = null;
        };
        return new TH(save, load, clear, null, null);
    }
}