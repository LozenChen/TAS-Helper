using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using MonoMod.RuntimeDetour;
using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using TAS.EverestInterop;

namespace Celeste.Mod.TASHelper.Utils;

// copy from Celeste TAS

#pragma warning disable CS8600
#pragma warning disable CS8601
#pragma warning disable CS8603
#pragma warning disable CS8625

internal delegate TReturn GetDelegate<in TInstance, out TReturn>(TInstance instance);
internal static class FastReflection {
    // ReSharper disable UnusedMember.Local
    private record struct DelegateKey(Type Type, string Name, Type InstanceType, Type ReturnType) {
        public readonly Type Type = Type;
        public readonly string Name = Name;
        public readonly Type InstanceType = InstanceType;
        public readonly Type ReturnType = ReturnType;
    }
    // ReSharper restore UnusedMember.Local

    private static readonly ConcurrentDictionary<DelegateKey, Delegate> CachedFieldGetDelegates = new();

    private static GetDelegate<TInstance, TReturn> CreateGetDelegateImpl<TInstance, TReturn>(Type type, string name) {
        FieldInfo field = type.GetFieldInfo(name);
        if (field == null) {
            return null;
        }

        Type returnType = typeof(TReturn);
        Type fieldType = field.FieldType;
        if (!returnType.IsAssignableFrom(fieldType)) {
            throw new InvalidCastException($"{field.Name} is of type {fieldType}, it cannot be assigned to the type {returnType}.");
        }

        var key = new DelegateKey(type, name, typeof(TInstance), typeof(TReturn));
        if (CachedFieldGetDelegates.TryGetValue(key, out var result)) {
            return (GetDelegate<TInstance, TReturn>)result;
        }

        if (field.IsConst()) {
            object value = field.GetValue(null);
            TReturn returnValue = value == null ? default : (TReturn)value;
            Func<TInstance, TReturn> func = _ => returnValue;

            GetDelegate<TInstance, TReturn> getDelegate =
                (GetDelegate<TInstance, TReturn>)func.Method.CreateDelegate(typeof(GetDelegate<TInstance, TReturn>), func.Target);
            CachedFieldGetDelegates[key] = getDelegate;
            return getDelegate;
        }

        var method = new DynamicMethod($"{field} Getter", returnType, new[] { typeof(TInstance) }, field.DeclaringType, true);
        var il = method.GetILGenerator();

        if (field.IsStatic) {
            il.Emit(OpCodes.Ldsfld, field);
        }
        else {
            il.Emit(OpCodes.Ldarg_0);
            if (field.DeclaringType.IsValueType && !typeof(TInstance).IsValueType) {
                il.Emit(OpCodes.Unbox_Any, field.DeclaringType);
            }

            il.Emit(OpCodes.Ldfld, field);
        }

        if (fieldType.IsValueType && !returnType.IsValueType) {
            il.Emit(OpCodes.Box, fieldType);
        }

        il.Emit(OpCodes.Ret);

        result = CachedFieldGetDelegates[key] = method.CreateDelegate(typeof(GetDelegate<TInstance, TReturn>));
        return (GetDelegate<TInstance, TReturn>)result;
    }

    public static GetDelegate<TInstance, TResult> CreateGetDelegate<TInstance, TResult>(this Type type, string fieldName) {
        return CreateGetDelegateImpl<TInstance, TResult>(type, fieldName);
    }

    public static GetDelegate<TInstance, TResult> CreateGetDelegate<TInstance, TResult>(string fieldName) {
        return CreateGetDelegate<TInstance, TResult>(typeof(TInstance), fieldName);
    }
}

internal static class ReflectionExtensions {
    private const BindingFlags StaticInstanceAnyVisibility =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

    private const BindingFlags InstanceAnyVisibilityDeclaredOnly =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;

    private static readonly object[] NullArgs = { null };

    // ReSharper disable UnusedMember.Local
    private record struct MemberKey(Type Type, string Name) {
        public readonly Type Type = Type;
        public readonly string Name = Name;
    }

    private record struct AllMemberKey(Type Type, BindingFlags BindingFlags) {
        public readonly Type Type = Type;
        public readonly BindingFlags BindingFlags = BindingFlags;
    }

    private record struct MethodKey(Type Type, string Name, long Types) {
        public readonly Type Type = Type;
        public readonly string Name = Name;
        public readonly long Types = Types;
    }
    private record struct ConstructorKey(Type Type, long Types) {
        public readonly Type Type = Type;
        public readonly long Types = Types;
    }
    // ReSharper restore UnusedMember.Local

    private static readonly ConcurrentDictionary<MemberKey, FieldInfo> CachedFieldInfos = new();
    private static readonly ConcurrentDictionary<MemberKey, PropertyInfo> CachedPropertyInfos = new();
    private static readonly ConcurrentDictionary<MethodKey, MethodInfo> CachedMethodInfos = new();
    private static readonly ConcurrentDictionary<MemberKey, MethodInfo> CachedGetMethodInfos = new();
    private static readonly ConcurrentDictionary<MemberKey, MethodInfo> CachedSetMethodInfos = new();
    private static readonly ConcurrentDictionary<AllMemberKey, IEnumerable<FieldInfo>> CachedAllFieldInfos = new();
    private static readonly ConcurrentDictionary<AllMemberKey, IEnumerable<PropertyInfo>> CachedAllPropertyInfos = new();
    private static readonly ConcurrentDictionary<ConstructorKey, ConstructorInfo> CachedConstructorInfos = new();

    public static FieldInfo GetFieldInfo(this Type type, string name) {
        var key = new MemberKey(type, name);
        if (CachedFieldInfos.TryGetValue(key, out var result)) {
            return result;
        }

        do {
            result = type.GetField(name, StaticInstanceAnyVisibility);
        } while (result == null && (type = type.BaseType) != null);

        return CachedFieldInfos[key] = result;
    }

    public static PropertyInfo GetPropertyInfo(this Type type, string name) {
        var key = new MemberKey(type, name);
        if (CachedPropertyInfos.TryGetValue(key, out var result)) {
            return result;
        }

        do {
            result = type.GetProperty(name, StaticInstanceAnyVisibility);
        } while (result == null && (type = type.BaseType) != null);

        return CachedPropertyInfos[key] = result;
    }

    public static MethodInfo GetMethodInfo(this Type type, string name, Type[] types = null) {
        var key = new MethodKey(type, name, types.GetCustomHashCode());
        if (CachedMethodInfos.TryGetValue(key, out MethodInfo result)) {
            return result;
        }

        do {
            MethodInfo[] methodInfos = type.GetMethods(StaticInstanceAnyVisibility);
            result = methodInfos.FirstOrDefault(info =>
                info.Name == name && types?.SequenceEqual(info.GetParameters().Select(i => i.ParameterType)) != false);
        } while (result == null && (type = type.BaseType) != null);

        return CachedMethodInfos[key] = result;
    }

    public static MethodInfo GetGetMethod(this Type type, string propertyName) {
        var key = new MemberKey(type, propertyName);
        if (CachedGetMethodInfos.TryGetValue(key, out var result)) {
            return result;
        }

        do {
            result = type.GetPropertyInfo(propertyName)?.GetGetMethod(true);
        } while (result == null && (type = type.BaseType) != null);

        return CachedGetMethodInfos[key] = result;
    }

    public static MethodInfo GetSetMethod(this Type type, string propertyName) {
        var key = new MemberKey(type, propertyName);
        if (CachedSetMethodInfos.TryGetValue(key, out var result)) {
            return result;
        }

        do {
            result = type.GetPropertyInfo(propertyName)?.GetSetMethod(true);
        } while (result == null && (type = type.BaseType) != null);

        return CachedSetMethodInfos[key] = result;
    }

    public static IEnumerable<FieldInfo> GetAllFieldInfos(this Type type, bool includeStatic = false) {
        BindingFlags bindingFlags = InstanceAnyVisibilityDeclaredOnly;
        if (includeStatic) {
            bindingFlags |= BindingFlags.Static;
        }

        var key = new AllMemberKey(type, bindingFlags);
        if (CachedAllFieldInfos.TryGetValue(key, out var result)) {
            return result;
        }

        HashSet<FieldInfo> hashSet = new();
        while (type != null && type.IsSubclassOf(typeof(object))) {
            IEnumerable<FieldInfo> fieldInfos = type.GetFields(bindingFlags);

            foreach (FieldInfo fieldInfo in fieldInfos) {
                if (hashSet.Contains(fieldInfo)) {
                    continue;
                }

                hashSet.Add(fieldInfo);
            }

            type = type.BaseType;
        }

        CachedAllFieldInfos[key] = hashSet;
        return hashSet;
    }

    public static IEnumerable<PropertyInfo> GetAllProperties(this Type type, bool includeStatic = false) {
        BindingFlags bindingFlags = InstanceAnyVisibilityDeclaredOnly;
        if (includeStatic) {
            bindingFlags |= BindingFlags.Static;
        }

        var key = new AllMemberKey(type, bindingFlags);
        if (CachedAllPropertyInfos.TryGetValue(key, out var result)) {
            return result;
        }

        HashSet<PropertyInfo> hashSet = new();
        while (type != null && type.IsSubclassOf(typeof(object))) {
            IEnumerable<PropertyInfo> properties = type.GetProperties(bindingFlags);
            foreach (PropertyInfo fieldInfo in properties) {
                if (hashSet.Contains(fieldInfo)) {
                    continue;
                }

                hashSet.Add(fieldInfo);
            }

            type = type.BaseType;
        }

        CachedAllPropertyInfos[key] = hashSet;
        return hashSet;
    }

    public static ConstructorInfo GetConstructorInfo(this Type type, params Type[] types) {
        var key = new ConstructorKey(type, types.GetCustomHashCode());
        if (CachedConstructorInfos.TryGetValue(key, out ConstructorInfo result)) {
            return result;
        }

        ConstructorInfo[] constructors = type.GetConstructors(StaticInstanceAnyVisibility);
        result = constructors.FirstOrDefault(info => types.SequenceEqual(info.GetParameters().Select(i => i.ParameterType)));
        return CachedConstructorInfos[key] = result;
    }

    public static object GetFieldValue(this object obj, string name) {
        return obj.GetType().GetFieldInfo(name)?.GetValue(obj);
    }

    public static T GetFieldValue<T>(this object obj, string name) {
        object result = obj.GetType().GetFieldInfo(name)?.GetValue(obj);
        if (result == null) {
            return default;
        }
        else {
            return (T)result;
        }
    }

    public static T GetFieldValue<T>(this Type type, string name) {
        object result = type.GetFieldInfo(name)?.GetValue(null);
        if (result == null) {
            return default;
        }
        else {
            return (T)result;
        }
    }

    public static void SetFieldValue(this object obj, string name, object value) {
        obj.GetType().GetFieldInfo(name)?.SetValue(obj, value);
    }

    public static void SetFieldValue(this Type type, string name, object value) {
        type.GetFieldInfo(name)?.SetValue(null, value);
    }

    public static T GetPropertyValue<T>(this object obj, string name) {
        object result = obj.GetType().GetPropertyInfo(name)?.GetValue(obj, null);
        if (result == null) {
            return default;
        }
        else {
            return (T)result;
        }
    }

    public static T GetPropertyValue<T>(this Type type, string name) {
        object result = type.GetPropertyInfo(name)?.GetValue(null, null);
        if (result == null) {
            return default;
        }
        else {
            return (T)result;
        }
    }

    public static void SetPropertyValue(this object obj, string name, object value) {
        if (obj.GetType().GetPropertyInfo(name) is { CanWrite: true } propertyInfo) {
            propertyInfo.SetValue(obj, value, null);
        }
    }

    public static void SetPropertyValue(this Type type, string name, object value) {
        if (type.GetPropertyInfo(name) is { CanWrite: true } propertyInfo) {
            propertyInfo.SetValue(null, value, null);
        }
    }

    private static T InvokeMethod<T>(object obj, Type type, string name, params object[] parameters) {
        parameters ??= NullArgs;
        object result = type.GetMethodInfo(name)?.Invoke(obj, parameters);
        if (result == null) {
            return default;
        }
        else {
            return (T)result;
        }
    }

    public static T InvokeMethod<T>(this object obj, string name, params object[] parameters) {
        return InvokeMethod<T>(obj, obj.GetType(), name, parameters);
    }

    public static T InvokeMethod<T>(this Type type, string name, params object[] parameters) {
        return InvokeMethod<T>(null, type, name, parameters);
    }

    public static void InvokeMethod(this object obj, string name, params object[] parameters) {
        InvokeMethod<object>(obj, obj.GetType(), name, parameters);
    }

    public static void InvokeMethod(this Type type, string name, params object[] parameters) {
        InvokeMethod<object>(null, type, name, parameters);
    }
}

internal static class HashCodeExtensions {
    public static long GetCustomHashCode<T>(this IEnumerable<T> enumerable) {
        if (enumerable == null) {
            return 0;
        }

        unchecked {
            long hash = 17;
            foreach (T item in enumerable) {
                hash = hash * -1521134295 + EqualityComparer<T>.Default.GetHashCode(item);
            }

            return hash;
        }
    }
}

internal static class TypeExtensions {
    public static bool IsSameOrSubclassOf(this Type potentialDescendant, Type potentialBase) {
        return potentialDescendant.IsSubclassOf(potentialBase) || potentialDescendant == potentialBase;
    }

    public static bool IsSameOrSubclassOf(this Type potentialDescendant, params Type[] potentialBases) {
        return potentialBases.Any(potentialDescendant.IsSameOrSubclassOf);
    }

    public static bool IsSimpleType(this Type type) {
        return type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal) || type == typeof(Vector2);
    }

    public static bool IsStructType(this Type type) {
        return type.IsValueType && !type.IsEnum && !type.IsPrimitive && !type.IsEquivalentTo(typeof(decimal));
    }

    public static bool IsConst(this FieldInfo fieldInfo) {
        return fieldInfo.IsLiteral && !fieldInfo.IsInitOnly;
    }
}


internal static class EnumerableExtensions {
    public static bool IsEmpty<T>(this IEnumerable<T> enumerable) {
        return !enumerable.Any();
    }

    public static bool IsNullOrEmpty<T>(this IEnumerable<T> enumerable) {
        return enumerable == null || !enumerable.Any();
    }

    public static bool IsNotEmpty<T>(this IEnumerable<T> enumerable) {
        return !enumerable.IsEmpty();
    }

    public static bool IsNotNullOrEmpty<T>(this IEnumerable<T> enumerable) {
        return !enumerable.IsNullOrEmpty();
    }

    public static IEnumerable<T> SkipLast<T>(this IEnumerable<T> source, int n = 1) {
        var it = source.GetEnumerator();
        bool hasRemainingItems = false;
        var cache = new Queue<T>(n + 1);

        do {
            if (hasRemainingItems = it.MoveNext()) {
                cache.Enqueue(it.Current);
                if (cache.Count > n)
                    yield return cache.Dequeue();
            }
        } while (hasRemainingItems);
    }
}

internal static class ListExtensions {
    public static T GetValueOrDefault<T>(this IList<T> list, int index, T defaultValue = default) {
        return index >= 0 && index < list.Count ? list[index] : defaultValue;
    }
}

internal static class DictionaryExtensions {
    public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue defaultValue = default) {
        return dict.TryGetValue(key, out TValue value) ? value : defaultValue;
    }
}

internal static class LevelExtensions {

    private static List<Entity> toAdd = new();

    private static List<Entity> toRemove = new();
    public static void AddImmediately(this Scene scene, Entity entity) {
        // ensure entity is added even if the regular engine update loop is interrupted, e.g. TAS stop
        // such entities may be added during gameplay instead of when load level
        toAdd.Add(entity);
    }

    public static void RemoveImmediately(this Scene scene, Entity entity) {
        toRemove.Add(entity);
    }

    [Initialize]
    private static void Initialize() {
        using (new DetourContext { After = new List<string> { "*", "CelesteTAS-EverestInterop" }, ID = "TAS Helper AddEntities Immediately" }) {
            // it involves UpdateLists, so it's really dangerous!
            typeof(Scene).GetMethod("BeforeUpdate").HookBefore(AddEntities); // still add it so that if ultra fast forwarding (so render is skipped), there's no duplicate entity
            if (!ModUtils.MotionSmoothingInstalled) {
                // https://discord.com/channels/403698615446536203/429775320720211968/1337656187801571329
                // i guess MotionSmoothing will do some actions here, if we UpdateLists then something may go wrong?
                typeof(Scene).GetMethod("BeforeRender").HookBefore(AddEntities);
            }
        }
    }

    private static void AddEntities() {
        if (toRemove.IsNotEmpty()) {
            foreach (Entity entity in toRemove) {
                Engine.Scene.Remove(entity);
            }
            toRemove.Clear();
            Engine.Scene.Entities.UpdateLists();
        }
        if (toAdd.IsNotEmpty()) {
            foreach (Entity entity in toAdd) {
                Engine.Scene.Add(entity);
            }
            toAdd.Clear();
            Engine.Scene.Entities.UpdateLists();
        }
    }

    // this should always be called in Initialize, so when any tracker instance is created, these types are already stored
    public static void AddToTracker(Type entity, bool inherited = false) {
        // if inherited, then all subclass entities of class T can be fetched using Tracker.GetEntities<T>()
        // otherwise, Tracker.GetEntities<T>() only return those entities whose type is exactly T
        if (!typeof(Entity).IsAssignableFrom(entity)) {
            return;
        }

        // avoids CA1854: two lookups when only one is needed
        if (Tracker.TrackedEntityTypes.TryGetValue(entity, out List<Type> types)) {
            if (!types.Contains(entity)) {
                Tracker.TrackedEntityTypes[entity].Add(entity);
            }
        }
        else {
            Tracker.TrackedEntityTypes.Add(entity, new List<Type>() { entity });
        }

        if (inherited) {
            foreach (Type subclass in Tracker.GetSubclasses(entity)) {
                if (subclass.IsAbstract) {
                    continue;
                }

                if (Tracker.TrackedEntityTypes.TryGetValue(subclass, out List<Type> parentOfSubclass)) {
                    if (!parentOfSubclass.Contains(entity)) {
                        parentOfSubclass.Add(entity);
                    }
                }
                else {
                    Tracker.TrackedEntityTypes.Add(subclass, new List<Type>() { entity });
                }
            }
        }

        Tracker.StoredEntityTypes.Add(entity);
    }

    public static List<Entity> SafeGetEntities<T>(this Tracker tracker) {
        if (tracker.Entities.TryGetValue(typeof(T), out List<Entity> list)) {
            return list;
        }
        AddToTracker(typeof(T));
        // our add to tracker may get lost if some other mods hot reload, leading to crashes if we use GetEntities
        // it's after initialize so we need to do more thing
        tracker.Entities.Add(typeof(T), new List<Entity>());
        return new List<Entity>();
    }

    public static List<Entity> SafeGetEntities(this Tracker tracker, Type type) {
        if (tracker.Entities.TryGetValue(type, out List<Entity> list)) {
            return list;
        }
        AddToTracker(type);
        // our add to tracker may get lost if some other mods hot reload, leading to crashes if we use GetEntities
        // it's after initialize so we need to do more thing
        tracker.Entities.Add(type, new List<Entity>());
        return new List<Entity>();
    }

    public static Vector2 ScreenToWorld(this Level level, Vector2 position) {
        Vector2 size = new Vector2(320f, 180f);
        Vector2 scaledSize = size / level.ZoomTarget;
        Vector2 offset = level.ZoomTarget != 1f ? (level.ZoomFocusPoint - scaledSize / 2f) / (size - scaledSize) * size : Vector2.Zero;
        float scale = level.Zoom * ((320f - level.ScreenPadding * 2f) / 320f);
        Vector2 paddingOffset = new Vector2(level.ScreenPadding, level.ScreenPadding * 9f / 16f);

        if (SaveData.Instance?.Assists.MirrorMode ?? false) {
            position.X = 1920f - position.X;
        }

        if (ModUtils.UpsideDown) {
            position.Y = 1080f - position.Y;
        }

        position /= 1920f / 320f;
        position -= paddingOffset;
        position = (position - offset) / scale + offset;
        position = level.Camera.ScreenToCamera(position);
        return position;
    }

    public static Vector2 WorldToScreen(this Level level, Vector2 position) {
        Vector2 size = new Vector2(320f, 180f);
        Vector2 scaledSize = size / level.ZoomTarget;
        Vector2 offset = level.ZoomTarget != 1f ? (level.ZoomFocusPoint - scaledSize / 2f) / (size - scaledSize) * size : Vector2.Zero;
        float scale = level.Zoom * ((320f - level.ScreenPadding * 2f) / 320f);
        Vector2 paddingOffset = new Vector2(level.ScreenPadding, level.ScreenPadding * 9f / 16f);

        position = level.Camera.CameraToScreen(position);
        position = (position - offset) * scale + offset;
        position += paddingOffset;
        position *= 1920f / 320f;

        if (SaveData.Instance?.Assists.MirrorMode ?? false) {
            position.X = 1920f - position.X;
        }

        if (ModUtils.UpsideDown) {
            position.Y = 1080f - position.Y;
        }

        return position;
    }

    public static Vector2 MouseToWorld(this Level level, Vector2 mousePosition) {
        float viewScale = (float)Engine.ViewWidth / Engine.Width;
        return level.ScreenToWorld(mousePosition / viewScale).Floor();
    }

}

internal static class ColorExtensions {
    public static Color SetAlpha(this Color color, float alpha) {
        float beta = (3 - alpha) * alpha * 0.5f;
        return new Color((int)((float)color.R * beta), (int)((float)color.G * beta), (int)((float)color.B * beta), (int)((float)color.A * alpha));
    }
}

// https://github.com/NoelFB/Foster/blob/main/Framework/Extensions/EnumExt.cs
internal static class EnumExtensions {
    /// <summary>
    /// Enum.Has boxes the value, where as this method does not.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool Has<TEnum>(this TEnum lhs, TEnum rhs) where TEnum : unmanaged, Enum {
        return sizeof(TEnum) switch {
            1 => (*(byte*)&lhs & *(byte*)&rhs) > 0,
            2 => (*(ushort*)&lhs & *(ushort*)&rhs) > 0,
            4 => (*(uint*)&lhs & *(uint*)&rhs) > 0,
            8 => (*(ulong*)&lhs & *(ulong*)&rhs) > 0,
            _ => throw new Exception("Size does not match a known Enum backing type."),
        };
    }
}

internal static class Vector2Extensions {
    public static string ToSimpleString(this Vector2 vector2, int decimals) {
        return $"{vector2.X.ToFormattedString(decimals)}, {vector2.Y.ToFormattedString(decimals)}";
    }

    public static string ToDynamicFormattedString(this Vector2 vector2, int decimals) {
        if (vector2 == Vector2.Zero) {
            return "0";
        }
        else if (vector2.Y == 0f) {
            return $"X = {vector2.X.ToDynamicDecimalsString(decimals)}";
        }
        else if (vector2.X == 0f) {
            return $"Y = {vector2.Y.ToDynamicDecimalsString(decimals)}";
        }
        else {
            int d1 = vector2.X.GetDecimals(decimals);
            int d2 = vector2.Y.GetDecimals(decimals);
            int d = Math.Max(d1, d2);
            return $"X = {vector2.X.ToSignedString(d)} ; Y = {vector2.Y.ToSignedString(d)}";
        }
    }
}

internal static class NumberExtensions {
    private static readonly string format = "0.".PadRight(339, '#');

    private const double eps = 1E-6;

    public static string ToDynamicDecimalsString(this float value, int decimals) {
        string sign = value switch {
            > 0 => "+",
            < 0 => "-",
            _ => ""
        };
        return sign + Math.Abs(value).ToString($"F{value.GetDecimals(decimals)}");
    }

    public static string ToSignedString(this float value, int decimals) {
        string sign = value switch {
            > 0 => "+",
            < 0 => "-",
            _ => ""
        };
        return sign + Math.Abs(value).ToFormattedString(decimals);
    }
    public static int GetDecimals(this float value, int decimals) {
        int indeedDecimals = 0;
        while (indeedDecimals < decimals) {
            if (AlmostInteger(value, indeedDecimals)) {
                break;
            }
            indeedDecimals++;
        }
        return indeedDecimals;
    }

    public static bool AlmostInteger(this float value, int decimals) {
        return Math.Abs(value - Math.Round(value, decimals)) < eps;
    }

    public static bool AlmostInteger(this double value, int decimals) {
        return Math.Abs(value - Math.Round(value, decimals)) < eps;
    }

    public static string ToFormattedString(this float value, int decimals) {
        if (decimals == -1) {
            return value.ToString(format);
        }
        else {
            return ((double)value).ToFormattedString(decimals);
        }
    }

    public static string ToFormattedString(this double value, int decimals) {
        if (decimals == -1) {
            return value.ToString(format); // unlimited precision
        }
        else {
            return value.ToString($"F{decimals}");
        }
    }
}

internal static class EntityIdExtension {
    public static string GetEntityId(this Entity entity) {
        if (entity.GetEntityData()?.ToEntityId().ToString() is { } entityID) {
            return $"{entity.GetType().Name}[{entityID}]";
        }
        else {
            return entity.GetType().Name;
        }
    }
}

internal static class SceneExtensions {
    public static Player GetPlayer(this Scene scene) => scene.Tracker.GetEntity<Player>();

    public static Level GetLevel(this Scene scene) {
        return scene switch {
            Level level => level,
            LevelLoader levelLoader => levelLoader.Level,
            _ => null
        };
    }

    public static Session GetSession(this Scene scene) {
        return scene switch {
            Level level => level.Session,
            LevelLoader levelLoader => levelLoader.session,
            LevelExit levelExit => levelExit.session,
            AreaComplete areaComplete => areaComplete.Session,
            _ => null
        };
    }
}

internal static class DrawExtensions {
    public static void MonocleDrawPoint(this Vector2 at, Color color, float scale) {
        Monocle.Draw.SpriteBatch.Draw(Monocle.Draw.Pixel.Texture.Texture_Safe, at, Monocle.Draw.Pixel.ClipRect, color, 0f, Vector2.One * 0.5f, scale, SpriteEffects.None, 0f);
    }
}