using Monocle;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using MonoMod.RuntimeDetour;
using System.Reflection;
using Celeste.Mod.TASHelper.Entities;
using TAS.EverestInterop;

namespace Celeste.Mod.TASHelper.OrderOfOperation;
internal static class OOP_Core {

    private static MethodInfo EngineUpdate = typeof(Engine).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
    private static MethodInfo LevelUpdate = typeof(Level).GetMethod("Update");
    private static MethodInfo SceneUpdate = typeof(Scene).GetMethod("Update");
    private static MethodInfo EntityListUpdate = typeof(EntityList).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
    private static MethodInfo PlayerUpdate = typeof(Player).GetMethod("Update");
    private static MethodInfo PlayerOrigUpdate = typeof(Player).GetMethod("orig_Update");
    // if we add a breakpoint to A, which is called by B, then we must add breakpoints to B
    // so that any hook given by other mods are handled properly

    public static bool Applied = false;

    public static void Step() {
        if (!Applied) {
            ApplyAll();
        }
        else {
            stepping = true;
            SpringBoard.RefreshAll(); // spring board relies on passed Breakpoints, so it must be cleared later
            ResetState();
            BreakPoints.passedBreakPoints.Clear();
        }
    }

    private static bool GetStepping() {
        if (stepping) {
            stepping = false;
            return true;
        }
        return false;
    }

    private static void SendText(string str) {
        HotkeyWatcher.instance?.Refresh(str.Replace(BreakPoints.Prefix, ""));
    }

    private static bool stepping = false;

    private static ILHookConfig manualConfig = default;

    private static readonly Action<ILCursor> NullAction = (cursor) => { };

    [Initialize]
    public static void Initialize() {
        manualConfig.ManualApply = true;

        BreakPoints.MarkEnding(EngineUpdate, "EngineUpdate end", () => prepareToUndoAll = true); // i should have configured detourcontext... don't know why, but MarkEnding must be called at first (at least before those breakpoints on same method)

        BreakPoints.MarkEnding(LevelUpdate, "LevelUpdate end", () => LevelUpdate_Entry.SubMethodPassed = true);

        BreakPoints.MarkEnding(SceneUpdate, "SceneUpdate end", () => SceneUpdate_Entry.SubMethodPassed = true);

        BreakPoints.MarkEnding(PlayerUpdate, "PlayerUpdate end", () => EntityListUpdate_Entry.SubMethodPassed = true);

        BreakPoints.MarkEnding(PlayerOrigUpdate, "PlayerOrigUpdate end", () => PlayerOrigUpdate_Entry.SubMethodPassed = true);

        BreakPoints.CreateImpl(EngineUpdate, "EngineUpdate_Start", label => (cursor, _) => {
            cursor.Emit(OpCodes.Ldstr, label);
            Instruction mark = cursor.Prev;
            cursor.EmitDelegate(BreakPoints.RecordLabel);
            cursor.Emit(OpCodes.Ret);

            cursor.Goto(0);
            cursor.EmitDelegate(GetStepping);
            cursor.Emit(OpCodes.Brtrue, mark);
            cursor.Emit(OpCodes.Ret);
        });
        
        BreakPoints.Create(EngineUpdate, "EngineUpdate_SceneBeforeUpdate_Start", 
            ins => ins.OpCode == OpCodes.Ldarg_0,
            ins => ins.MatchLdfld<Engine>("scene"),
            ins => ins.MatchCallOrCallvirt<Scene>("BeforeUpdate")
        );

        LevelUpdate_Entry = BreakPoints.CreateFull(EngineUpdate, "EngineUpdate_LevelUpdate_End", 3, NullAction, NullAction,
            ins => ins.OpCode == OpCodes.Ldarg_0,
            ins => ins.MatchLdfld<Engine>("scene"),
            ins => ins.MatchCallOrCallvirt<Scene>("Update")
        );

        SceneUpdate_Entry = BreakPoints.CreateFull(LevelUpdate, "LevelUpdate_SceneUpdate_End", 2, NullAction, NullAction , ins => ins.OpCode == OpCodes.Ldarg_0, ins => ins.MatchCallOrCallvirt<Scene>("Update"));

        EntityListUpdate_Entry = BreakPoints.CreateFull(SceneUpdate, "SceneUpdate_EntityListUpdate_End", 3, NullAction, NullAction,
            ins => ins.OpCode == OpCodes.Ldarg_0,
            ins => ins.MatchCallOrCallvirt<Scene>("get_Entities"),
            ins => ins.MatchCallOrCallvirt<EntityList>("Update")
        );


        PlayerOrigUpdate_Entry = BreakPoints.CreateFull(PlayerUpdate, "PlayerUpdate_PlayerOrigUpdate_End", 2, NullAction, NullAction) ;

        BreakPoints.Create(PlayerOrigUpdate, "PlayerOrigUpdate_Start");

        BreakPoints.Create(PlayerOrigUpdate, "PlayerOrigUpdate_BaseUpdate", 
            ins => ins.OpCode == OpCodes.Ldarg_0,
            ins => ins.MatchCallOrCallvirt<Actor>("Update")
        );

        BreakPoints.CreateFull(PlayerOrigUpdate, "PlayerOrigUpdate_MoveH", 0, NullAction, 
            cursor => {
                cursor.Index += 3;
                cursor.MoveAfterLabels();
            },
            ins => ins.OpCode == OpCodes.Ldarg_0,
            ins => ins.OpCode == OpCodes.Ldc_I4_0,
            ins => ins.MatchCallOrCallvirt<Player>("set_Ducking"),
            ins => ins.OpCode == OpCodes.Ldarg_0,
            ins => ins.OpCode == OpCodes.Ldfld,
            ins => ins.MatchCallOrCallvirt<StateMachine>("get_State"),
            ins => ins.MatchLdcI4(9)
        );

        BreakPoints.CreateFull(PlayerOrigUpdate, "PlayerOrigUpdate_MoveV", 0, NullAction,
            cursor => {
                cursor.Index += 3;
                cursor.MoveAfterLabels();
            },
            ins => ins.OpCode == OpCodes.Ldnull,
            ins => ins.MatchCallOrCallvirt<Actor>("MoveH"),
            ins => ins.OpCode == OpCodes.Pop,
            ins => ins.OpCode == OpCodes.Ldarg_0,
            ins => ins.OpCode == OpCodes.Ldfld,
            ins => ins.MatchCallOrCallvirt<StateMachine>("get_State"),
            ins => ins.MatchLdcI4(9)
        );

        BreakPoints.Create(PlayerOrigUpdate, "PlayerOrigUpdate_EntityCollide", 
            ins => ins.OpCode == OpCodes.Ldarg_0,
            ins => ins.MatchCallOrCallvirt<Player>("get_Dead"),
            ins => ins.OpCode == OpCodes.Brtrue,
            ins => ins.OpCode == OpCodes.Ldarg_0,
            ins => ins.OpCode == OpCodes.Ldfld,
            ins => ins.MatchCallOrCallvirt<StateMachine>("get_State"),
            ins => ins.MatchLdcI4(21)
        );

        BreakPoints.Create(EngineUpdate, "EngineUpdate_SceneAfterUpdate_Start",
            ins => ins.OpCode == OpCodes.Ldarg_0,
            ins => ins.MatchLdfld<Engine>("scene"),
            ins => ins.MatchCallOrCallvirt<Scene>("AfterUpdate")
        );


        SpringBoard.Create(EngineUpdate);
        SpringBoard.Create(LevelUpdate);
        SpringBoard.Create(SceneUpdate);
        SpringBoard.Create(EntityListUpdate);
        SpringBoard.Create(PlayerUpdate);
        SpringBoard.Create(PlayerOrigUpdate);

        hookTASIsPaused = new ILHook(typeof(TAS.EverestInterop.Core).GetMethod("IsPause", BindingFlags.NonPublic | BindingFlags.Static), il => {
            ILCursor cursor = new ILCursor(il);
            cursor.Emit(OpCodes.Ldc_I4_0);
            cursor.Emit(OpCodes.Ret);
        }, manualConfig);

    }

    private static ILHook hookTASIsPaused;

    private static bool prepareToUndoAll = false;

    public static void ApplyAll() {
        BreakPoints.ApplyAll();
        SpringBoard.RefreshAll();
        hookTASIsPaused.Apply();
        Applied = true;

        ResetState();

        SendText("OOP Stepping start");
    }

    public static void UndoAll() {
        SpringBoard.UndoAll();
        BreakPoints.UndoAll();
        hookTASIsPaused.Undo();
        Applied = false;

        ResetState();

        SendText("OOP Stepping end");
    }

    private static void ResetState() {
        prepareToUndoAll = false;
        LevelUpdate_Entry.SubMethodPassed = false;
        SceneUpdate_Entry.SubMethodPassed = false;
        EntityListUpdate_Entry.SubMethodPassed = false;
        PlayerOrigUpdate_Entry.SubMethodPassed = false;
    }

    private static BreakPoints LevelUpdate_Entry;
    private static BreakPoints SceneUpdate_Entry;
    private static BreakPoints EntityListUpdate_Entry;
    private static BreakPoints PlayerOrigUpdate_Entry;

    private class BreakPoints {

        // if a BreakPoint has subBreakPoints, make sure it's exactly before the method call, and emit Ret exactly after the method call
        // also, make sure if we jump to a breakpoint from start, the stack behavior is ok (i.e. no temp variable lives from before a breakpoint to after it)

        public const string Prefix = "TAS Helper OOP_Core";

        public static readonly Dictionary<string, BreakPoints> dictionary = new();

        public static readonly HashSet<string> passedBreakPoints = new HashSet<string>();

        public static readonly Dictionary<MethodBase, HashSet<BreakPoints>> detoursOnThisMethod = new();

        public int RetShift = 0;

        public string UID;

        public IDetour labelEmitter;

        public bool? SubMethodPassed = null;

        private BreakPoints(string ID, IDetour detour) {
            UID = ID;
            labelEmitter = detour;
        }

        public static BreakPoints Create(MethodBase from, string label, params Func<Instruction, bool>[] predicates) {
            return CreateFull(from, label, 0, NullAction, NullAction, predicates);
        }

        public static BreakPoints CreateFull(MethodBase from, string label, int RetShift, Action<ILCursor> before, Action<ILCursor> after, params Func<Instruction, bool>[] predicates) {
            Func<string, Action<ILCursor, ILContext>> manipulator = (label) => (cursor, _) => {
                before(cursor);
                if (cursor.TryGotoNext(MoveType.AfterLabel, predicates)) {
                    after(cursor);
                    cursor.Emit(OpCodes.Ldstr, label);
                    cursor.EmitDelegate(RecordLabel);
                    cursor.Index += RetShift; // when there's a method, which internally has breakpoints, exactly after this breakpoint, then we Ret after this method call
                    cursor.Emit(OpCodes.Ret);
                }
            };
            return CreateImpl(from, label, manipulator, RetShift);
        }

        internal static BreakPoints CreateImpl(MethodBase from, string label, Func<string, Action<ILCursor, ILContext>> manipulator, int RetShift = 0) {
            string ID = CreateUID(label);
            IDetour detour;
            using (new DetourContext { After = new List<string> { "*", "CelesteTAS-EverestInterop", "TASHelper", "TAS Helper OOP_Core Ending" }, ID = "TAS Helper OOP_Core BreakPoints" }) {
                detour = new ILHook(from, il => {
                    ILCursor cursor = new(il);
                    manipulator(ID)(cursor, il);
                }, manualConfig);
            }
            BreakPoints breakpoint = new BreakPoints(ID, detour);
            breakpoint.RetShift = RetShift;
            if (!detoursOnThisMethod.ContainsKey(from)) {
                detoursOnThisMethod[from] = new HashSet<BreakPoints>();
            }
            detoursOnThisMethod[from].Add(breakpoint);
            dictionary[ID] = breakpoint;
            return breakpoint;
        }

        private static string CreateUID(string label) {
            string result = $"{Prefix}{label}";
            if (!dictionary.ContainsKey(result)) {
                return result;
            }
            int index = 1;
            do {
                result = $"{Prefix}{label}_{index}";
                index++;
            } while(dictionary.ContainsKey(result));
            return result;
        }
        internal static void RecordLabel(string label) {
            passedBreakPoints.Add(label);
            SendText(label); // if several labels are recorded in same frame, then the last one will be the output
        }

        public static BreakPoints MarkEnding(MethodBase method, string label, Action? afterRetAction = null) {
            string ID = CreateUID(label);
            IDetour detour;
            using (new DetourContext { After = new List<string> { "*", "CelesteTAS-EverestInterop", "TASHelper" }, ID = "TAS Helper OOP_Core Ending" }) {
                detour = new ILHook(method, il => {
                    ILCursor cursor = new(il);
                    while (cursor.TryGotoNext(MoveType.AfterLabel, i => i.OpCode == OpCodes.Ret)) {
                        cursor.Emit(OpCodes.Ldstr, ID);
                        cursor.EmitDelegate(RecordLabel);
                        // don't know why but i fail to add a beforeRetAction here
                        cursor.Emit(OpCodes.Ret);
                        if (afterRetAction is not null) {
                            cursor.EmitDelegate(afterRetAction);
                        }
                        cursor.Index++;
                    }
                }, manualConfig);
            }
            BreakPoints breakpoint = new BreakPoints(ID, detour);

            if (!detoursOnThisMethod.ContainsKey(method)) {
                detoursOnThisMethod[method] = new HashSet<BreakPoints>();
            }
            detoursOnThisMethod[method].Add(breakpoint);
            dictionary[ID] = breakpoint;
            return breakpoint;
        }

        public static void ApplyAll() {
            foreach (BreakPoints breakPoints in dictionary.Values) {
                breakPoints.labelEmitter.Apply();
            }
        }

        public static void UndoAll() {
            foreach (BreakPoints breakPoints in dictionary.Values) {
                breakPoints.labelEmitter.Undo();
            }
            passedBreakPoints.Clear();
        }

        [Unload]
        private static void Unload() {
            foreach (BreakPoints breakPoints in dictionary.Values) {
                breakPoints.labelEmitter.Dispose();
            }
        }
    }

    private class SpringBoard {
        // mainly used to jump from one breakpoint to the next (sometimes not actually jump, e.g. we can't jump from a breakpoint in Engine.Update to a breakpoint in Level.Update, we have to use at least two springBoards)
        // jump if current BreakPoint is Finished; otherwise track current BreakPoint to passedBreakPoints, run codes, and return before next BreakPoint
        public static readonly Dictionary<MethodBase, IDetour> dictionary = new();

        private static void Refresh(MethodBase methodBase) {
            if (dictionary.ContainsKey(methodBase)) {
                dictionary[methodBase].Undo();
                dictionary[methodBase].Apply();
                return;
            }
            Create(methodBase);
        }

        public static void Create(MethodBase methodBase) {
            IDetour detour;
            using (new DetourContext { After = new List<string> { "*", "CelesteTAS-EverestInterop", "TASHelper", "TAS Helper OOP_Core BreakPoints", "TAS Helper OOP_Core Ending" }, ID = "TAS Helper OOP_Core SpringBoard" }) {
                detour = new ILHook(methodBase, il => {
                    ILCursor cursor = new(il);
                    if (cursor.TryGotoNext(ins => ins.OpCode == OpCodes.Ldstr && BreakPoints.passedBreakPoints.Contains((string)ins.Operand))) {
                        // for EndingBreakpoints, there maybe several different matching results, but jump to one is enough
                        Instruction target = cursor.Next;
                        string label = (string)cursor.Next.Operand;
                        BreakPoints point = BreakPoints.dictionary[label];
                        if (point.SubMethodPassed is bool b && !b) {
                            // do nothing
                        }
                        else {
                            cursor.Index++;
                            cursor.MoveAfterLabels();
                            cursor.Emit(OpCodes.Pop); // in next run, we will jump to this label, pop (so it's not recorded), and run till next label
                            cursor.Remove(); // remove RecordLabel
                            cursor.Index += point.RetShift;
                            cursor.Remove(); // remove Ret
                        }

                        cursor.Goto(0);
                        if (methodBase == EngineUpdate) {
                            cursor.Goto(3, MoveType.AfterLabel);
                        }
                        cursor.Emit(OpCodes.Br, target);
                    }

                }, manualConfig);
            }
            dictionary[methodBase] = detour;
        }

        public static void RefreshAll() {
            foreach (MethodBase method in dictionary.Keys) {
                Refresh(method);
            }
        }

        public static void UndoAll() {
            foreach (IDetour detour in dictionary.Values) {
                detour.Undo();
            }
        }

        [Unload]
        private static void Unload() {
            foreach (IDetour detour in dictionary.Values) {
                detour.Dispose();
            }
        }
    }


    [Load]
    public static void Load() {
        using (new DetourContext { After = new List<string> { "*", "CelesteTAS-EverestInterop", "TASHelper"}, ID = "TAS Helper OOP_Core OnLevelRender" }){
            On.Celeste.Level.Render += OnLevelRender;
        }
    }

    [Unload]
    public static void Unload() {
        On.Celeste.Level.Render -= OnLevelRender;
    }
    private static void OnLevelRender(On.Celeste.Level.orig_Render orig, Level self) {
        orig(self);
        if (Applied) {
            if (prepareToUndoAll) {
                UndoAll();
            }

            Hotkeys.Update(); // TH_Hotkeys relies on Hotkeys, so it needs update
            TAS.GameInfo.Update();
        }
    }
}
