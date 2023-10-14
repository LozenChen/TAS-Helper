using Monocle;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using MonoMod.RuntimeDetour;
using System.Reflection;
using Celeste.Mod.TASHelper.Entities;
using TAS.EverestInterop;
using TAS;

namespace Celeste.Mod.TASHelper.OrderOfOperation;
internal static class OOP_Core {

    private static readonly MethodInfo EngineUpdate = typeof(Engine).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly MethodInfo LevelUpdate = typeof(Level).GetMethod("Update");
    private static readonly MethodInfo SceneUpdate = typeof(Scene).GetMethod("Update");
    private static readonly MethodInfo EntityListUpdate = typeof(EntityList).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly MethodInfo PlayerUpdate = typeof(Player).GetMethod("Update");
    private static readonly MethodInfo PlayerOrigUpdate = typeof(Player).GetMethod("orig_Update");
    private static readonly MethodInfo EntityUpdateWithBreakPoints = typeof(BreakPoints.ForEachBreakPoints).GetMethod(BreakPoints.ForEachBreakPoints.entityUpdateWithBreakPoints, BindingFlags.NonPublic | BindingFlags.Static);
    private static readonly MethodInfo EntityUpdateWithoutBreakPoints = typeof(BreakPoints.ForEachBreakPoints).GetMethod(BreakPoints.ForEachBreakPoints.entityUpdateWithoutBreakPoints, BindingFlags.NonPublic | BindingFlags.Static);
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
            ResetTempState();
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
        // Celeste.Commands.Log(str.Replace(BreakPoints.Prefix, ""));
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

       // BreakPoints.MarkEnding(EntityListUpdate, "EntityListUpdate end"); No, this has to be treated specially, see ForEachBreakPoints.MarkEndingSpecial 

        BreakPoints.MarkEnding(PlayerUpdate, "PlayerUpdate end", () => EntityUpdate_withBreakPoints_Entry.SubMethodPassed = true);

        BreakPoints.MarkEnding(PlayerOrigUpdate, "PlayerOrigUpdate end", () => PlayerOrigUpdate_Entry.SubMethodPassed = true);
        
        BreakPoints.MarkEnding(EntityUpdateWithBreakPoints, "Entity(Pre/./Post)Update end (with BreakPoints)", BreakPoints.ForEachBreakPoints.EntityUpdateWithBreakPointsDone);

        BreakPoints.MarkEnding(EntityUpdateWithoutBreakPoints, "Entity(Pre/./Post)Update end (without BreakPoints)", BreakPoints.ForEachBreakPoints.EntityUpdateWithoutBreakpointsDone);
        
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

        EntityListUpdate_ForEach_Entry = BreakPoints.CreateFull(EntityListUpdate, "EntityListUpdate_ForEach_Entry", BreakPoints.RetShiftDoNotEmit, NullAction, NullAction,
            ins => ins.OpCode == OpCodes.Ldarg_0,
            ins => ins.MatchLdfld<EntityList>("entities"),
            ins => ins.MatchCallOrCallvirt<List<Entity>>("GetEnumerator"),
            ins => ins.OpCode == OpCodes.Stloc_0);

        
        EntityUpdate_withBreakPoints_Entry = BreakPoints.CreateFull(EntityUpdateWithBreakPoints, "EntityUpdate_Entry", 2, NullAction, NullAction, ins => ins.OpCode == OpCodes.Ldarg_0, ins => ins.MatchCallOrCallvirt<Entity>("Update"));

        EntityUpdate_withoutBreakPoints_Entry = BreakPoints.Create(EntityUpdateWithoutBreakPoints, "EntityUpdate_Start");
        
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
        
        BreakPoints.ForEachBreakPoints.Create();
        BreakPoints.ForEachBreakPoints.MarkEndingSpecial();
        BreakPoints.ForEachBreakPoints.AddToTarget("Player", true);
        /*
        BreakPoints.ForEachBreakPoints.AddToTarget("CrystalStaticSpinner[f-11:902]");
        BreakPoints.ForEachBreakPoints.AddToTarget("BadelineBoost");
        */

        SpringBoard.Create(EngineUpdate);
        SpringBoard.Create(LevelUpdate);
        SpringBoard.Create(SceneUpdate);
        SpringBoard.Create(EntityListUpdate);
        SpringBoard.Create(EntityUpdateWithBreakPoints);
        SpringBoard.Create(EntityUpdateWithoutBreakPoints);
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
        BreakPoints.ForEachBreakPoints.Apply();
        hookTASIsPaused.Apply();
        Applied = true;
        ResetTempState();

        SendText("OOP Stepping start");
    }

    public static void UndoAll() {
        SpringBoard.UndoAll();
        BreakPoints.UndoAll();
        BreakPoints.ForEachBreakPoints.Undo();
        hookTASIsPaused.Undo();
        Applied = false;
        ResetTempState();

        SendText("OOP Stepping end");

        // todo: TAS does not sync after subframe-stepping a frame
    }

    private static void ResetTempState() {
        prepareToUndoAll = false;
        LevelUpdate_Entry.SubMethodPassed = false;
        SceneUpdate_Entry.SubMethodPassed = false;
        EntityListUpdate_Entry.SubMethodPassed = false;
        EntityListUpdate_ForEach_Entry.SubMethodPassed = false;
        EntityUpdate_withBreakPoints_Entry.SubMethodPassed = false;
        PlayerOrigUpdate_Entry.SubMethodPassed = false;
        BreakPoints.ForEachBreakPoints.ResetTemp();
    }

    private static BreakPoints LevelUpdate_Entry;
    private static BreakPoints SceneUpdate_Entry;
    private static BreakPoints EntityListUpdate_Entry;
    private static BreakPoints EntityListUpdate_ForEach_Entry;
    private static BreakPoints EntityUpdate_withBreakPoints_Entry;
    private static BreakPoints EntityUpdate_withoutBreakPoints_Entry;
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

        internal const int RetShiftDoNotEmit = -100;

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
                    if (RetShift > RetShiftDoNotEmit) {
                        cursor.Index += RetShift; // when there's a method, which internally has breakpoints, exactly after this breakpoint, then we Ret after this method call
                        cursor.Emit(OpCodes.Ret);
                    } 
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

        public static BreakPoints MarkEnding(MethodBase method, string label, Action? afterRetAction = null, bool EmitRet = true, MoveType moveType = MoveType.AfterLabel) {
            string ID = CreateUID(label);
            IDetour detour;
            using (new DetourContext { After = new List<string> { "*", "CelesteTAS-EverestInterop", "TASHelper" }, ID = "TAS Helper OOP_Core Ending" }) {
                detour = new ILHook(method, il => {
                    ILCursor cursor = new(il);
                    while (cursor.TryGotoNext(moveType, i => i.OpCode == OpCodes.Ret)) {
                        cursor.Emit(OpCodes.Ldstr, ID);
                        cursor.EmitDelegate(RecordLabel);
                        // don't know why but i fail to add a beforeRetAction here
                        if (EmitRet) {
                            cursor.Emit(OpCodes.Ret);
                        }
                        if (afterRetAction is not null) {
                            cursor.EmitDelegate(afterRetAction);
                        }
                        cursor.Index++;
                    }
                }, manualConfig);
            }
            BreakPoints breakpoint = new BreakPoints(ID, detour);
           if (!EmitRet) {
                breakpoint.RetShift = RetShiftDoNotEmit;
            }

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
            ForEachBreakPoints.Dispose();
        }

        public static class ForEachBreakPoints {

            private static readonly HashSet<string> targets = new();

            private static readonly HashSet<string> targets_withBreakpoints = new();

            private static readonly HashSet<string> removed_targets = new();

            private static string curr_target_withBreakpoint;

            private static string curr_target_withoutBreakpoint;

            private static int passed_targets = 0;
            private static int expected_passed_targets => removed_targets.Count;

            private static IDetour detour;

            internal static void Create() {
                using (new DetourContext { Before = new List<string> { "*", "CelesteTAS-EverestInterop", "TASHelper" }, ID = "TAS Helper OOP_Core ForEachBreakPoints" }) {
                    detour = new ILHook(EntityListUpdate, il => {
                        ILCursor cursor = new ILCursor(il);
                        Instruction Ins_continue;
                        Instruction Ins_run_normally;
                        Instruction Ins_ret;
                        ILLabel Loop_head;
                        if (cursor.TryGotoNext(ins => ins.OpCode == OpCodes.Leave_S)) {
                            cursor.Next.MatchLeaveS(out ILLabel tmp);
                            cursor.Goto(tmp.Target);
                            Ins_ret = cursor.Next;
                            cursor.Goto(0);
                            if (cursor.TryGotoNext(MoveType.AfterLabel, ins => ins.MatchLdloca(0), ins => ins.MatchCallOrCallvirt<List<Entity>.Enumerator>("MoveNext"))) {
                                Ins_continue = cursor.Next;
                                cursor.Index += 2;
                                cursor.Next.MatchBrtrue(out Loop_head);
                                cursor.Goto(Loop_head.Target, MoveType.AfterLabel);
                                cursor.Index += 3;
                                Ins_run_normally = cursor.Next;
                                cursor.Emit(OpCodes.Ldloc_1);
                                cursor.EmitDelegate(IsGotoContinue);
                                cursor.Emit(OpCodes.Brtrue, Ins_continue);
                                cursor.Emit(OpCodes.Ldloc_1);
                                cursor.EmitDelegate(IsRunNormally);
                                cursor.Emit(OpCodes.Brtrue, Ins_run_normally);
                                cursor.Emit(OpCodes.Ldloc_1);
                                cursor.EmitDelegate(EntityUpdateWithBreakPoints);
                                cursor.Emit(OpCodes.Leave_S, Ins_ret);
                                cursor.Emit(OpCodes.Ldloc_1);
                                cursor.EmitDelegate(EntityUpdateWithoutBreakPoints);
                                cursor.Emit(OpCodes.Leave_S, Ins_ret);
                                cursor.Index -= 3;
                                Instruction Ins_TargetWithoutBreakpoints = cursor.Next;
                                cursor.Index -= 3;
                                cursor.Emit(OpCodes.Ldloc_1);
                                cursor.EmitDelegate(IsTargetWithBreakPoints);
                                cursor.Emit(OpCodes.Brfalse, Ins_TargetWithoutBreakpoints);
                            }
                        }
                    }, manualConfig);
                }
            }

            internal static void MarkEndingSpecial() {
                // instead of emit this at the Ret, we emit it at the Leave_S which points to Ret (which exits the try-block)
                // cause it seems hard to emit before Ret (maybe some issue with try-catch-finally block, idk)
                Func<string, Action<ILCursor, ILContext>> manipulator = (label) => (cursor, _) => {
                    if (cursor.TryGotoNext(MoveType.AfterLabel, ins => ins.OpCode == OpCodes.Leave_S, ins => ins.MatchLdloca(0), ins => ins.OpCode == OpCodes.Constrained, ins => ins.MatchCallOrCallvirt<IDisposable>("Dispose"), ins => ins.OpCode == OpCodes.Endfinally)) {
                        cursor.Emit(OpCodes.Ldstr, label);
                        cursor.EmitDelegate(RecordLabelWrap);
                        cursor.EmitDelegate(()=> {
                            EntityListUpdate_Entry.SubMethodPassed = true;
                            BreakPoints.ForEachBreakPoints.Reset();
                        });
                    }
                };
                CreateImpl(EntityListUpdate, "EntityListUpdate end", manipulator, RetShiftDoNotEmit);
            }

            private static void RecordLabelWrap(string s) {
                RecordLabel(s);
            }

            public static void AddToTarget(Entity entity, bool hasBreakPoints = false) {
                string str = GetID(entity);
                targets.Add(str);
                if (hasBreakPoints) {
                    targets_withBreakpoints.Add(str);
                }
            }

            public static void AddToTarget(string entityUID, bool hasBreakPoints = false) {
                targets.Add(entityUID);
                if (hasBreakPoints) {
                    targets_withBreakpoints.Add(entityUID);
                }
            }

            public static void Apply() {
                detour.Apply();
                Reset();
            }

            public static void Undo() {
                detour.Undo();
                Reset();
            }

            [Unload]
            public static void Dispose() {
                detour.Dispose();
            }

            public static void Reset() {
                removed_targets.Clear();
                passed_targets = 0;
            }

            public static void ResetTemp() {
                passed_targets = 0;
            }

            private static void EntityUpdateWithBreakPoints(Entity entity) {
                entity._PreUpdate();
                if (entity.Active) {
                    entity.Update();
                }
                entity._PostUpdate();
            }

            public static string entityUpdateWithBreakPoints => nameof(EntityUpdateWithBreakPoints);

            public static string entityUpdateWithoutBreakPoints => nameof(EntityUpdateWithoutBreakPoints);

            private static void EntityUpdateWithoutBreakPoints(Entity entity) {
                entity._PreUpdate();
                if (entity.Active) {
                    entity.Update();
                }
                entity._PostUpdate();
            }

            public static void EntityUpdateWithBreakPointsDone() {
                SendText($"{curr_target_withBreakpoint} update end");
                BreakPoints.passedBreakPoints.Remove(EntityUpdate_withBreakPoints_Entry.UID);
                removed_targets.Add(curr_target_withBreakpoint);
            }

            public static void EntityUpdateWithoutBreakpointsDone() {
                SendText($"{curr_target_withoutBreakpoint} update end");
                BreakPoints.passedBreakPoints.Remove(EntityUpdate_withoutBreakPoints_Entry.UID);
                removed_targets.Add(curr_target_withoutBreakpoint);
            }

            public static string GetID(Entity entity) {
                return entity.GetType().Name;
            }

            public static string GetUID(Entity entity) {
                if (entity.GetEntityData()?.ToEntityId().ToString() is { } id) {
                    return $"{entity.GetType().Name}[{id}]";
                }
                return $"{entity.GetType().Name}";
            }

            public static bool CheckContain(HashSet<string> target, Entity entity, out string id) {
                string ID = GetID(entity);
                if (target.Contains(ID)) {
                    id = ID;
                    return true;
                }
                if (entity.GetEntityData()?.ToEntityId().ToString() is { } entityID) {
                    id = $"{entity.GetType().Name}[{entityID}]";
                    return target.Contains(id);
                }
                id = "";
                return false;
            }

            private static bool IsGotoContinue(Entity entity) {
                if (CheckContain(targets, entity, out string str)) {
                    passed_targets++;
                    if (removed_targets.Contains(str)) {
                        return true;
                    }
                }
                return passed_targets < expected_passed_targets;
            }

            private static bool IsRunNormally(Entity entity) {
                return !CheckContain(targets, entity, out _);
            }

            private static bool IsTargetWithBreakPoints(Entity entity) {
                CheckContain(targets, entity, out string str);
                bool b = targets_withBreakpoints.Contains(str);
                if (b) {
                    SendText($"{str} update start");
                    curr_target_withBreakpoint = str;
                }
                else {
                    SendText($"{str} update start");
                    curr_target_withoutBreakpoint = str;
                }
                return b;
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
                            if (point.RetShift > BreakPoints.RetShiftDoNotEmit) {
                                cursor.Index += point.RetShift;
                                cursor.Remove(); // remove Ret
                            }
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
