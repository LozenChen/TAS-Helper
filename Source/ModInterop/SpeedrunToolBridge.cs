using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.ModInterop;

namespace Celeste.Mod.TASHelper.ModInterop;

// One bridge for both SpeedrunTool interop surfaces:
// - SpeedrunTool.SaveLoad (SaveLoadAction + DeepClone), initialized at depth MaxValue-5
// - SpeedrunTool.TasAction (predictor savestate slots), initialized at depth MaxValue-2
// The two have different availability timings, so they keep separate *_Installed flags and Initialize methods.
internal static class SpeedrunToolBridge {

    // SaveLoad interop available (DeepClone import resolved)
    public static bool SpeedrunToolInstalled;

    // TasAction interop available (predictor SaveState/LoadState)
    public static bool Installed;

    public const string Slot = "TasHelperPredictor";

    public static bool wasSaved;

    private static object action;

    internal static bool GetInstalledWhenLoading => ModUtils.GetModule("SpeedrunTool") is { } srt && srt.Metadata.Version >= new Version(3, 25, 0);

    [Initialize(depth: int.MaxValue - 5)]
    public static void InitializeSaveLoad() {
        typeof(SaveLoadImports).ModInterop();
        SpeedrunToolInstalled = SaveLoadImports.DeepClone is not null;
        AddSaveLoadAction();
    }

    [Initialize(depth: int.MaxValue - 2)]
    public static void InitializeTasAction() {
        typeof(TasActionImports).ModInterop();
        if (ModUtils.GetModule("SpeedrunTool") is { } srt && srt.Metadata.Version >= new Version(3, 25, 0)) {
            // this interop exists since 3.24.4, for celestetas compatibility
            Installed = TasActionImports.SaveState is not null;
        }
    }

    [Unload]
    public static void Unload() {
        RemoveSaveLoadAction();
    }

    public static bool SaveState() {
        wasSaved = true;
        return TasActionImports.SaveState(Slot);
    }

    public static bool LoadState() => TasActionImports.LoadState(Slot);

    public static void ClearState() {
        if (wasSaved) {
            TasActionImports.ClearState(Slot);
            wasSaved = false;
        }
    }

    public static bool TasIsSaved() => TasActionImports.TasIsSaved(Slot);

    private static void AddSaveLoadAction() {
        if (!SpeedrunToolInstalled) {
            return;
        }

        action = SaveLoadImports.RegisterSaveLoadAction(
            (savedValues, _) => {
                savedValues[typeof(SpeedrunToolBridge)] = DeepCloneShared(new Dictionary<string, object> {
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
                Dictionary<string, object> clonedValues = DeepCloneShared(savedValues)[typeof(SpeedrunToolBridge)];
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
                if (Engine.Scene is Level level) {
                    Gameplay.Spinner.Info.TimeActiveHelper.PredictTimeActive(level);
                }
                Gameplay.Spinner.Info.PositionHelper.OnClone();
            },
            null, null, null, null
        );
    }

    private static void RemoveSaveLoadAction() {
        if (SpeedrunToolInstalled) {
            SaveLoadImports.Unregister(action);
        }
    }

    internal static T DeepCloneShared<T>(T obj) {
        return (T)SaveLoadImports.DeepClone(obj);
    }

    [ModImportName("SpeedrunTool.SaveLoad")]
    internal static class SaveLoadImports {

        /// <summary>
        /// Register SaveLoadAction. (Please save your values into the dictionary, otherwise multi saveslots will not be supported.)
        /// </summary>
        public static Func<Action<Dictionary<Type, Dictionary<string, object>>, Level>, Action<Dictionary<Type, Dictionary<string, object>>, Level>, Action, Action<Level>, Action<Level>, Action, object> RegisterSaveLoadAction;

        public static Func<Type, string[], object> RegisterStaticTypes;

        public static Action<object> Unregister;

        public static Func<object, object> DeepClone;
    }

    [ModImportName("SpeedrunTool.TasAction")]
    internal static class TasActionImports {
        public static Func<string, bool> SaveState;
        public static Func<string, bool> LoadState;
        public static Action<string> ClearState;
        public static Func<string, bool> TasIsSaved;
    }
}
