using Celeste.Mod.SpeedrunTool.Extensions;
using Celeste.Mod.TASHelper.Utils;
using Monocle;
using TAS;
using TAS.Input;

namespace Celeste.Mod.TASHelper.Predictor;

public static class TasFileWatcher {

    public static void Initialize() {
        typeof(InputController).GetMethod("Clear").HookAfter(StopWatchers);
        typeof(InputController).GetMethod("ParseFileEnd", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).HookAfter(CreateWatcher);
    }

    public static FileSystemWatcher? watcher = null;
    public static string FilePath;

    public static void StopWatchers() {
        watcher?.Dispose();
        watcher = null;
    }
    public static void CreateWatcher() {
        string filePath = InputController.StudioTasFilePath;
        if (FilePath == filePath && TasFileWatcher.watcher is not null) {
            return;
        }
        TasFileWatcher.watcher?.Dispose();

        FileSystemWatcher watcher;
        if (filePath.IsNullOrEmpty() || !Manager.Controller.UsedFiles.ContainsKey(filePath)) {
            return;
        }

        watcher = new FileSystemWatcher();
        watcher.Path = Path.GetDirectoryName(filePath);
        watcher.Filter = Path.GetFileName(filePath);

        watcher.Changed += OnTasFileChanged;

        try {
            watcher.EnableRaisingEvents = true;
        }
        catch (Exception e) {
            watcher.Dispose();
            return;
        }

        TasFileWatcher.watcher = watcher;
        FilePath = filePath;
    }

    private static void OnTasFileChanged(object sender, FileSystemEventArgs e) {
        if (TasHelperSettings.PredictOnFileChange && TasHelperSettings.PredictFutureEnabled && FrameStep && Engine.Scene is Level) {
            Core.hasDelayedPredict = true;
        }
    }
}
