//#define AttributeDebug
using System.Reflection;

namespace Celeste.Mod.TASHelper.Utils.Attributes;
internal static class AttributeUtils {
    private static readonly object[] Parameterless = { };
    internal static readonly IDictionary<Type, IEnumerable<MethodInfo>> MethodInfos = new Dictionary<Type, IEnumerable<MethodInfo>>();

#if AttributeDebug
    public static string exceptionClass = "";
    public static Dictionary<MethodInfo, Type> debugDict = new();
    public static void CollectMethods<T>() where T : Attribute {
        typeof(AttributeUtils).Assembly.GetTypesSafe().ToList().ForEach(type => type
            .GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(info => info.GetParameters().Length == 0 && info.GetCustomAttribute<T>() != null)
            .ToList().ForEach(method => debugDict[method] = type));

        if (exceptionClass.IsNullOrEmpty()) {
            MethodInfos[typeof(T)] = typeof(AttributeUtils).Assembly.GetTypesSafe().SelectMany(type => type
            .GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(info => info.GetParameters().Length == 0 && info.GetCustomAttribute<T>() != null));
            return;
        }

        MethodInfos[typeof(T)] = typeof(AttributeUtils).Assembly.GetTypesSafe().Where(type => !type.FullName.StartsWith(exceptionClass)).SelectMany(type => type
            .GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(info => info.GetParameters().Length == 0 && info.GetCustomAttribute<T>() != null));
    }

    public static void Invoke<T>() where T : Attribute {
        if (MethodInfos.TryGetValue(typeof(T), out var methodInfos)) {
            foreach (MethodInfo methodInfo in methodInfos) {
                try {
                    methodInfo.Invoke(null, Parameterless);
                }
                catch {
                    Celeste.Commands.Log($"AttributeUtils Invoke {debugDict[methodInfo]}.{methodInfo} failed");
                }
            }
        }
    }
#else
    public static void CollectMethods<T>() where T : Attribute {
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
internal class InitializeAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Method)]
internal class TasDisableRunAttribute : Attribute { }