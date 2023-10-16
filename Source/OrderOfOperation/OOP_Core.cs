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
            BreakPoints.ReformHashPassedBreakPoints();
            SpringBoard.RefreshAll(); // spring board relies on passed Breakpoints, so it must be cleared later
            ResetTempState();
        }
    }

    private static bool GetStepping() {
        if (stepping) {
            needGameInfoUpdate = true;
            stepping = false;
            return true;
        }
        return false;
    }

    internal static bool TryAutoSkip() {
        if (AutoSkipBreakpoints.Contains(lastText)) {
            return true;
        }
        return false;
    }

    public static readonly HashSet<string> AutoSkipBreakpoints = new ();

    [Command("oop_add_autoskip", "Autoskip a normal breakpoint (not a for-each breakpoint)(TAS Helper)")]
    public static void AddAutoSkip(string uid) {
        if (uid.StartsWith(BreakPoints.Prefix)) {
            AutoSkipBreakpoints.Add(uid);
        }
        else {
            AutoSkipBreakpoints.Add($"{BreakPoints.Prefix}{uid}");
        }
    }

    [Command("oop_remove_autoskip", "Do not autoskip a normal breakpoint (TAS Helper)")]
    public static void RemoveAutoSkip(string uid) {
        if (!AutoSkipBreakpoints.Remove(uid)) {
            AutoSkipBreakpoints.Remove(uid.Replace(BreakPoints.Prefix, ""));
        }
    }

    [Command("oop_show_autoskip", "Show all autoskipped breakpoints (TAS Helper)")]
    public static void ShowAutoSkip() {
        foreach (string s in AutoSkipBreakpoints) {
            Celeste.Commands.Log(s);
        }
    }

    private static string lastText = "";

    private static void SendText(string str) {
        lastText = str;
        HotkeyWatcher.instance?.Refresh(str.Replace(BreakPoints.Prefix, ""));
    }

    private static bool stepping = false;

    private static ILHookConfig manualConfig = default;

    private static readonly Action<ILCursor> NullAction = (cursor) => { };

    [Initialize]
    public static void Initialize() {
        manualConfig.ManualApply = true;

        BreakPoints.MarkEnding(EngineUpdate, "EngineUpdate end", () => prepareToUndoAll = true); // i should have configured detourcontext... don't know why, but MarkEnding must be called at first (at least before those breakpoints on same method)

        BreakPoints.MarkEnding(LevelUpdate, "LevelUpdate end", () => LevelUpdate_Entry.SubMethodPassed = true).AddAutoSkip();

        BreakPoints.MarkEnding(SceneUpdate, "SceneUpdate end", () => SceneUpdate_Entry.SubMethodPassed = true).AddAutoSkip();

        /*
        BreakPoints.MarkEnding(EntityListUpdate, "EntityListUpdate end", () => {
            EntityListUpdate_Entry.SubMethodPassed = true;
            BreakPoints.ForEachBreakPoints.Reset();
        }, false, MoveType.Before);
        */
        //No, this has to be treated specially, see ForEachBreakPoints.MarkEndingSpecial 

        BreakPoints.MarkEnding(PlayerUpdate, "PlayerUpdate end", () => EntityUpdate_withBreakPoints_Entry.SubMethodPassed = true).AddAutoSkip();

        BreakPoints.MarkEnding(PlayerOrigUpdate, "PlayerOrigUpdate end", () => PlayerOrigUpdate_Entry.SubMethodPassed = true);
        
        BreakPoints.MarkEnding(EntityUpdateWithBreakPoints, "Entity(Pre/./Post)Update end (with BreakPoints)", BreakPoints.ForEachBreakPoints.EntityUpdateWithBreakPointsDone).AddAutoSkip();

        BreakPoints.MarkEnding(EntityUpdateWithoutBreakPoints, "Entity(Pre/./Post)Update end (without BreakPoints)", BreakPoints.ForEachBreakPoints.EntityUpdateWithoutBreakpointsDone).AddAutoSkip();
        
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
        ).AddAutoSkip();

        LevelUpdate_Entry = BreakPoints.CreateFull(EngineUpdate, "EngineUpdate_LevelUpdate_End", 3, NullAction, NullAction,
            ins => ins.OpCode == OpCodes.Ldarg_0,
            ins => ins.MatchLdfld<Engine>("scene"),
            ins => ins.MatchCallOrCallvirt<Scene>("Update")
        ).AddAutoSkip();

        SceneUpdate_Entry = BreakPoints.CreateFull(LevelUpdate, "LevelUpdate_SceneUpdate_End", 2, NullAction, NullAction ,
            ins => ins.OpCode == OpCodes.Ldarg_0,
            ins => ins.MatchCallOrCallvirt<Scene>("Update"),
            ins => ins.OpCode == OpCodes.Br,
            ins => ins.OpCode == OpCodes.Ldarg_0,
            ins => ins.MatchLdfld<Level>("RetryPlayerCorpse")).AddAutoSkip();

        EntityListUpdate_Entry = BreakPoints.CreateFull(SceneUpdate, "SceneUpdate_EntityListUpdate_End", 3, NullAction, NullAction,
            ins => ins.OpCode == OpCodes.Ldarg_0,
            ins => ins.MatchCallOrCallvirt<Scene>("get_Entities"),
            ins => ins.MatchCallOrCallvirt<EntityList>("Update")
        ).AddAutoSkip();

        EntityListUpdate_ForEach_Entry = BreakPoints.CreateFull(EntityListUpdate, "EntityListUpdate_ForEach_Entry", BreakPoints.RetShiftDoNotEmit, NullAction, NullAction,
            ins => ins.OpCode == OpCodes.Ldarg_0,
            ins => ins.MatchLdfld<EntityList>("entities"),
            ins => ins.MatchCallOrCallvirt<List<Entity>>("GetEnumerator"),
            ins => ins.OpCode == OpCodes.Stloc_0).AddAutoSkip();

        
        EntityUpdate_withBreakPoints_Entry = BreakPoints.CreateFull(EntityUpdateWithBreakPoints, "EntityUpdate_Entry", 2, NullAction, NullAction, ins => ins.OpCode == OpCodes.Ldarg_0, ins => ins.MatchCallOrCallvirt<Entity>("Update")).AddAutoSkip();

        EntityUpdate_withoutBreakPoints_Entry = BreakPoints.Create(EntityUpdateWithoutBreakPoints, "EntityUpdate_Start").AddAutoSkip();
        
        PlayerOrigUpdate_Entry = BreakPoints.CreateFull(PlayerUpdate, "PlayerUpdate_PlayerOrigUpdate_End", 2, NullAction, NullAction).AddAutoSkip();

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
        ).AddAutoSkip();
        
        BreakPoints.ForEachBreakPoints.Create();
        BreakPoints.ForEachBreakPoints.MarkEndingSpecial();
        BreakPoints.ForEachBreakPoints.AddTarget("Player", true);
        /*
        BreakPoints.ForEachBreakPoints.AddTarget("CrystalStaticSpinner[f-11:902]");
        BreakPoints.ForEachBreakPoints.AddTarget("BadelineBoost");
        */

        SpringBoard.Create(EngineUpdate);
        SpringBoard.Create(LevelUpdate);
        SpringBoard.Create(SceneUpdate);
        SpringBoard.CreateSpecial();
        SpringBoard.Create(EntityUpdateWithBreakPoints);
        SpringBoard.Create(EntityUpdateWithoutBreakPoints);
        SpringBoard.Create(PlayerUpdate);
        SpringBoard.Create(PlayerOrigUpdate);

        hookTASIsPaused = new ILHook(typeof(TAS.EverestInterop.Core).GetMethod("IsPause", BindingFlags.NonPublic | BindingFlags.Static), il => {
            ILCursor cursor = new ILCursor(il);
            cursor.Emit(OpCodes.Ldc_I4_0);
            cursor.Emit(OpCodes.Ret);
        }, manualConfig);

        hookManagerUpdate = new ILHook(typeof(Manager).GetMethod("Update", BindingFlags.Public | BindingFlags.Static), il => {
            ILCursor cursor = new ILCursor(il);
            if (cursor.TryGotoNext(MoveType.AfterLabel, ins => ins.MatchCallOrCallvirt(typeof(Hotkeys).GetMethod("Update")))) {
                cursor.EmitDelegate(PretendPressHotkey);
            }
        }, manualConfig);
    }

    private static ILHook hookTASIsPaused;

    private static ILHook hookManagerUpdate;

    private static bool prepareToUndoAll = false;

    public static void ApplyAll() {
        BreakPoints.ApplyAll();
        SpringBoard.RefreshAll();
        BreakPoints.ForEachBreakPoints.Apply();
        hookTASIsPaused.Apply();
        hookManagerUpdate.Apply();
        Applied = true;
        ResetLongtermState();
        ResetTempState();

        SendText("OOP Stepping start");
    }

    public static void UndoAll() {
        SpringBoard.UndoAll();
        BreakPoints.UndoAll();
        BreakPoints.ForEachBreakPoints.Undo();
        hookTASIsPaused.Undo();
        hookManagerUpdate.Undo();
        Applied = false;
        ResetLongtermState();
        ResetTempState();
        SendText("OOP Stepping end");
    }

    private static void PretendPressHotkey() {
        Utils.ReflectionExtensions.SetFieldValue(Hotkeys.FrameAdvance, "Check", true);
    }

    private static void ResetLongtermState() {
        prepareToUndoAll = false;
        LevelUpdate_Entry.SubMethodPassed = false;
        SceneUpdate_Entry.SubMethodPassed = false;
        EntityListUpdate_Entry.SubMethodPassed = false;
        EntityListUpdate_ForEach_Entry.SubMethodPassed = false;
        PlayerOrigUpdate_Entry.SubMethodPassed = false;
        BreakPoints.latestBreakpointBackup.Clear();
    }
    private static void ResetTempState() {
        EntityUpdate_withBreakPoints_Entry.SubMethodPassed = false;
        BreakPoints.ForEachBreakPoints.ResetTemp();
        BreakPoints.passedBreakpoints.Clear();
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

        public const string Prefix = "TAS Helper OOP_Core::";

        public static readonly Dictionary<string, BreakPoints> dictionary = new();

        public static readonly HashSet<string> HashPassedBreakPoints = new HashSet<string>();

        public static readonly Dictionary<MethodBase, string> latestBreakpointBackup = new();

        public static readonly List<string> passedBreakpoints = new();

        public static readonly Dictionary<MethodBase, HashSet<BreakPoints>> detoursOnThisMethod = new();

        public int RetShift = 0;

        public string UID;

        public IDetour labelEmitter;

        public MethodBase method;

        public bool? SubMethodPassed = null;

        internal const int RetShiftDoNotEmit = -100;

        private BreakPoints(string ID, IDetour detour, MethodBase method) {
            UID = ID;
            labelEmitter = detour;
            this.method = method;
        }

        public static BreakPoints Create(MethodBase method, string label, params Func<Instruction, bool>[] predicates) {
            return CreateFull(method, label, 0, NullAction, NullAction, predicates);
        }

        public static BreakPoints CreateFull(MethodBase method, string label, int RetShift, Action<ILCursor> before, Action<ILCursor> after, params Func<Instruction, bool>[] predicates) {
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
            return CreateImpl(method, label, manipulator, RetShift);
        }

        internal static BreakPoints CreateImpl(MethodBase method, string label, Func<string, Action<ILCursor, ILContext>> manipulator, int RetShift = 0) {
            string ID = CreateUID(label);
            IDetour detour;
            using (new DetourContext { After = new List<string> { "*", "CelesteTAS-EverestInterop", "TASHelper", "TAS Helper OOP_Core Ending" }, ID = "TAS Helper OOP_Core BreakPoints" }) {
                detour = new ILHook(method, il => {
                    ILCursor cursor = new(il);
                    manipulator(ID)(cursor, il);
                }, manualConfig);
            }
            BreakPoints breakpoint = new BreakPoints(ID, detour, method);
            breakpoint.RetShift = RetShift;
            if (!detoursOnThisMethod.ContainsKey(method)) {
                detoursOnThisMethod[method] = new HashSet<BreakPoints>();
            }
            detoursOnThisMethod[method].Add(breakpoint);
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
            passedBreakpoints.Add(label);
            SendText(label); // if several labels are recorded in same frame, then the last one will be the output
        }

        public BreakPoints AddAutoSkip() {
            AutoSkipBreakpoints.Add(this.UID);
            return this;
        }

        public BreakPoints RemoveAutoSkip() {
            AutoSkipBreakpoints.Remove(this.UID);
            return this;
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
            BreakPoints breakpoint = new BreakPoints(ID, detour, method);
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

        public static void ReformHashPassedBreakPoints() {
            // we should not clear latestBreakpointBackup here
            foreach (string str in passedBreakpoints) {
                latestBreakpointBackup[dictionary[str].method] = str;
            }
            
            HashPassedBreakPoints.Clear();
            foreach (string s in latestBreakpointBackup.Values) {
                HashPassedBreakPoints.Add(s);
            }

        }

        public static void ApplyAll() {
            foreach (BreakPoints breakPoints in dictionary.Values) {
                breakPoints.labelEmitter.Apply();
            }
            HashPassedBreakPoints.Clear();
            latestBreakpointBackup.Clear();
            passedBreakpoints.Clear();
        }

        public static void UndoAll() {
            foreach (BreakPoints breakPoints in dictionary.Values) {
                breakPoints.labelEmitter.Undo();
            }
            HashPassedBreakPoints.Clear();
            latestBreakpointBackup.Clear();
            passedBreakpoints.Clear();
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
                        if (cursor.TryGotoNext(MoveType.Before, ins => ins.OpCode == OpCodes.Leave_S, ins => ins.MatchLdloca(0), ins => ins.OpCode == OpCodes.Constrained, ins => ins.MatchCallOrCallvirt<IDisposable>("Dispose"), ins => ins.OpCode == OpCodes.Endfinally)) {
                        cursor.Emit(OpCodes.Ldstr, label);
                        cursor.EmitDelegate(RecordLabelWrap);
                        cursor.EmitDelegate(()=> {
                            EntityListUpdate_Entry.SubMethodPassed = true;
                            BreakPoints.ForEachBreakPoints.Reset();
                        });
                    }
                };
                endingLabel = CreateImpl(EntityListUpdate, "EntityListUpdate end", manipulator, RetShiftDoNotEmit).UID;
            }

            internal static string endingLabel;

            private static void RecordLabelWrap(string s) {
                RecordLabel(s);
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

            public static void AddTarget(Entity entity, bool hasBreakPoints = false) {
                string str = GetID(entity);
                targets.Add(str);
                if (hasBreakPoints) {
                    targets_withBreakpoints.Add(str);
                }
            }


            [Command("oop_add_target", "Add the entity as a for-each breakpoint of the OOP stepping (TAS Helper)")]
            public static void AddTarget(string entityUID, bool hasBreakPoints = false) {
                targets.Add(entityUID);
                if (hasBreakPoints) {
                    targets_withBreakpoints.Add(entityUID);
                }
            }

            [Command("oop_remove_target", "Remove a for-each breakpoint of the OOP stepping (TAS Helper)")]
            public static void RemoveTarget(string entityUID) {
                targets.Remove(entityUID);
                targets_withBreakpoints.Remove(entityUID);
            }

            [Command("oop_show_target", "Show all for-each breakpoints of the OOP stepping (TAS Helper)")]
            public static void ShowTarget() {
                foreach (string s in targets) {
                    Celeste.Commands.Log(s);
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
                BreakPoints.passedBreakpoints.Remove(EntityUpdate_withBreakPoints_Entry.UID);
                BreakPoints.latestBreakpointBackup.Remove(OOP_Core.EntityUpdateWithBreakPoints);
                removed_targets.Add(curr_target_withBreakpoint);
            }

            public static void EntityUpdateWithoutBreakpointsDone() {
                SendText($"{curr_target_withoutBreakpoint} update end");
                BreakPoints.passedBreakpoints.Remove(EntityUpdate_withoutBreakPoints_Entry.UID);
                BreakPoints.latestBreakpointBackup.Remove(OOP_Core.EntityUpdateWithoutBreakPoints);
                removed_targets.Add(curr_target_withoutBreakpoint);
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
                    if (cursor.TryGotoNext(ins => ins.OpCode == OpCodes.Ldstr && BreakPoints.HashPassedBreakPoints.Contains((string)ins.Operand))) {
                        // for EndingBreakpoints, there maybe several different matching results, but jump to one is enough
                        Instruction target = cursor.Next;
                        string label = (string)cursor.Next.Operand;

                        bool recordRemoved = false;

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

                            recordRemoved = true;
                        }


                        jumpLog.Add($"\n {label.Replace(BreakPoints.Prefix, "")} | Finished: {recordRemoved}");

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

        public static void CreateSpecial() {
            // i have no idea, but the try-catch block is so fucky
            IDetour detour;
            MethodBase methodBase = OOP_Core.EntityListUpdate;
            using (new DetourContext { After = new List<string> { "*", "CelesteTAS-EverestInterop", "TASHelper", "TAS Helper OOP_Core BreakPoints", "TAS Helper OOP_Core Ending" }, ID = "TAS Helper OOP_Core SpringBoard" }) {
                detour = new ILHook(methodBase, il => {
                    ILCursor cursor = new(il);
                    if (BreakPoints.HashPassedBreakPoints.Contains(BreakPoints.ForEachBreakPoints.endingLabel)) {
                        cursor.Emit(OpCodes.Ret);
                        jumpLog.Add($"\n EntityListUpdate end | Finished: true");
                        return;
                    }
                    if (cursor.TryGotoNext(ins => ins.OpCode == OpCodes.Ldstr && BreakPoints.HashPassedBreakPoints.Contains((string)ins.Operand))) {
                        // for EndingBreakpoints, there maybe several different matching results, but jump to one is enough
                        Instruction target = cursor.Next;
                        string label = (string)cursor.Next.Operand;

                        bool recordRemoved = false;

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

                            recordRemoved = true;
                        }


                        jumpLog.Add($"\n {label.Replace(BreakPoints.Prefix, "")} | Finished: {recordRemoved}");

                        cursor.Goto(0);
                        cursor.Emit(OpCodes.Br, target);
                    }

                }, manualConfig);
            }
            dictionary[methodBase] = detour;
        }

        public static List<string> jumpLog = new();

        public static void RefreshAll() {
            SpringBoard.jumpLog.Clear();
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
        using (new DetourContext { After = new List<string> { "*", "CelesteTAS-EverestInterop"}, Before = new List<string> { "TASHelper" } , ID = "TAS Helper OOP_Core OnLevelRender" }){
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
            if (needGameInfoUpdate) {
                TAS.GameInfo.Update();
                needGameInfoUpdate = false;
            }
        }
    }

    private static bool needGameInfoUpdate = false;
}
