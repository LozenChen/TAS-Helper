//#define AttributeDebug
using System;
using System.Reflection;

namespace Celeste.Mod.TASHelper.Utils.Attributes;
internal static class AttributeUtils {
    private static readonly object[] Parameterless = [];
    internal static readonly IDictionary<Type, IEnumerable<MethodInfo>> MethodInfos = new Dictionary<Type, IEnumerable<MethodInfo>>();

#if AttributeDebug
    
    public static Dictionary<MethodInfo, Type> debugDict = new();
    public static void CollectMethods<T>() where T : Attribute {
        typeof(AttributeUtils).Assembly.GetTypesSafe().ToList().ForEach(type => type
            .GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(info => info.GetParameters().Length == 0 && info.GetCustomAttribute<T>() != null)
            .ToList().ForEach(method => debugDict[method] = type));

        List<MethodInfo> list = typeof(AttributeUtils).Assembly.GetTypesSafe().SelectMany(type => type
            .GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(info => info.GetParameters().Length == 0 && info.GetCustomAttribute<T>() != null && IsGoodClass(info.DeclaringType))).ToList();

        if (typeof(T) == typeof(InitializeAttribute)) {
            list = list.OrderByDescending(info => info.GetCustomAttribute<InitializeAttribute>().Depth).ToList();
        }

        bool useSelect = leftPercent != 0f || rightPercent != 1f;

        if (useSelect) {
            list = list.SelectPart(leftPercent, rightPercent);
        }

        Logger.Log(LogLevel.Verbose, "TASHelper", $"AttributeUtils.CollectMethods<{typeof(T)}>() adds:\n{string.Join("\n", list.Select(x => x.DeclaringType))}");

        MethodInfos[typeof(T)] = list;
    }


    public static string[] enabledClass = [];

    public static string[] exceptionClass = ["Celeste.Mod.TASHelper.Utils.EventOnHook+_Level", "Celeste.Mod.TASHelper.Gameplay.Spinner.SimplifiedSpinner"]; // add class which possibly have bugs to here

    private static bool IsGoodClass(Type type) {
        string name = type.FullName;
        bool success = true;
        if (enabledClass.IsNotEmpty()) {
            success = false;
            foreach (string acceptPredicate in enabledClass) {
                if (name.StartsWith(acceptPredicate)) {
                    success = true;
                    break;
                }
            }
        }
        if (success && exceptionClass.IsNotEmpty()) {
            foreach (string rejectPredicate in exceptionClass) {
                if (name.StartsWith(rejectPredicate)) {
                    success = false;
                    break;
                }
            }
        }
        if (success) {
            return true;
        }
        else {
            Logger.Log(LogLevel.Debug, "TASHelper AttributeUtils Reject", name);
            return false;
        }
    }

    // 注意: 由于 EventOnHook 类的搜索是单独写的, 因此会有略微不兼容

    private static float leftPercent = 0.0f;
    private static float rightPercent = 1.0f;
    // 当有 bug 但无报错时 (尤其是 mod 兼容性问题时), 二分法来锁定问题
    private static List<MethodInfo> SelectPart(this List<MethodInfo> list, float leftPercent, float rightPercent) {
        int count = list.Count;
        int left = (int)Math.Floor(count * leftPercent);
        int targetCount = (int)Math.Floor(count * (rightPercent - leftPercent));
        if (targetCount > count - left) {
            targetCount = count - left;
        }
        Logger.Log("TASHelper", $"Select {left} - {left + targetCount - 1}, from Total {count}");
        return list.GetRange(left, targetCount);
    }

    public static void Invoke<T>() where T : Attribute {
        if (MethodInfos.TryGetValue(typeof(T), out var methodInfos)) {
            foreach (MethodInfo methodInfo in methodInfos) {
                try {
                    methodInfo.Invoke(null, Parameterless);
                }
                catch (Exception e){
                    Logger.Log(LogLevel.Error, "TASHelper", $"AttributeUtils Invoke {debugDict[methodInfo]}.{methodInfo} failed");
                    Logger.Log(LogLevel.Error, "TASHelper", $"Inner Exception: {e}");
                }
            }
        }
    }
#else
    public static void CollectMethods<T>() where T : Attribute {
        if (typeof(T) == typeof(InitializeAttribute)) {
            MethodInfos[typeof(T)] = typeof(AttributeUtils).Assembly.GetTypesSafe().SelectMany(type => type
                .GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(info => info.GetParameters().Length == 0 && info.GetCustomAttribute<InitializeAttribute>() != null))
                .OrderByDescending(info => info.GetCustomAttribute<InitializeAttribute>().Depth);
            return;
        }
        MethodInfos[typeof(T)] = typeof(AttributeUtils).Assembly.GetTypesSafe().SelectMany(type => type
            .GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(info => info.GetParameters().Length == 0 && info.GetCustomAttribute<T>() != null));
    }

    public static void Invoke<T>() where T : Attribute {
        if (MethodInfos.TryGetValue(typeof(T), out var methodInfos)) {
            foreach (MethodInfo methodInfo in methodInfos) {
                methodInfo.Invoke(null, Parameterless);
            }
        }
    }
#endif
}


[AttributeUsage(AttributeTargets.Method)]
internal class LoadAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Method)]
internal class UnloadAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Method)]
internal class LoadContentAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Method)]
internal class InitializeAttribute : Attribute {
    public int Depth;

    public InitializeAttribute(int depth = 0) {
        Depth = depth; // depth higher = invoked earlier
    }
}

[AttributeUsage(AttributeTargets.Method)]
internal class ReloadAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Method)]
internal class EventOnHookAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Method)]
// it allows 0 - 3 parameters, so you can't collect/invoke it using attribute utils functions. we put it here just to make it global
internal class LoadLevelAttribute : Attribute {
    public bool Before;

    public LoadLevelAttribute(bool before = false) {
        Before = before;
    }
}

[AttributeUsage(AttributeTargets.Method)]
// 0 - 2 parameters
internal class AddDebugRenderAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Method)]
// 0 - 1 parameters
internal class SceneBeforeUpdateAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Method)]
// 0 - 1 parameters
internal class SceneOnUpdateAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Method)]
// 0 - 1 parameters
internal class SceneAfterUpdateAttribute : Attribute { }