using Microsoft.Xna.Framework;
using MonoMod.ModInterop;

namespace Celeste.Mod.TASHelper.ModInterop;

public static class ModImports {
    public static bool IsPlayerInverted => GravityHelperImport.IsPlayerInverted?.Invoke() ?? false;

    public static Vector2 GetGravityAffectedVector2(this Vector2 vec) {
        if (IsPlayerInverted) {
            return new Vector2(vec.X, -vec.Y);
        }
        return vec;
    }

    public static bool IsDreamTunnelDashState(this int state) => CommunalHelperDashStates.GetDreamTunnelDashState?.Invoke() == state;

    [Initialize]
    private static void Initialize() {
        typeof(GravityHelperImport).ModInterop();
        typeof(CommunalHelperDashStates).ModInterop();
    }
}


[ModImportName("GravityHelper")]
internal static class GravityHelperImport {
    public static Func<bool> IsPlayerInverted;
}

[ModImportName("CommunalHelper.DashStates")]
public static class CommunalHelperDashStates {
    public static Func<int> GetDreamTunnelDashState;

    public static Func<int> HasDreamTunnelDash;
}