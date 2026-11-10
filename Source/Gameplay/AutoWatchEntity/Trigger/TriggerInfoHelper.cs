using System.Reflection;
using System.Reflection.Emit;

namespace Celeste.Mod.TASHelper.Gameplay.AutoWatchEntity;

internal static class TriggerInfoHelper {

    public delegate string TriggerStaticHandler(Trigger trigger, Level level);

    public delegate string TriggerDynamicPlayerlessHandler(Trigger trigger, Level level);

    public delegate string TriggerDynamicPlayerHandler(Trigger trigger, Level level, Player player);

    public static Dictionary<Type, TriggerStaticHandler> StaticInfoGetters = new Dictionary<Type, TriggerStaticHandler>();

    public static Dictionary<Type, TriggerDynamicPlayerlessHandler> DynamicInfoPlayerlessGetters = new Dictionary<Type, TriggerDynamicPlayerlessHandler>();

    public static Dictionary<Type, TriggerDynamicPlayerHandler> DynamicInfoPlayerGetters = new Dictionary<Type, TriggerDynamicPlayerHandler>();

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
        ModTriggerDynamicInfo.AddToDictionary();
    }

    // Build a strongly typed delegate that casts the (Trigger, Level, Player) delegate arguments to the
    // concrete parameter types of the getter method and calls it. This replaces the previous
    // MethodInfo.Invoke(null, new object[] { ... }) wrapper, which allocated an argument array and did full
    // reflection dispatch for every watched trigger every frame.
    private static T CreateHandler<T>(MethodInfo method, params int[] argumentMap) where T : Delegate {
        ParameterInfo[] parameters = method.GetParameters();
        Type[] delegateParameters = typeof(T).GetMethod("Invoke")!.GetParameters().Select(p => p.ParameterType).ToArray();
        DynamicMethod dynamicMethod = new(
            $"{method.DeclaringType?.Name}_{method.Name}_TASHelperHandler",
            typeof(string),
            delegateParameters,
            typeof(TriggerInfoHelper).Module,
            skipVisibility: true);
        ILGenerator il = dynamicMethod.GetILGenerator();
        for (int i = 0; i < parameters.Length; i++) {
            il.Emit(OpCodes.Ldarg, argumentMap[i]);
            if (parameters[i].ParameterType != delegateParameters[argumentMap[i]]) {
                il.Emit(OpCodes.Castclass, parameters[i].ParameterType);
            }
        }
        il.Emit(OpCodes.Call, method);
        il.Emit(OpCodes.Ret);
        return (T)dynamicMethod.CreateDelegate(typeof(T));
    }

    public static bool TryCreateStaticHandler(MethodInfo method, out TriggerStaticHandler handler) {
        if (method.GetParameters().Length == 1 && method.GetParameters()[0].ParameterType.IsSubclassOf(typeof(Trigger))) {
            handler = CreateHandler<TriggerStaticHandler>(method, 0);
            return true;
        }
        else if (method.GetParameters().Length == 2
            && method.GetParameters()[0].ParameterType.IsSubclassOf(typeof(Trigger))
            && method.GetParameters()[1].ParameterType == typeof(Level)
        ) {
            handler = CreateHandler<TriggerStaticHandler>(method, 0, 1);
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
            handler2 = CreateHandler<TriggerDynamicPlayerlessHandler>(method, 0);
        }
        else if (method.GetParameters().Length == 2
            && method.GetParameters()[0].ParameterType.IsSubclassOf(typeof(Trigger))
            && method.GetParameters()[1].ParameterType == typeof(Level)
        ) {
            handler2 = CreateHandler<TriggerDynamicPlayerlessHandler>(method, 0, 1);
        }
        else if (method.GetParameters().Length == 2
            && method.GetParameters()[0].ParameterType.IsSubclassOf(typeof(Trigger))
            && method.GetParameters()[1].ParameterType == typeof(Player)
        ) {
            handler = CreateHandler<TriggerDynamicPlayerHandler>(method, 0, 2);
        }
        else if (method.GetParameters().Length == 3
            && method.GetParameters()[0].ParameterType.IsSubclassOf(typeof(Trigger))
            && method.GetParameters()[1].ParameterType == typeof(Level)
            && method.GetParameters()[2].ParameterType == typeof(Player)
        ) {
            handler = CreateHandler<TriggerDynamicPlayerHandler>(method, 0, 1, 2);
        }
        else if (method.GetParameters().Length == 3
            && method.GetParameters()[0].ParameterType.IsSubclassOf(typeof(Trigger))
            && method.GetParameters()[1].ParameterType == typeof(Player)
            && method.GetParameters()[2].ParameterType == typeof(Level)
        ) {
            handler = CreateHandler<TriggerDynamicPlayerHandler>(method, 0, 2, 1);
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