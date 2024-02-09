using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System.Reflection;
using _Celeste = Celeste;

namespace Celeste.Mod.TASHelper.Utils;
internal static class HookHelper {
    // taken from CelesteTAS
    private static readonly List<IDetour> Hooks = new();

    public static ILHookConfig manualConfig = default;

    internal static void InitializeAtFirst() {
        manualConfig.ManualApply = true;
    }

    public static void Unload() {
        foreach (IDetour detour in Hooks) {
            detour.Dispose();
        }

        Hooks.Clear();
    }

    // check https://jatheplayer.github.io/celeste/ilhookview/ before creating a hook, to avoid conflict

    public static void OnHook(this MethodBase from, Delegate to) {
        Hooks.Add(new Hook(from, to));
    }

    public static void IlHook(this MethodBase from, ILContext.Manipulator manipulator) {
        Hooks.Add(new ILHook(from, manipulator));
    }

    public static void IlHook(this MethodBase from, Action<ILCursor, ILContext> manipulator) {
        from.IlHook(il => {
            ILCursor ilCursor = new(il);
            manipulator(ilCursor, il);
        });
    }

    public static void HookBefore<T>(this MethodBase methodInfo, Action<T> action) {
        methodInfo.IlHook((cursor, _) => {
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate(action);
        });
    }

    public static void HookBefore(this MethodBase methodInfo, Action action) {
        methodInfo.IlHook((cursor, _) => {
            cursor.EmitDelegate(action);
        });
    }

    public static void HookAfter<T>(this MethodBase methodInfo, Action<T> action) {
        methodInfo.IlHook((cursor, _) => {
            while (cursor.TryGotoNext(MoveType.AfterLabel, i => i.OpCode == OpCodes.Ret)) {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate(action);
                cursor.Index++;
            }
        });
    }

    public static void HookAfter(this MethodBase methodInfo, Action action) {
        methodInfo.IlHook((cursor, _) => {
            while (cursor.TryGotoNext(MoveType.AfterLabel, i => i.OpCode == OpCodes.Ret)) {
                cursor.EmitDelegate(action);
                cursor.Index++;
            }
        });
    }

    public static void SkipMethod(Type conditionType, string conditionMethodName, string methodName, params Type[] types) {
        foreach (Type type in types) {
            if (type?.GetMethodInfo(methodName) is { } method) {
                SkipMethod(conditionType, conditionMethodName, method);
            }
        }
    }

    public static void SkipMethod(Type conditionType, string conditionMethodName, params MethodInfo[] methodInfos) {
        foreach (MethodInfo methodInfo in methodInfos) {
            methodInfo.IlHook(il => {
                ILCursor ilCursor = new(il);
                Instruction start = ilCursor.Next;
                ilCursor.Emit(OpCodes.Call, conditionType.GetMethodInfo(conditionMethodName));
                ilCursor.Emit(OpCodes.Brfalse, start).Emit(OpCodes.Ret);
            });
        }
    }

    public static void ReturnZeroMethod(Type conditionType, string conditionMethodName, params MethodInfo[] methods) {
        foreach (MethodInfo methodInfo in methods) {
            if (methodInfo != null && !methodInfo.IsGenericMethod && methodInfo.DeclaringType?.IsGenericType != true &&
                methodInfo.ReturnType == typeof(float)) {
                methodInfo.IlHook(il => {
                    ILCursor ilCursor = new(il);
                    Instruction start = ilCursor.Next;
                    ilCursor.Emit(OpCodes.Call, conditionType.GetMethodInfo(conditionMethodName));
                    ilCursor.Emit(OpCodes.Brfalse, start).Emit(OpCodes.Ldc_R4, 0f).Emit(OpCodes.Ret);
                });
            }
        }
    }
}

internal static class EventOnHook {
    private static void ThrowException() {
        Logger.Log("TAS Helper", "EventOnHook finds a method with improper attributes!");
        throw new Exception("Invalid ParameterInfo");
    }
    public static class Scene {
        public delegate void UpdateHandler(Monocle.Scene scene);

        public static event UpdateHandler BeforeUpdate;

        public static event UpdateHandler AfterUpdate;

        [EventOnHook]
        private static void CreateOnHook() {
            On.Monocle.Scene.BeforeUpdate += OnBeforeUpdate;
            On.Monocle.Scene.AfterUpdate += OnAfterUpdate;
        }

        [Unload]
        private static void Unload() {
            On.Monocle.Scene.BeforeUpdate -= OnBeforeUpdate;
            On.Monocle.Scene.AfterUpdate -= OnAfterUpdate;
        }

        private static void OnBeforeUpdate(On.Monocle.Scene.orig_BeforeUpdate orig, Monocle.Scene self) {
            orig(self);
            BeforeUpdate?.Invoke(self);
        }

        private static void OnAfterUpdate(On.Monocle.Scene.orig_AfterUpdate orig, Monocle.Scene self) {
            orig(self);
            AfterUpdate?.Invoke(self);
        }
    }

    public static class Level {
        public delegate void LoadLevelHandler(_Celeste.Level level, Player.IntroTypes playerIntro, bool isFromLoader = false);

        public static event LoadLevelHandler LoadLevel;

        public static event LoadLevelHandler LoadLevel_Before;

        private delegate void LoadLevelHandler_Parameter0();

        private static event LoadLevelHandler_Parameter0 LoadLevel_Parameter0;

        private static event LoadLevelHandler_Parameter0 LoadLevel_Before_Parameter0;

        private delegate void LoadLevelHandler_Parameter1(_Celeste.Level level);

        private static event LoadLevelHandler_Parameter1 LoadLevel_Parameter1;

        private static event LoadLevelHandler_Parameter1 LoadLevel_Before_Parameter1;

        private delegate void LoadLevelHandler_Parameter2(_Celeste.Level level, Player.IntroTypes playerIntro);

        private static event LoadLevelHandler_Parameter2 LoadLevel_Parameter2;

        private static event LoadLevelHandler_Parameter2 LoadLevel_Before_Parameter2;

        [Initialize]
        private static void Initialize() {
            foreach (MethodInfo method in typeof(AttributeUtils).Assembly.GetTypesSafe().SelectMany(type => type
            .GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))) {
                if (method.GetCustomAttribute<LoadLevelAttribute>() is not LoadLevelAttribute attr) {
                    continue;
                }
                if (attr.Before) {
                    switch (method.GetParameters().Length) {
                        case 0: {
                                LoadLevel_Parameter0 += (LoadLevelHandler_Parameter0)method.CreateDelegate(typeof(LoadLevelHandler_Parameter0));
                                break;
                            }
                        case 1: {
                                LoadLevel_Parameter1 += (LoadLevelHandler_Parameter1)method.CreateDelegate(typeof(LoadLevelHandler_Parameter1));
                                break;
                            }
                        case 2: {
                                LoadLevel_Parameter2 += (LoadLevelHandler_Parameter2)method.CreateDelegate(typeof(LoadLevelHandler_Parameter2));
                                break;
                            }
                        case 3: {
                                LoadLevel += (LoadLevelHandler)method.CreateDelegate(typeof(LoadLevelHandler));
                                break;
                            }
                        default: {
                                ThrowException();
                                break;
                            }
                    }
                }
                else {
                    switch (method.GetParameters().Length) {
                        case 0: {
                                LoadLevel_Before_Parameter0 += (LoadLevelHandler_Parameter0)method.CreateDelegate(typeof(LoadLevelHandler_Parameter0));
                                break;
                            }
                        case 1: {
                                LoadLevel_Before_Parameter1 += (LoadLevelHandler_Parameter1)method.CreateDelegate(typeof(LoadLevelHandler_Parameter1));
                                break;
                            }
                        case 2: {
                                LoadLevel_Before_Parameter2 += (LoadLevelHandler_Parameter2)method.CreateDelegate(typeof(LoadLevelHandler_Parameter2));
                                break;
                            }
                        case 3: {
                                LoadLevel_Before += (LoadLevelHandler)method.CreateDelegate(typeof(LoadLevelHandler));
                                break;
                            }
                        default: {
                                ThrowException();
                                break;
                            }
                    }
                }
            }
            if (LoadLevel_Parameter0 is not null) {
                LoadLevel += (_, _, _) => LoadLevel_Parameter0.Invoke();
            }
            if (LoadLevel_Parameter1 is not null) {
                LoadLevel += (level, _, _) => LoadLevel_Parameter1.Invoke(level);
            }
            if (LoadLevel_Parameter2 is not null) {
                LoadLevel += (level, playerIntro, _) => LoadLevel_Parameter2.Invoke(level, playerIntro);
            }
            if (LoadLevel_Before_Parameter0 is not null) {
                LoadLevel_Before += (_, _, _) => LoadLevel_Before_Parameter0.Invoke();
            }
            if (LoadLevel_Before_Parameter1 is not null) {
                LoadLevel_Before += (level, _, _) => LoadLevel_Before_Parameter1.Invoke(level);
            }
            if (LoadLevel_Before_Parameter2 is not null) {
                LoadLevel_Before += (level, playerIntro, _) => LoadLevel_Before_Parameter2.Invoke(level, playerIntro);
            }
        }

        [EventOnHook]
        private static void CreateOnHook() {
            // yeah i know Everest.Events.Level.OnLoadLevel already exists
            // but my compiler throws me a CS0229 error
            // maybe the issue of publicizer, idk
            On.Celeste.Level.LoadLevel += OnLoadLevel;

        }

        [Unload]
        private static void Unload() {
            On.Celeste.Level.LoadLevel -= OnLoadLevel;
        }

        private static void OnLoadLevel(On.Celeste.Level.orig_LoadLevel orig, _Celeste.Level level, Player.IntroTypes playerIntro, bool isFromLoader = false) {
            LoadLevel_Before?.Invoke(level, playerIntro, isFromLoader);
            orig(level, playerIntro, isFromLoader);
            LoadLevel?.Invoke(level, playerIntro, isFromLoader);
        }
    }


    public static class EntityList {
        public delegate void DebugRenderHandler(Monocle.EntityList self, Monocle.Camera camera);

        public static event DebugRenderHandler DebugRender;

        private delegate void DebugRenderHandler_Parameter0();

        private static event DebugRenderHandler_Parameter0 DebugRender_Parameter0;

        private delegate void DebugRenderHandler_Parameter1(Monocle.EntityList self);

        private static event DebugRenderHandler_Parameter1 DebugRender_Parameter1;

        [Initialize]
        private static void Initialize() {
            foreach (MethodInfo method in typeof(AttributeUtils).Assembly.GetTypesSafe().SelectMany(type => type
            .GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)).Where(method => method.GetCustomAttribute<AddDebugRenderAttribute>() is { })) {
                switch (method.GetParameters().Length) {
                    case 0: {
                            DebugRender_Parameter0 += (DebugRenderHandler_Parameter0)method.CreateDelegate(typeof(DebugRenderHandler_Parameter0));
                            break;
                        }
                    case 1: {
                            DebugRender_Parameter1 += (DebugRenderHandler_Parameter1)method.CreateDelegate(typeof(DebugRenderHandler_Parameter1));
                            break;
                        }
                    case 2: {
                            DebugRender += (DebugRenderHandler)method.CreateDelegate(typeof(DebugRenderHandler));
                            break;
                        }
                    default: {
                            ThrowException();
                            break;
                        }
                }
            }
            if (DebugRender_Parameter0 is not null) {
                DebugRender += (_, _) => DebugRender_Parameter0.Invoke();
            }
            if (DebugRender_Parameter1 is not null) {
                DebugRender += (self, _) => DebugRender_Parameter1.Invoke(self);
            }
        }

        [EventOnHook]
        private static void CreateOnHook() {
            On.Monocle.EntityList.DebugRender += OnEntityListDebugRender;

        }

        [Unload]
        private static void Unload() {
            On.Monocle.EntityList.DebugRender -= OnEntityListDebugRender;
        }

        private static void OnEntityListDebugRender(On.Monocle.EntityList.orig_DebugRender orig, Monocle.EntityList self, Monocle.Camera camera) {
            orig(self, camera);
            DebugRender?.Invoke(self, camera);
        }
    }
}

public static class CILCodeHelper {
    public static void CILCodeLogger(this ILCursor ilCursor, int logCount = 999999, bool useCommand = true) {
        // remember, Commands.Log can only work in Initialize()
        Celeste.Commands.Log("------------------------------");
        Logger.Log(LogLevel.Debug, "TAS Helper", "---- CILCodeLogger ----");
        if (Apply) {
            if (AsShift) {
                ilCursor.Index += Position;
            }
            else {
                ilCursor.Index = Position;
            }
        }
        while (logCount > 0 && ilCursor.Next is not null) {
            string str;
            if (ilCursor.Next.Operand is ILLabel label) {
                str = $"{ilCursor.Next.Offset.ToString("x4")}, {ilCursor.Next.OpCode}, {ilCursor.Next.Operand} | {label.Target.Offset.ToString("x4")}, {label.Target.OpCode}, {label.Target.Operand}";
            }
            else if (ilCursor.Next.Operand is Instruction ins) {
                str = $"{ilCursor.Next.Offset.ToString("x4")}, {ilCursor.Next.OpCode} | {ins.Offset.ToString("x4")}, {ins.OpCode}, {ins.Operand}";
            }
            else {
                str = $"{ilCursor.Next.Offset.ToString("x4")}, {ilCursor.Next.OpCode}, {ilCursor.Next.Operand}";
            }
            Mod.Logger.Log(LogLevel.Debug, "TAS Helper", str);
            if (useCommand) {
                Celeste.Commands.Log(str);
            }
            logCount--;
            ilCursor.Index++;
        }
    }

    public static void CILCodeLogger(this MethodBase methodBase, int logCount = 999999, bool useCommand = true) {
        new ILHook(methodBase, il => {
            ILCursor cursor = new ILCursor(il);
            CILCodeLogger(cursor, logCount, useCommand);
        }).Dispose();
    }

    public static void CILCodeLoggerAtLast(this MethodBase methodBase) {
        methods.Add(methodBase);
    }

    internal static void InitializeAtLast() {
        using (DetourContext context = new DetourContext() { After = new List<string>() { "*" } }) {
            foreach (MethodBase method in methods) {
                method.CILCodeLogger();
            }
        }
        methods.Clear();
    }

    private static List<MethodBase> methods = new();

    private static int position = 0;

    public static int Position {
        get => position;
        set {
            position = value;
            Apply = true;
        }
    }

    public static bool Apply = false;

    public static bool AsShift = true;
}

