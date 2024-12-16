global using Celeste.Mod.TASHelper.Utils.Attributes;
global using Celeste.Mod.TASHelper.Utils.CommandUtils;
global using static Celeste.Mod.TASHelper.GlobalVariables;
using Celeste.Mod.TASHelper.Module;
using Celeste.Mod.TASHelper.Utils;
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

    public static bool FastForwarding => Manager.FastForwarding;
    public static bool FrameStep => Manager.Running && Manager.CurrState is Manager.State.Paused or Manager.State.FrameAdvance;
    public static bool StrictFrameStep => Manager.Running && Manager.CurrState is Manager.State.FrameAdvance;

    // i haven't check how these states are set, but this just works well
    public static Player? playerInstance => Engine.Scene.Tracker.GetEntity<Player>();

    public static readonly object[] parameterless = { };
}


internal static class GlobalMethod {
    public static T Apply<T>(this T obj, Action<T> action) {
        action(obj);
        return obj;
    }

}