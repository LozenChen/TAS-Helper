using Celeste.Mod.TASHelper.ModInterop;
using Monocle;
using System.Runtime.CompilerServices;

namespace Celeste.Mod.TASHelper.Gameplay.Spinner.Info;


internal static class TimeActiveHelper {
    public static float TimeActive = 0f;

    public static float[] PredictLoadTimeActive = new float[10];
    public static float[] PredictUnloadTimeActive = new float[100];

    internal static int GroupCounter = 0;

    [LevelUpdate(before: true)]
    internal static void CalculateBeforeUpdate(Level self) {
        if (!TasHelperSettings.Enabled || WillFastForward) {
            return;
        }

        PredictTimeActive(self);
    }


    // JIT optimization may cause PredictLoadTimeActive[2] != 524288f when TimeActive = 524288f
    [MethodImpl(MethodImplOptions.NoOptimization)]
    internal static void PredictTimeActive(Level self) {
        GroupCounter = CelesteTasImports.GetGroupCounter();
        float time = TimeActive = self.TimeActive;
        for (int i = 0; i <= 9; i++) {
            PredictLoadTimeActive[i] = PredictUnloadTimeActive[i] = time;
            time += Engine.DeltaTime;
        }
        for (int i = 10; i <= 99; i++) {
            PredictUnloadTimeActive[i] = time;
            time += Engine.DeltaTime;
        }
        // this must be before tas mod's FreeCameraHitbox.SubHudRendererOnBeforeRender, otherwise spinners will flash if you zoom out in center camera mode
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool OnInterval(float TimeActive, float interval, float offset, float DeltaTime) {
        return Math.Floor(((double)TimeActive - offset - DeltaTime) / interval) < Math.Floor(((double)TimeActive - offset) / interval);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool OnInterval(float TimeActive, float interval, float offset) {
        // this function should match https://github.com/EverestAPI/Everest/commit/fa7fc64f74a904eaf4d56f508d25561dde26597f
        return Math.Floor(((double)TimeActive - offset - Engine.DeltaTime) / interval) < Math.Floor(((double)TimeActive - offset) / interval);
    }


    public static int PredictCountdown(float offset, bool isDust, bool isLoad) {
        float interval = isDust || isLoad ? 0.05f : 0.25f;
        if (isLoad) {
            for (int i = 0; i < 9; i++) {
                if (OnInterval(PredictLoadTimeActive[i], interval, offset)) return i;
            }
            return 9;
        }
        else {
            for (int i = 0; i < 99; i++) {
                if (OnInterval(PredictUnloadTimeActive[i], interval, offset)) return i;
            }
            return 99;
        }
    }

    public static int CalculateSpinnerGroup(float offset) {
        if (OnInterval(PredictLoadTimeActive[0], 0.05f, offset)) {
            return GroupCounter;
        }
        if (OnInterval(PredictLoadTimeActive[1], 0.05f, offset)) {
            return (1 + GroupCounter) % 3;
        }
        if (OnInterval(PredictLoadTimeActive[2], 0.05f, offset)) {
            return (2 + GroupCounter) % 3;
        }
        return 3;
    }
}