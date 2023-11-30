using Celeste.Mod.SpeedrunTool.SaveLoad;
using Celeste.Mod.TASHelper.Module.Menu;
using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework;
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

public static class ExtraSlActions {

    // if there's mod which add SlAction by itself, instead of by SRT, then we also add it to our SaveLoadActions

    public static readonly List<TH> TH_Actions = new();

    public static List<SRT> SRT_Actions = new();

    // we want to ensure these actions are added lastly

    [Initialize]
    public static void LoadSRT() {
        SRT_Actions.Add(TasHelperSL.CreateSRT());
        foreach (SRT action in SRT_Actions) {
            SRT.Add(action);
        }
    }

    public static void LoadTH() {
        TH_Actions.Add(TasModSL.Create());
        // tas mod already adds to SRT itself
        TH_Actions.Add(TasHelperSL.Create());
        TH_Actions.Add(GravityHelperSL.Create());
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

    private static Dictionary<Entity, Vector2> TH_LastPositions = new();
    private static Dictionary<Entity, bool> TH_LastCollidables = new();
    public static TH Create() {
        TH.SlAction save = (_, _) => {
            DashTime = GameInfo.DashTime;
            Frozen = GameInfo.Frozen;
            TransitionFrames = GameInfo.TransitionFrames;
            TH_freezeTimerBeforeUpdateBeforePredictLoops = Predictor.Core.FreezeTimerBeforeUpdate;
            TH_CachedNodes = Gameplay.MovingEntityTrack.CachedNodes.TH_DeepCloneShared();
            TH_CachedStartEnd = Gameplay.MovingEntityTrack.CachedStartEnd.TH_DeepCloneShared();
            TH_CachedCircle = Gameplay.MovingEntityTrack.CachedCircle.TH_DeepCloneShared();
            TH_LastPositions = ActualEntityCollideHitbox.LastPositions.TH_DeepCloneShared();
            TH_LastCollidables = ActualEntityCollideHitbox.LastColldables.TH_DeepCloneShared();
        };
        TH.SlAction load = (_, _) => {
            GameInfo.DashTime = DashTime;
            GameInfo.Frozen = Frozen;
            GameInfo.TransitionFrames = TransitionFrames;
            Predictor.Core.FreezeTimerBeforeUpdate = TH_freezeTimerBeforeUpdateBeforePredictLoops;
            TH_Hotkeys.HotkeyInitialize();
            Gameplay.MovingEntityTrack.CachedNodes = TH_CachedNodes.TH_DeepCloneShared();
            Gameplay.MovingEntityTrack.CachedStartEnd = TH_CachedStartEnd.TH_DeepCloneShared();
            Gameplay.MovingEntityTrack.CachedCircle = TH_CachedCircle.TH_DeepCloneShared();
            Dictionary<Entity, Vector2> lastPos = TH_LastPositions.TH_DeepCloneShared();
            Dictionary<Entity, bool> lastCollide = TH_LastCollidables.TH_DeepCloneShared();
            ActualEntityCollideHitbox.LastPositions.Clear();
            ActualEntityCollideHitbox.LastColldables.Clear();
            foreach (Entity key in lastPos.Keys) {
                ActualEntityCollideHitbox.LastPositions[key] = lastPos[key]; // AECH.LastPositions is readonly... so it has to work like this
            }
            foreach (Entity key in lastCollide.Keys) {
                ActualEntityCollideHitbox.LastColldables[key] = lastCollide[key];
            }
        };
        Action clear = () => {
            TH_CachedNodes = null;
            TH_CachedStartEnd = null;
            TH_CachedCircle = null;
            TH_LastPositions.Clear();
            TH_LastCollidables.Clear();
        };
        return new TH(save, load, clear, null, null);
    }

    public static SRT CreateSRT() {
        SRT.SlAction save = (_, _) => {
            SRT_freezeTimerBeforeUpdateBeforePredictLoops = Predictor.Core.FreezeTimerBeforeUpdate;
            SRT_CachedNodes = Gameplay.MovingEntityTrack.CachedNodes.DeepCloneShared();
            SRT_CachedStartEnd = Gameplay.MovingEntityTrack.CachedStartEnd.DeepCloneShared();
            SRT_CachedCircle = Gameplay.MovingEntityTrack.CachedCircle.DeepCloneShared();
        };
        SRT.SlAction load = (_, _) => {
            Predictor.Core.FreezeTimerBeforeUpdate = SRT_freezeTimerBeforeUpdateBeforePredictLoops;
            Gameplay.MovingEntityTrack.CachedNodes = SRT_CachedNodes.DeepCloneShared();
            Gameplay.MovingEntityTrack.CachedStartEnd = SRT_CachedStartEnd.DeepCloneShared();
            Gameplay.MovingEntityTrack.CachedCircle = SRT_CachedCircle.DeepCloneShared();
            TH_Hotkeys.HotkeyInitialize();
        };
        Action clear = () => {
            SRT_CachedNodes = null;
            SRT_CachedStartEnd = null;
            SRT_CachedCircle = null;
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