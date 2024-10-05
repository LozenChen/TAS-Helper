//#define Maintenance

#if Maintenance
using Celeste.Mod.TASHelper.Gameplay.AutoWatchEntity;
using Celeste.Mod.TASHelper.Utils;
using Monocle;

namespace Celeste.Mod.TASHelper.Maintenance;
internal static class MaintenanceRoutine {

    [Initialize]

    private static void Initialize() {
        Logger.Log(LogLevel.Debug, "TAS Helper Maintenance", "This should be commented out when release!");

        CheckTriggerInfoHelper();
    }


    private static void CheckTriggerInfoHelper() {
        int count = 0;
        Logger.Log(LogLevel.Debug, $"TASHelper/{nameof(TriggerInfoHelper)}:NotImplementedTriggers",
            "As follows:\n" + string.Join("\n", Utils.ModUtils.GetTypes().Where(x =>
                x.IsSubclassOf(typeof(Trigger))
                && !TriggerInfoHelper.StaticInfoGetters.ContainsKey(x)
                && !TriggerInfoHelper.implementedMods.Contains(x.Assembly.GetName().Name)
            ).Select(x => { count++; return $"{count}). {x.Assembly.GetName().Name} @ {x.FullName}"; })));
    }

    private static void CheckSimplifiedTriggers() {
        // check if new triggers appear and need to be "simplified"
        List<Type> types = ModUtils.GetTypes().Where(x => x.IsSameOrSubclassOf(typeof(Trigger))).ToList();
        foreach (Type type in types) {
            Logger.Log("TAS Helper Maintenance", type.FullName);
        }
    }

    private static void CheckSpeedrunTool() {
        // Tiny SRT needs "sync fork" from SpeedrunTool
        // also, if a mod support SRT on its own, we need to create a corresponding support in TASHelper
    }

    private static void CheckTasSync() {
        // run tases of the most popular maps
    }

    private static void CheckSpinners() {
        // check if there are new custom spinners
        Type spinner = typeof(CrystalStaticSpinner);
        foreach (KeyValuePair<Type, List<Type>> pair in Tracker.TrackedEntityTypes) {
            if (pair.Value.Contains(spinner)) {
                Logger.Log("TAS Helper", pair.Key.FullName);
            }
        }
    }
}
#endif
