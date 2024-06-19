using Celeste.Mod.TASHelper.Module;
using System.Reflection;

namespace Celeste.Mod.TASHelper.Utils;

public static class RestoreSettingsExt {
    // make TAS Helper settings work as if it's a CelesteTAS setting
    // so it will not be influenced by CelesteTAS's "Restore All Settings when TAS Stop"

    private static FieldInfo origModSettingsGetter;

    [Initialize]
    public static void Initialize() {
        origModSettingsGetter = typeof(TAS.EverestInterop.RestoreSettings).GetField("origModSettings", BindingFlags.NonPublic | BindingFlags.Static);
        typeof(TAS.Manager).GetMethod("EnableRun").IlHook((cursor, _) => {
            cursor.Index = cursor.Context.Instrs.Count - 1;
            cursor.EmitDelegate<Action>(SkipTASHelper);
        });
    }

    private static void SkipTASHelper() {
        if (TasSettings.RestoreSettings) {
            Dictionary<EverestModule, object> dict = (Dictionary<EverestModule, object>)origModSettingsGetter.GetValue(null);
            dict?.Remove(TASHelperModule.Instance);
        }
    }
}