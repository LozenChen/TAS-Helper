using Microsoft.Xna.Framework.Input;
using System.Reflection;
using TAS.EverestInterop.Hitboxes;
using TAS.EverestInterop.InfoHUD;
using TAS.EverestInterop;
using TAS.Input.Commands;
using SRT = Celeste.Mod.SpeedrunTool.SaveLoad.SaveLoadAction;
using TH = Celeste.Mod.TASHelper.TinySRT.TH_SaveLoadAction;
using Monocle;

namespace Celeste.Mod.TASHelper.TinySRT;

public static class SlActionsAddedByMods {

    // if there's mod which add SlAction by itself, instead of by SRT, then we also add it to our SaveLoadActions

    public static readonly List<TH> Actions = new();

    [Load]
    public static void LoadTAS() {
        Actions.Add(TasModSL.Create());

        foreach (TH action in Actions) {
            TH.Add(action);
        }
    }

    [Unload]
    public static void UnloadTAS() {
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

        ConstructorInfo constructor = typeof(TH).GetConstructors()[0];
        Type delegateType = constructor.GetParameters()[0].ParameterType;

        saveLoadAction = new TH(save, load, clear, null, null);
        return saveLoadAction;
    }
}