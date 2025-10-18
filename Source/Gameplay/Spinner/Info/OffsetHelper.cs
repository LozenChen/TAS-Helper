using Monocle;

namespace Celeste.Mod.TASHelper.Gameplay.Spinner.Info;

internal static class OffsetHelper {
    private static Dictionary<Type, GetDelegate<object, float>> OffsetGetters = new();

    internal static void Add(Type type, GetDelegate<object, float> offsetGetter) {
        OffsetGetters[type] = offsetGetter;
    }
    public static float? GetOffset(Entity self) {
        // another dumb way: add a component to hold the offset and everytime we look for the component to get offset
        // this is much slower than the current one

        if (OffsetGetters.TryGetValue(self.GetType(), out GetDelegate<object, float> getter)) {
            return getter(self);
        }
        return null;
    }
}