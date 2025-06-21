using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.ModInterop;

namespace Celeste.Mod.TASHelper.ModInterop;

internal static class SpeedrunToolInterop {

    public static bool SpeedrunToolInstalled;

    private static object action;

    [Initialize(depth: int.MaxValue - 5)]
    public static void Initialize() {
        typeof(SpeedrunToolImport).ModInterop();
        SpeedrunToolInstalled = SpeedrunToolImport.DeepClone is not null;
        AddSaveLoadAction();
    }

    [Unload]
    public static void Unload() {
        RemoveSaveLoadAction();
    }

    private static void AddSaveLoadAction() {
        if (!SpeedrunToolInstalled) {
            return;
        }

        action = SpeedrunToolImport.RegisterSaveLoadAction(
            (savedValues, _) => {
                savedValues[typeof(SpeedrunToolInterop)] = DeepCloneShared(new Dictionary<string, object> {
                    { "FreezeTimerBeforeUpdate", Predictor.PredictorCore.FreezeTimerBeforeUpdate },
                    { "CachedNodes", Gameplay.MovingEntityTrack.CachedNodes },
                    { "CachedStartEnd", Gameplay.MovingEntityTrack.CachedStartEnd },
                    { "CachedCircle", Gameplay.MovingEntityTrack.CachedCircle},
                    { "SJbeatColors", Gameplay.CassetteBlockHelper.CassetteBlockVisualizer.SJbeatColors},
                    { "QMbeatColors", Gameplay.CassetteBlockHelper.CassetteBlockVisualizer.QMbeatColors},
                    { "ColorSwapTime", Gameplay.CassetteBlockHelper.CassetteBlockVisualizer.ColorSwapTime},
                    { "offsetGroup" , Gameplay.Spinner.ExactSpinnerGroup.offsetGroup},
                    { "MOA",Gameplay.MovementOvershootAssistant.MOA_Renderer.Instance },
                    { "WhenWatchedRenderers", Gameplay.AutoWatchEntity.CoreLogic.WhenWatchedRenderers }
                });
            },
            (savedValues, _) => {
                Dictionary<string, object> clonedValues = DeepCloneShared(savedValues)[typeof(SpeedrunToolInterop)];
                Predictor.PredictorCore.FreezeTimerBeforeUpdate = (float)clonedValues["FreezeTimerBeforeUpdate"];
                Gameplay.MovingEntityTrack.CachedNodes = (List<Vector2[]>)clonedValues["CachedNodes"];
                Gameplay.MovingEntityTrack.CachedStartEnd = (HashSet<Gameplay.MovingEntityTrack.StartEnd>)clonedValues["CachedStartEnd"];
                Gameplay.MovingEntityTrack.CachedCircle = (HashSet<Gameplay.MovingEntityTrack.RotateData>)clonedValues["CachedCircle"];
                Gameplay.CassetteBlockHelper.CassetteBlockVisualizer.SJbeatColors = (Dictionary<int, Color>)clonedValues["SJbeatColors"];
                Gameplay.CassetteBlockHelper.CassetteBlockVisualizer.QMbeatColors = (Dictionary<int, Color>)clonedValues["QMbeatColors"];
                Gameplay.CassetteBlockHelper.CassetteBlockVisualizer.ColorSwapTime = (Dictionary<int, List<int>>)clonedValues["ColorSwapTime"];
                Gameplay.Spinner.ExactSpinnerGroup.offsetGroup = (Dictionary<Entity, Tuple<bool, string>>)clonedValues["offsetGroup"];
                Gameplay.MovementOvershootAssistant.MOA_Renderer.Instance = (Gameplay.MovementOvershootAssistant.MOA_Renderer)clonedValues["MOA"];
                Gameplay.AutoWatchEntity.CoreLogic.WhenWatchedRenderers = (List<Gameplay.AutoWatchEntity.AutoWatchRenderer>)clonedValues["WhenWatchedRenderers"];

                Module.Menu.TH_Hotkeys.HotkeyInitialize();

                Gameplay.AutoWatchEntity.CoreLogic.EverythingOnClone();
                Gameplay.Spinner.Info.TimeActiveHelper.PredictTimeActive(Engine.Scene);
                Gameplay.Spinner.Info.PositionHelper.OnClone();
            },
            null, null, null, null
        );
    }

    private static void RemoveSaveLoadAction() {
        if (SpeedrunToolInstalled) {
            SpeedrunToolImport.Unregister(action);
        }
    }

    internal static T DeepCloneShared<T>(T obj) {
        return (T)SpeedrunToolImport.DeepClone(obj);
    }
}

[ModImportName("SpeedrunTool.SaveLoad")]
internal static class SpeedrunToolImport {

    /// <summary>
    /// Register SaveLoadAction. (Please save your values into the dictionary, otherwise multi saveslots will not be supported.)
    /// </summary>
    /// <param name="saveState"></param>
    /// <param name="loadState"></param>
    /// <param name="clearState"></param>
    /// <param name="beforeSaveState"></param>
    /// <param name="beforeLoadState"></param>
    /// <param name="preCloneEntities"></param>
    /// <returns>SaveLoadAction instance, used for unregister</returns>
    public static Func<Action<Dictionary<Type, Dictionary<string, object>>, Level>, Action<Dictionary<Type, Dictionary<string, object>>, Level>, Action, Action<Level>, Action<Level>, Action, object> RegisterSaveLoadAction;

    public static Func<Type, string[], object> RegisterStaticTypes;

    public static Action<object> Unregister;

    public static Func<object, object> DeepClone;
}