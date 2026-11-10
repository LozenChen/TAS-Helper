using System.Collections;

namespace Celeste.Mod.TASHelper.Gameplay.AutoWatchEntity;

// The AutoWatch renderers read a few fields of compiler-generated coroutine state machines every frame.
// Cache a reflection delegate per field instead of doing GetFieldValue (dictionary lookup + FieldInfo.GetValue
// + boxing) on every access.
internal static class CoroutineFieldCache {
    public static int GetInt(IEnumerator enumerator, ref GetDelegate<object, int> getter, string fieldName, int fallback) {
        if (enumerator is null) {
            return fallback;
        }
        getter ??= enumerator.GetType().CreateGetDelegate<object, int>(fieldName);
        return getter is null ? fallback : getter(enumerator);
    }

    public static float GetFloat(IEnumerator enumerator, ref GetDelegate<object, float> getter, string fieldName, float fallback) {
        if (enumerator is null) {
            return fallback;
        }
        getter ??= enumerator.GetType().CreateGetDelegate<object, float>(fieldName);
        return getter is null ? fallback : getter(enumerator);
    }

    public static bool GetBool(IEnumerator enumerator, ref GetDelegate<object, bool> getter, string fieldName, bool fallback) {
        if (enumerator is null) {
            return fallback;
        }
        getter ??= enumerator.GetType().CreateGetDelegate<object, bool>(fieldName);
        return getter is null ? fallback : getter(enumerator);
    }

    public static object GetObject(IEnumerator enumerator, ref GetDelegate<object, object> getter, string fieldName, object fallback = null) {
        if (enumerator is null) {
            return fallback;
        }
        getter ??= enumerator.GetType().CreateGetDelegate<object, object>(fieldName);
        return getter is null ? fallback : getter(enumerator);
    }
}
