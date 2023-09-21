using Monocle;
using System.Runtime.CompilerServices;

namespace Celeste.Mod.TASHelper.Gameplay;

public static class ConsoleCommands {
    [Command("spinner_freeze", "Quick command to set Level.TimeActive 524288 (TAS Helper)")]
    public static void CmdSpinnerFreeze(bool on = true) {
        if (Engine.Scene is Level level) {
            level.TimeActive = on ? 524288f : 0f;
        }
    }

    [Command("nearest_timeactive", "Return the nearest possible timeactive of the target time")]
    [MethodImpl(MethodImplOptions.NoOptimization)]
    public static void CmdNearestTimeAcitve(float target, float start = 0f) {
        if (target >= 524288f) {
            Celeste.Commands.Log(524288f);
            return;
        }
        float delta = 1f / 60f;
        float curr = start;
        while (curr < target) {
            curr += delta;
        }
        Celeste.Commands.Log(curr);
    }
}