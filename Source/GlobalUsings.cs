global using static Celeste.Mod.TASHelper.GlobalVariables;
using Celeste.Mod.TASHelper.Module;
using Monocle;
using TAS;
using TAS.EverestInterop.Hitboxes;
using TAS.Module;

namespace Celeste.Mod.TASHelper;

internal static class GlobalVariables {

    public static TASHelperSettings TasHelperSettings => TASHelperSettings.Instance;

    public static CelesteTasSettings TasSettings => CelesteTasSettings.Instance;

    public static bool DebugRendered {
        get {
            try {
                return HitboxToggle.DrawHitboxes || Engine.Commands.Open || GameplayRenderer.RenderDebug;
            }
            catch {
                // don't know why but several bugs about this have been reported
                return HitboxToggle.DrawHitboxes;
            }
        }

        private set { }
    }
    public static bool UltraFastForwarding => TAS.Manager.UltraFastForwarding;
    public static bool FrameStep => Manager.Running && (Manager.States.HasFlag(StudioCommunication.States.FrameStep) || Manager.NextStates.HasFlag(StudioCommunication.States.FrameStep));

    public static bool StrictFrameStep => Manager.Running && ((Manager.LastStates.HasFlag(StudioCommunication.States.FrameStep) ? 1 : 0) + (Manager.States.HasFlag(StudioCommunication.States.FrameStep) ? 1 : 0) + (Manager.NextStates.HasFlag(StudioCommunication.States.FrameStep) ? 1 : 0) > 1);
    // i haven't check how these states are set, but this just works well
    public static Player? player => Engine.Scene.Tracker.GetEntity<Player>();

    public static object[] parameterless = new object[] { };
}


internal static class GlobalMethod {
    public static T Apply<T>(this T obj, Action<T> action) {
        action(obj);
        return obj;
    }

}