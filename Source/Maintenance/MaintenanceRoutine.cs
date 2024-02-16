//#define Maintenance

#if Maintenance
using Celeste.Mod.TASHelper.Utils;

namespace Celeste.Mod.TASHelper.Maintenance;
internal static class MaintenaceRoutine {

    private static void CheckSpeedrunTool() {
        // Tiny SRT needs "sync fork" from SpeedrunTool
    }

    [Initialize]
    private static void CheckSimplifiedTriggers() {
        // check if new triggers appear and need to be "simplified"
        List<Type> types = ModUtils.GetTypes().Where(x => x.IsSameOrSubclassOf(typeof(Trigger))).ToList();
        foreach (Type type in types) {
            Logger.Log("TAS Helper Maintenance", type.FullName);
        }
    }

    private static void CheckTasSync() {
        // run tases of the most popular maps
    }
}
#endif
