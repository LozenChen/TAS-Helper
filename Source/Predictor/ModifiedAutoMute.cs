using Celeste.Mod.TASHelper.Utils;
using FMOD.Studio;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using TAS.EverestInterop;

namespace Celeste.Mod.TASHelper.Predictor;

public static class ModifiedAutoMute {

    private static IDetour detour;

    private static IDetour detour2;

    [Initialize]
    private static void Initialize() {
        if (!ModInterop.TasSpeedrunToolInterop.Installed) {
            return;
        }

        detour = new ILHook(typeof(AutoMute).GetGetMethod("ShouldBeMuted"), il => {
            ILCursor cursor = new ILCursor(il);
            if (cursor.TryGotoNext(ins => ins.MatchLdcR4(2f))) {
                cursor.Remove();
                cursor.Emit(OpCodes.Ldc_R4, 0f);
            }
        }, HookHelper.manualConfig);

        detour2 = new ILHook(typeof(AutoMute).GetGetMethod("FrameStep"), il => {
            ILCursor cursor = new ILCursor(il);
            cursor.Emit(OpCodes.Ldc_I4_1);
            cursor.Emit(OpCodes.Ret);
        }, HookHelper.manualConfig);
    }

    [Unload]
    private static void Unload() {
        detour?.Dispose();
        detour2?.Dispose();
    }

    public static void Apply() {
        detour?.Apply();
        detour2?.Apply();
    }

    public static void Undo() {
        detour?.Undo();
        detour2?.Undo();
    }

    internal static void OnPredictorUpdateEnd() {
        IDictionary<WeakReference<EventInstance>, int> LoopAudioInstances = typeof(AutoMute).GetFieldValue<IDictionary<WeakReference<EventInstance>, int>>("LoopAudioInstances");

        Audio.CurrentAmbienceEventInstance?.setVolume(0);

        if (LoopAudioInstances.Count > 0) {
            WeakReference<EventInstance>[] copy = LoopAudioInstances.Keys.ToArray();
            foreach (WeakReference<EventInstance> loopAudioInstance in copy) {
                if (loopAudioInstance.TryGetTarget(out EventInstance eventInstance)) {
                    if (LoopAudioInstances[loopAudioInstance] <= 0) {
                        eventInstance.setVolume(0);
                        LoopAudioInstances.Remove(loopAudioInstance);
                    }
                    else {
                        LoopAudioInstances[loopAudioInstance]--;
                    }
                }
                else {
                    LoopAudioInstances.Remove(loopAudioInstance);
                }
            }
        }
    }
}