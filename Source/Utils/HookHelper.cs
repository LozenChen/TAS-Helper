using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System.Reflection;
using _Celeste = Celeste;

namespace Celeste.Mod.TASHelper.Utils;

internal static class DetourContextHelper {
    // Hook ordering has three traps in MonoMod v25 that are silent at runtime:
    //  1. Before/After match the other detour's DetourConfig.Id EXACTLY. There is no wildcard "*"
    //     and no assembly-name fallback any more (both existed in MonoMod 21.4.29 / Everest's
    //     legacy layer only), so a wrong id means "no constraint" and order falls back to
    //     priority / creation order.
    //  2. The other mod's id is its MOD name, e.g. CelesteTAS' hook uses DetourConfig("CelesteTAS"),
    //     NOT its assembly name "CelesteTAS-EverestInterop".
    //  3. Before/After describe the APPLICATION order: my managed hook / IL manipulator is ordered
    //     before / after the listed hooks. For managed hooks that is also the runtime execution
    //     order; for IL hooks the runtime order of the injected code additionally depends on the
    //     insertion position (a same-cursor prefix insert reverses it).
    // Express the intent with the factories below instead of passing raw Before/After lists: they
    // validate the ids, so the silent-failure cases above become loud ArgumentExceptions.
    //
    //   managed:  using (DetourContextHelper.ManagedHook("TAS Helper X")
    //                          .AppliedBefore(DetourContextHelper.CelesteTAS).Use()) {
    //                 On.Celeste.Foo.Bar += ...;
    //             }
    //   IL hook:  DetourConfig config = DetourContextHelper.ILHook("TAS Helper X")
    //                          .AppliedAfter(DetourContextHelper.CelesteTAS).ToConfig();
    public const string CelesteTAS = "CelesteTAS";

    // Ids of TASHelper's own hooks referenced by other ordering configurations.
    // Must stay in sync with the ids used where those hooks are created.
    internal static class HookIds {
        internal const string Hotkeys = "TAS Helper Hotkeys";
    }

    internal static HookOrdering ManagedHook(string id) => new(id);
    internal static HookOrdering ILHook(string id) => new(id);

    internal sealed class HookOrdering {
        private readonly string id;
        private int? priority = null;
        private readonly List<string> appliedBefore = new();
        private readonly List<string> appliedAfter = new();

        internal HookOrdering(string id) {
            this.id = string.IsNullOrWhiteSpace(id)
                ? throw new ArgumentException("A hook id is required", nameof(id))
                : id;
        }

        internal HookOrdering AppliedBefore(params string[] otherHookIds) {
            appliedBefore.AddRange(Validate(otherHookIds, nameof(otherHookIds)));
            return this;
        }

        internal HookOrdering AppliedAfter(params string[] otherHookIds) {
            appliedAfter.AddRange(Validate(otherHookIds, nameof(otherHookIds)));
            return this;
        }

        internal HookOrdering WithPriority(int value) {
            priority = value;
            return this;
        }

        internal IDisposable Use() => new DetourConfigContext(ToConfig()).Use();

        internal DetourConfig ToConfig() => new(id, priority, appliedBefore, appliedAfter);

        private static string[] Validate(string[] otherHookIds, string parameterName) {
            if (otherHookIds is null || otherHookIds.Length == 0) {
                throw new ArgumentException("At least one other hook id is required", parameterName);
            }
            foreach (string otherHookId in otherHookIds) {
                if (string.IsNullOrWhiteSpace(otherHookId)) {
                    throw new ArgumentException("Hook ids must not be null or empty", parameterName);
                }
                if (otherHookId.Contains('*')) {
                    throw new ArgumentException($"Wildcard '{otherHookId}' is not supported by MonoMod v25; list the concrete DetourConfig ids instead", parameterName);
                }
                if (otherHookId == "CelesteTAS-EverestInterop") {
                    throw new ArgumentException("CelesteTAS' assembly name is not a hook id; use DetourContextHelper.CelesteTAS (\"CelesteTAS\")", parameterName);
                }
            }
            return otherHookIds;
        }
    }
}
internal static class HookHelper {
    // taken from CelesteTAS

    private static readonly List<ILHook> ILHooks = new();

    // DEBUG: IL hooks whose manipulator did not change the instruction stream (pattern not found),
    // which usually means another mod (or a game/CelesteTAS update) invalidated the anchor.
    internal static readonly List<string> DebugHookFailures = new();

    // Wrap a manipulator registered through the legacy "IL.X += ..." events so that, in DEBUG,
    // a manipulator which ends up not changing the instruction stream is reported by Loader.
    // In RELEASE the original delegate is returned unchanged, so "IL.X -= ..." keeps working.
    public static ILContext.Manipulator Checked(ILContext.Manipulator manipulator) {
#if DEBUG
        return il => {
            int before = il.Body.Instructions.Count;
            manipulator(il);
            if (il.Body.Instructions.Count == before) {
                DebugHookFailures.Add(il.Method?.FullName ?? "(unknown method)");
            }
        };
#else
        return manipulator;
#endif
    }

    private static void RunManipulator(MethodBase method, ILContext.Manipulator manipulator, ILContext il) {
#if DEBUG
        int before = il.Body.Instructions.Count;
        manipulator(il);
        if (il.Body.Instructions.Count == before) {
            DebugHookFailures.Add($"{method.DeclaringType?.FullName}::{method.Name}");
        }
#else
        manipulator(il);
#endif
    }

    public static void Unload() {
        foreach (ILHook il in ILHooks) {
            il.Dispose();
        }

        ILHooks.Clear();
    }

    // check https://jatheplayer.github.io/celeste/ilhookview/ before creating a hook, to avoid conflict
    // 完全限定名: https://learn.microsoft.com/zh-cn/dotnet/framework/reflection-and-codedom/specifying-fully-qualified-type-names

    public static void ILHook(this MethodBase from, ILContext.Manipulator manipulator) {
        ILHooks.Add(new ILHook(from, il => RunManipulator(from, manipulator, il)));
    }

    public static void ILHook(this MethodBase from, Action<ILCursor, ILContext> manipulator) {
        from.ILHook(il => {
            ILCursor ilCursor = new(il);
            manipulator(ilCursor, il);
        });
    }

    public static void HookBefore<T>(this MethodBase methodInfo, Action<T> action) {
        methodInfo.ILHook((cursor, _) => {
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate(action);
        });
    }

    public static void HookBefore(this MethodBase methodInfo, Action action) {
        methodInfo.ILHook((cursor, _) => {
            cursor.EmitDelegate(action);
        });
    }

    public static void HookAfter<T>(this MethodBase methodInfo, Action<T> action) {
        methodInfo.ILHook((cursor, _) => {
            while (cursor.TryGotoNext(MoveType.AfterLabel, i => i.OpCode == OpCodes.Ret)) {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate(action);
                cursor.Index++;
            }
        });
    }

    public static void HookAfter(this MethodBase methodInfo, Action action) {
        methodInfo.ILHook((cursor, _) => {
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
            methodInfo.ILHook(il => {
                ILCursor ilCursor = new(il);
                Instruction start = ilCursor.Next;
                ilCursor.Emit(OpCodes.Call, conditionType.GetMethodInfo(conditionMethodName));
                ilCursor.Emit(OpCodes.Brfalse, start).Emit(OpCodes.Ret);
            });
        }
    }
    public static ILHook ManualAppliedILHook(this MethodBase from, ILContext.Manipulator manipulator) {
        return new ILHook(from, il => RunManipulator(from, manipulator, il), applyByDefault: false);
    }
    public static ILHook ManualAppliedILHook(this MethodBase from, ILContext.Manipulator manipulator, DetourConfig config) {
        return new ILHook(from, il => RunManipulator(from, manipulator, il), config, applyByDefault: false);
    }
}

internal static class EventOnHook {
    private static void ThrowException() {
        Logger.Log("TAS Helper", "EventOnHook finds a method with improper attributes!");
        throw new Exception("Invalid ParameterInfo");
    }
    internal static class _LevelUpdate {
        public delegate void UpdateHandler(Level level);

        public static event UpdateHandler BeforeUpdate;

        public static event UpdateHandler AfterUpdate;

        private delegate void UpdateHandler_Parameter0();

        private static event UpdateHandler_Parameter0 BeforeUpdate_Parameter0;

        private static event UpdateHandler_Parameter0 AfterUpdate_Parameter0;

        private delegate void UpdateHandler_Parameter1(Level level);

        private static event UpdateHandler_Parameter1 BeforeUpdate_Parameter1;

        private static event UpdateHandler_Parameter1 AfterUpdate_Parameter1;

        [EventOnHook]
        private static void CreateOnHook() {
            Everest.Events.Level.OnBeforeUpdate += OnBeforeUpdate;
            Everest.Events.Level.OnAfterUpdate += OnAfterUpdate;

            // execution order
            // Scene.BeforeUpdate -> Everest.Events.Level.BeforeUpdate -> Level.Update -> Everest.Events.Level.AfterUpdate -> Scene.AfterUpdate 
        }

        [Unload]
        private static void Unload() {
            Everest.Events.Level.OnBeforeUpdate -= OnBeforeUpdate;
            Everest.Events.Level.OnAfterUpdate -= OnAfterUpdate;
            BeforeUpdate = AfterUpdate = null;
        }

        [Initialize]
        private static void Initialize() {
            foreach (MethodInfo method in typeof(AttributeUtils).Assembly.GetTypesSafe().SelectMany(type => type
                .GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))) {
                if (method.GetCustomAttribute<LevelUpdateAttribute>() is not LevelUpdateAttribute attr) {
                    continue;
                }

                if (attr.Before) {
                    switch (method.GetParameters().Length) {
                        case 0: {
                                BeforeUpdate_Parameter0 += (UpdateHandler_Parameter0)method.CreateDelegate(typeof(UpdateHandler_Parameter0));
                                break;
                            }
                        case 1: {
                                BeforeUpdate_Parameter1 += (UpdateHandler_Parameter1)method.CreateDelegate(typeof(UpdateHandler_Parameter1));
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
                                AfterUpdate_Parameter0 += (UpdateHandler_Parameter0)method.CreateDelegate(typeof(UpdateHandler_Parameter0));
                                break;
                            }
                        case 1: {
                                AfterUpdate_Parameter1 += (UpdateHandler_Parameter1)method.CreateDelegate(typeof(UpdateHandler_Parameter1));
                                break;
                            }
                        default: {
                                ThrowException();
                                break;
                            }
                    }
                }
            }


            if (BeforeUpdate_Parameter0 is not null) {
                BeforeUpdate += _ => BeforeUpdate_Parameter0.Invoke();
            }
            if (BeforeUpdate_Parameter1 is not null) {
                BeforeUpdate += scene => BeforeUpdate_Parameter1.Invoke(scene);
            }
            if (AfterUpdate_Parameter0 is not null) {
                AfterUpdate += _ => AfterUpdate_Parameter0.Invoke();
            }
            if (AfterUpdate_Parameter1 is not null) {
                AfterUpdate += scene => AfterUpdate_Parameter1.Invoke(scene);
            }
        }

        private static void OnBeforeUpdate(Level self) {
            BeforeUpdate?.Invoke(self);
        }

        private static void OnAfterUpdate(Level self) {
            AfterUpdate?.Invoke(self);
        }
    }

    internal static class _LoadLevel {
        // avoid naming this class "Level", so custom info will not find it using "Level", so "Level.Wind" will work properly in infohud
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
            LoadLevel = LoadLevel_Before = null;
            LoadLevel_Parameter0 = LoadLevel_Before_Parameter0 = null;
            LoadLevel_Parameter1 = LoadLevel_Before_Parameter1 = null;
            LoadLevel_Parameter2 = LoadLevel_Before_Parameter2 = null;
            foreach (MethodInfo method in typeof(AttributeUtils).Assembly.GetTypesSafe().SelectMany(type => type
            .GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))) {
                if (method.GetCustomAttribute<LoadLevelAttribute>() is not LoadLevelAttribute attr) {
                    continue;
                }
                /*
                string type = method.DeclaringType.FullName;
                if (type.StartsWith("Celeste.Mod.TASHelper.Entities.PauseUpdater")) {
                    Logger.Log(LogLevel.Warn, "TASHelper EventOnHook LoadLevel Remove", type);
                    continue;
                }
                Logger.Log(LogLevel.Debug, "TASHelper EventOnHook LoadLevel", type);
                */

                if (attr.Before) {
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
                else {
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
            // but that's only at the end of Level.OnLoadLevel
            // so i still need a LoadLevel_Before

            using (DetourContextHelper.ManagedHook("TAS Helper LoadLevel").AppliedAfter(DetourContextHelper.CelesteTAS).Use()) {
                On.Celeste.Level.LoadLevel += OnLoadLevel;
            }
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


    internal static class _EntityList {
        public delegate void DebugRenderHandler(Monocle.EntityList self, Monocle.Camera camera);

        public static event DebugRenderHandler DebugRender_Before;
        public static event DebugRenderHandler DebugRender_After;

        private delegate void DebugRenderHandler_Parameter0();

        private static event DebugRenderHandler_Parameter0 DebugRender_Before_Parameter0;
        private static event DebugRenderHandler_Parameter0 DebugRender_After_Parameter0;

        private delegate void DebugRenderHandler_Parameter1(Monocle.EntityList self);

        private static event DebugRenderHandler_Parameter1 DebugRender_Before_Parameter1;
        private static event DebugRenderHandler_Parameter1 DebugRender_After_Parameter1;

        [Initialize]
        private static void Initialize() {
            DebugRender_Before = null;
            DebugRender_After = null;
            DebugRender_Before_Parameter0 = null;
            DebugRender_After_Parameter0 = null;
            DebugRender_Before_Parameter1 = null;
            DebugRender_After_Parameter1 = null;
            foreach (MethodInfo method in typeof(AttributeUtils).Assembly.GetTypesSafe().SelectMany(type => type
            .GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))) {
                if (method.GetCustomAttribute<AddDebugRenderAttribute>() is not { } attr) {
                    continue;
                }
                if (attr.Before) {
                    switch (method.GetParameters().Length) {
                        case 0: {
                                DebugRender_Before_Parameter0 += (DebugRenderHandler_Parameter0)method.CreateDelegate(typeof(DebugRenderHandler_Parameter0));
                                break;
                            }
                        case 1: {
                                DebugRender_Before_Parameter1 += (DebugRenderHandler_Parameter1)method.CreateDelegate(typeof(DebugRenderHandler_Parameter1));
                                break;
                            }
                        case 2: {
                                DebugRender_Before += (DebugRenderHandler)method.CreateDelegate(typeof(DebugRenderHandler));
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
                                DebugRender_After_Parameter0 += (DebugRenderHandler_Parameter0)method.CreateDelegate(typeof(DebugRenderHandler_Parameter0));
                                break;
                            }
                        case 1: {
                                DebugRender_After_Parameter1 += (DebugRenderHandler_Parameter1)method.CreateDelegate(typeof(DebugRenderHandler_Parameter1));
                                break;
                            }
                        case 2: {
                                DebugRender_After += (DebugRenderHandler)method.CreateDelegate(typeof(DebugRenderHandler));
                                break;
                            }
                        default: {
                                ThrowException();
                                break;
                            }
                    }
                }
            }
            if (DebugRender_Before_Parameter0 is not null) {
                DebugRender_Before += (_, _) => DebugRender_Before_Parameter0.Invoke();
            }
            if (DebugRender_Before_Parameter1 is not null) {
                DebugRender_Before += (self, _) => DebugRender_Before_Parameter1.Invoke(self);
            }
            if (DebugRender_After_Parameter0 is not null) {
                DebugRender_After += (_, _) => DebugRender_After_Parameter0.Invoke();
            }
            if (DebugRender_After_Parameter1 is not null) {
                DebugRender_After += (self, _) => DebugRender_After_Parameter1.Invoke(self);
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
            DebugRender_Before?.Invoke(self, camera);
            orig(self, camera);
            DebugRender_After?.Invoke(self, camera);
        }
    }
}

#if DEBUG
public static class CILCodeHelper {
    public static void CILCodeLogger(this ILCursor cursor, int logCount = 999999, bool useCommand = true) {
        // remember, Commands.Log can only work in Initialize()
        if (useCommand) {
            Celeste.Commands.Log("------------------------------");
        }
        int orig_index = cursor.Index;
        System.Text.StringBuilder sb = new();
        sb.AppendLine("---- CILCodeLogger ----");
        if (Apply) {
            if (AsShift) {
                cursor.Index += Position;
            }
            else {
                cursor.Index = Position;
            }
        }
        while (logCount > 0 && cursor.Next is not null) {
            string str = Parser.Parse(cursor.Next);
            sb.AppendLine("    " + str);
            if (useCommand) {
                Celeste.Commands.Log(str);
            }
            logCount--;
            cursor.Index++;
        }
        cursor.Index = orig_index;
        Logger.Log(LogLevel.Debug, "TAS Helper", sb.ToString());
    }

    private static class Parser {
        internal static string Parse(Instruction ins) {
            if (ins is null) {
                return "NULL";
            }

            if (ins.Operand is ILLabel or Instruction) {
                string operand = (ins.Operand is ILLabel ? "ILLabel" : "Instruction").PadLeft(12);
                Instruction target = ins.Operand is Instruction targetIns ? targetIns : ((ILLabel)ins.Operand).Target!;
                return $"{OffsetToString(ins.Offset)},  {OpCodeToString(ins.OpCode)}, {operand} ->    {Parse(target)}";
            }
            if (ins.OpCode == OpCodes.Ldstr) {
                return $"{OffsetToString(ins.Offset)},  {OpCodeToString(ins.OpCode)}, \"{ins.Operand}\"";
            }
            if (ins.Operand is not null) {
                return $"{OffsetToString(ins.Offset)},  {OpCodeToString(ins.OpCode)}, {ins.Operand.ToString().Replace(" ", "    ")}";
            }
            return $"{OffsetToString(ins.Offset)},  {OpCodeToString(ins.OpCode)}";
        }

        private static string OpCodeToString(OpCode code) {
            return PadToMultiple(code.ToString(), 12);
        }

        private static string OffsetToString(int offset) {
            return $"{offset:x4}";
        }

        private static string PadToMultiple(object obj, int b) {
            string str = obj?.ToString() ?? " ";
            int length = str.Length;
            int targetLength = ((length - 1) / b + 1) * b;
            return str.PadRight(targetLength);
        }
    }

    public static void CILCodeLogger(this MulticastDelegate func) {
        if (func is null) {
            Logger.Log("TAS Helper", "CILCodeLogger: This Delegate is null.");
            return;
        }
        func.GetInvocationList().ToList().ForEach(x => x.Method.CILCodeLogger());
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
        foreach (MethodBase method in methods) {
            method.CILCodeLogger();
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
#endif
