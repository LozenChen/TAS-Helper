//#define ForMaintenance
using System.Reflection;

namespace Celeste.Mod.TASHelper.Gameplay.AutoWatchEntity;

internal static class TriggerInfoHelper {

    public delegate string TriggerStaticHandler(Trigger trigger, Level level);

    public delegate string TriggerDynamicPlayerlessHandler(Trigger trigger, Level level);

    public delegate string TriggerDynamicPlayerHandler(Trigger trigger, Level level, Player player);

    public static Dictionary<Type, TriggerStaticHandler> StaticInfoGetters = new Dictionary<Type, TriggerStaticHandler>();

    public static Dictionary<Type, TriggerDynamicPlayerlessHandler> DynamicInfoPlayerlessGetters = new Dictionary<Type, TriggerDynamicPlayerlessHandler>();

    public static Dictionary<Type, TriggerDynamicPlayerHandler> DynamicInfoPlayerGetters = new Dictionary<Type, TriggerDynamicPlayerHandler>();

    private static HashSet<string> implementedMods = new HashSet<string>() { "Celeste" };

    [Initialize]
    public static void Initialize() {
        StaticInfoGetters = new();
        DynamicInfoPlayerlessGetters = new();
        DynamicInfoPlayerGetters = new();
        foreach (MethodInfo method in typeof(TriggerStaticInfoGetter)
            .GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)) {
            if (TryCreateStaticHandler(method, out TriggerStaticHandler handler)) {
                StaticInfoGetters.Add(method.GetParameters()[0].ParameterType, handler);
            }
        }
        foreach (MethodInfo method in typeof(TriggerDynamicInfoGetter)
            .GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)) {
            TryAddDynamicHandler(method);
        }
        ModTriggerStaticInfo.AddToDictionary();
#if ForMaintenance
        Logger.Log(LogLevel.Debug, $"TASHelper/{nameof(TriggerInfoHelper)}:NotImplementedTriggers",
            string.Join("\n", Utils.ModUtils.GetTypes().Where(x => 
                x.IsSubclassOf(typeof(Trigger))
                && !StaticInfoGetters.ContainsKey(x)
                && !implementedMods.Contains(x.Assembly.GetName().Name)
            ).Select(x => x.Assembly.GetName().Name + " @ " + x.FullName)));
#endif        
    }

    public static bool TryCreateStaticHandler(MethodInfo method, out TriggerStaticHandler handler) {
        if (method.GetParameters().Length == 1 && method.GetParameters()[0].ParameterType.IsSubclassOf(typeof(Trigger))) {
            handler = (trigger, _) => {
                return (string)method.Invoke(null, new object[] { trigger });
            };
            return true;
        }
        else if (method.GetParameters().Length == 2
            && method.GetParameters()[0].ParameterType.IsSubclassOf(typeof(Trigger))
            && method.GetParameters()[1].ParameterType == typeof(Level)
        ) {
            handler = (trigger, level) => {
                return (string)method.Invoke(null, new object[] { trigger, level });
            };
            return true;
        }
        Logger.Log(LogLevel.Debug, "TASHelper", $"{nameof(TriggerInfoHelper)}.{nameof(TryCreateStaticHandler)}: unexpected parameters: {nameof(TriggerStaticInfoGetter)}/{method.Name}");
        handler = null;
        return false;
    }


    public static bool TryAddDynamicHandler(MethodInfo method) {
        TriggerDynamicPlayerHandler handler = null;
        TriggerDynamicPlayerlessHandler handler2 = null;
        if (method.GetParameters().Length == 1 && method.GetParameters()[0].ParameterType.IsSubclassOf(typeof(Trigger))) {
            handler2 = (trigger, _) => {
                return (string)method.Invoke(null, new object[] { trigger });
            };
        }
        else if (method.GetParameters().Length == 2
            && method.GetParameters()[0].ParameterType.IsSubclassOf(typeof(Trigger))
            && method.GetParameters()[1].ParameterType == typeof(Level)
        ) {
            handler2 = (trigger, level) => {
                return (string)method.Invoke(null, new object[] { trigger, level });
            };
        }
        else if (method.GetParameters().Length == 2
            && method.GetParameters()[0].ParameterType.IsSubclassOf(typeof(Trigger))
            && method.GetParameters()[1].ParameterType == typeof(Player)
        ) {
            handler = (trigger, _, player) => {
                return (string)method.Invoke(null, new object[] { trigger, player });
            };
        }
        else if (method.GetParameters().Length == 3
            && method.GetParameters()[0].ParameterType.IsSubclassOf(typeof(Trigger))
            && method.GetParameters()[1].ParameterType == typeof(Level)
            && method.GetParameters()[2].ParameterType == typeof(Player)
        ) {
            handler = (trigger, level, player) => {
                return (string)method.Invoke(null, new object[] { trigger, level, player });
            };
        }
        else if (method.GetParameters().Length == 3
            && method.GetParameters()[0].ParameterType.IsSubclassOf(typeof(Trigger))
            && method.GetParameters()[1].ParameterType == typeof(Player)
            && method.GetParameters()[2].ParameterType == typeof(Level)
        ) {
            handler = (trigger, level, player) => {
                return (string)method.Invoke(null, new object[] { trigger, player, level });
            };
        }

        if (handler is not null) {
            DynamicInfoPlayerGetters.Add(method.GetParameters()[0].ParameterType, handler);
            return true;
        }

        if (handler2 is not null) {
            DynamicInfoPlayerlessGetters.Add(method.GetParameters()[0].ParameterType, handler2);
            return true;
        }

        Logger.Log(LogLevel.Debug, "TASHelper", $"{nameof(TriggerInfoHelper)}.{nameof(TryAddDynamicHandler)}: unexpected parameters: {nameof(TriggerDynamicInfoGetter)}/{method.Name}");
        return false;
    }

    public static string GetStaticInfo(Trigger trigger) {
        if (trigger.Scene is not Level level) {
            return "";
        }
        if (StaticInfoGetters.TryGetValue(trigger.GetType(), out TriggerStaticHandler handler)) {
            return handler(trigger, level);
        }
        return "";
    }

    public static string GetDynamicInfo(Trigger trigger) {
        if (trigger.Scene is not Level level) {
            return "";
        }
        if (DynamicInfoPlayerlessGetters.TryGetValue(trigger.GetType(), out TriggerDynamicPlayerlessHandler handler)) {
            return handler(trigger, level);
        }

        if (playerInstance is not { } player || player.StateMachine.State == 18 || !trigger.CollideCheck(player)) {
            return "";
        }
        if (DynamicInfoPlayerGetters.TryGetValue(trigger.GetType(), out TriggerDynamicPlayerHandler handler2)) {
            return handler2(trigger, level, player);
        }
        return "";
    }

    public static bool HasDynamicInfo(Trigger trigger) {
        return DynamicInfoPlayerGetters.ContainsKey(trigger.GetType()) || DynamicInfoPlayerlessGetters.ContainsKey(trigger.GetType());
    }
}