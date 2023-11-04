//#define OoO_Debug

using Celeste.Mod.TASHelper.Entities;
using Celeste.Mod.TASHelper.Gameplay.Spinner;
using Celeste.Mod.TASHelper.Module.Menu;
using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using TAS;
using TAS.EverestInterop;

namespace Celeste.Mod.TASHelper.OrderOfOperation;
internal static class OoO_Core {

    private static readonly MethodInfo EngineUpdate = typeof(Engine).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly MethodInfo LevelUpdate = typeof(Level).GetMethod("Update");
    private static readonly MethodInfo SceneUpdate = typeof(Scene).GetMethod("Update");
    private static readonly MethodInfo EntityListUpdate = typeof(EntityList).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly MethodInfo PlayerUpdate = typeof(Player).GetMethod("Update");
    private static readonly MethodInfo PlayerOrigUpdate = typeof(Player).GetMethod("orig_Update");
    // if we add a breakpoint to A, which is called by B, then we must add breakpoints to B
    // so that any hook given by other mods are handled properly

    public static bool Applied = false;

    public static bool AutoSkippingBreakPoints = true;

    public static int StepCount { get; private set; } = 0;

#if OoO_Debug
    private static bool debugLogged = false;
#endif
    public static void Step() {
        // entry point of OoO
        if (!Applied) {
            ApplyAll();
            StepCount = 0;
#if OoO_Debug
            if (!debugLogged) {
                foreach (MethodBase method in BreakPoints.detoursOnThisMethod.Keys) {
                    //CILCodeHelper.CILCodeLogger(method, 9999);
                }
                debugLogged = true;
            }
#endif
        }
        else {
            stepping = true;
            BreakPoints.ReformHashPassedBreakPoints();
            SpringBoard.RefreshAll(); // spring board relies on passed Breakpoints, so it must be cleared later
            ResetTempState();
            StepCount++;
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
        if (overrideStepping || AutoSkippingBreakPoints && AutoSkippedBreakpoints.Contains(lastText)) {
            return true;
        }
        return false;
    }

    public static void StopFastForward() {
        if (BreakPoints.ForEachBreakPoints_EntityList.ultraFastForwarding) {
            return;
            // if we have already entered ultra fast forwarding, then we shouldn't not exit it in half way, so tas still sync
        }
        overrideStepping = false;
    }

    public static void FastForwardToEnding() {
        if (overrideStepping) {
            prepareToUltraFastForwarding = true;
        }
        overrideStepping = true;
    }

    private static bool overrideStepping = false;

    private static bool prepareToUltraFastForwarding = false;

    public static readonly HashSet<string> AutoSkippedBreakpoints = new();

    [Command_StringParameter("ooo_add_autoskip", $"Autoskip a normal breakpoint (not a for-each breakpoint)(TAS Helper)")]
    public static void AddAutoSkip(string uid) {
        if (uid.StartsWith(BreakPoints.Prefix)) {
            AutoSkippedBreakpoints.Add(uid);
        }
        else {
            AutoSkippedBreakpoints.Add($"{BreakPoints.Prefix}{uid}");
        }
    }

    [Command_StringParameter("ooo_remove_autoskip", "Stop autoskipping a normal breakpoint (TAS Helper)")]
    public static void RemoveAutoSkip(string uid) {
        if (!AutoSkippedBreakpoints.Remove(uid)) {
            AutoSkippedBreakpoints.Remove($"{BreakPoints.Prefix}{uid}");
        }
    }

    [Command("ooo_show_autoskip", "Show all autoskipped breakpoints (TAS Helper)")]
    public static void ShowAutoSkip() {
        foreach (string s in AutoSkippedBreakpoints) {
            Celeste.Commands.Log(s.Replace(BreakPoints.Prefix, ""));
        }
    }

    [Command("ooo_show_breakpoint", "Show all normal breakpoints (TAS Helper)")]
    public static void ShowBreakpoint() {
        foreach (string s in BreakPoints.dictionary.Keys) {
            Celeste.Commands.Log(s.Replace(BreakPoints.Prefix, ""));
        }
    }

    internal static string lastText = "";

    private static void SendText(string str) {
        lastText = str;
    }

    private static bool stepping = false;

    private static ILHookConfig manualConfig => Utils.HookHelper.manualConfig;

    private static readonly Action<ILCursor> NullAction = (cursor) => { };

    [Initialize]
    public static void Initialize() {
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

        BreakPoints.MarkEnding(PlayerUpdate, "PlayerUpdate end", BreakPoints.ForEachBreakPoints_EntityList.MarkSubMethodPassed).AddAutoSkip();

        BreakPoints.MarkEnding(PlayerOrigUpdate, "PlayerOrigUpdate end", () => PlayerOrigUpdate_Entry.SubMethodPassed = true);

        BreakPoints.CreateImpl(EngineUpdate, "EngineUpdate begin", label => (cursor, _) => {
            cursor.Emit(OpCodes.Ldstr, label);
            Instruction mark = cursor.Prev;
            cursor.EmitDelegate(BreakPoints.RecordLabel);
            cursor.Emit(OpCodes.Ret);

            cursor.Goto(0);
            cursor.EmitDelegate(GetStepping);
            cursor.Emit(OpCodes.Brtrue, mark);
            cursor.Emit(OpCodes.Ret);
        });

        BreakPoints.Create(EngineUpdate, "EngineUpdate_SceneBeforeUpdate begin",
            ins => ins.OpCode == OpCodes.Ldarg_0,
            ins => ins.MatchLdfld<Engine>("scene"),
            ins => ins.MatchCallOrCallvirt<Scene>("BeforeUpdate")
        ).AddAutoSkip();

        LevelUpdate_Entry = BreakPoints.CreateFull(EngineUpdate, "EngineUpdate_LevelUpdate end", 3, NullAction, NullAction,
            ins => ins.OpCode == OpCodes.Ldarg_0,
            ins => ins.MatchLdfld<Engine>("scene"),
            ins => ins.MatchCallOrCallvirt<Scene>("Update")
        ).AddAutoSkip();

        SceneUpdate_Entry = BreakPoints.CreateFull(LevelUpdate, "LevelUpdate_SceneUpdate end", 2, NullAction, NullAction,
            ins => ins.OpCode == OpCodes.Ldarg_0,
            ins => ins.MatchCallOrCallvirt<Scene>("Update"),
            ins => ins.OpCode == OpCodes.Br,
            ins => ins.OpCode == OpCodes.Ldarg_0,
            ins => ins.MatchLdfld<Level>("RetryPlayerCorpse")).AddAutoSkip();

        EntityListUpdate_Entry = BreakPoints.CreateFull(SceneUpdate, "SceneUpdate_EntityListUpdate end", 3, NullAction, NullAction,
            ins => ins.OpCode == OpCodes.Ldarg_0,
            ins => ins.MatchCallOrCallvirt<Scene>("get_Entities"),
            ins => ins.MatchCallOrCallvirt<EntityList>("Update")
        ).AddAutoSkip();

        EntityListUpdate_ForEach_Entry = BreakPoints.CreateFull(EntityListUpdate, "EntityListUpdate_ForEach entry", BreakPoints.RetShiftDoNotEmit, NullAction, NullAction,
            ins => ins.OpCode == OpCodes.Ldarg_0,
            ins => ins.MatchLdfld<EntityList>("entities"),
            ins => ins.MatchCallOrCallvirt<List<Entity>>("GetEnumerator"),
            ins => ins.OpCode == OpCodes.Stloc_0).AddAutoSkip();


        PlayerOrigUpdate_Entry = BreakPoints.CreateFull(PlayerUpdate, "PlayerUpdate_PlayerOrigUpdate end", 2, NullAction, NullAction).AddAutoSkip();

        BreakPoints.Create(PlayerOrigUpdate, "PlayerOrigUpdate begin");

        BreakPoints.Create(PlayerOrigUpdate, "PlayerOrigUpdate_ClimbHop",
            ins => ins.OpCode == OpCodes.Ldarg_0,
            ins => ins.MatchLdfld<Player>("climbHopSolid"),
            ins => ins.MatchLdfld<Entity>("Position"),
            ins => ins.OpCode == OpCodes.Ldarg_0,
            ins => ins.MatchLdfld<Player>("climbHopSolidPosition"),
            ins => ins.OpCode == OpCodes.Call
        );

        BreakPoints.Create(PlayerOrigUpdate, "PlayerOrigUpdate_BaseUpdate begin",
            ins => ins.OpCode == OpCodes.Ldarg_0,
            ins => ins.MatchCallOrCallvirt<Actor>("Update")
        );

        BreakPoints.CreateFull(PlayerOrigUpdate, "PlayerOrigUpdate_BaseUpdate end", 0, NullAction, cursor => cursor.Index += 2,
            ins => ins.OpCode == OpCodes.Ldarg_0,
            ins => ins.MatchCallOrCallvirt<Actor>("Update")
        );

        // CommunalHelper.CustomBooster has a hook nearby, which prevents player moving by herself if she is in certain CustomBoosters
        // previously this breakpoint is outside the if-block, thus affected by this CommunalHelper hook
        BreakPoints.Create(PlayerOrigUpdate, "PlayerOrigUpdate_MoveH",
            ins => ins.OpCode == OpCodes.Ldarg_0,
            ins => ins.OpCode == OpCodes.Ldarg_0,
            ins => ins.MatchLdflda<Player>("Speed"),
            ins => ins.MatchLdfld<Vector2>("X"),
            ins => ins.MatchCallOrCallvirt<Engine>("get_DeltaTime"),
            ins => ins.OpCode == OpCodes.Mul,
            ins => ins.OpCode == OpCodes.Ldarg_0,
            ins => ins.MatchLdfld<Player>("onCollideH"),
            ins => ins.OpCode == OpCodes.Ldnull,
            ins => ins.MatchCallOrCallvirt<Actor>("MoveH"),
            ins => ins.OpCode == OpCodes.Pop
        );

        BreakPoints.Create(PlayerOrigUpdate, "PlayerOrigUpdate_MoveV",
            ins => ins.OpCode == OpCodes.Ldarg_0,
            ins => ins.OpCode == OpCodes.Ldarg_0,
            ins => ins.MatchLdflda<Player>("Speed"),
            ins => ins.MatchLdfld<Vector2>("Y"),
            ins => ins.MatchCallOrCallvirt<Engine>("get_DeltaTime"),
            ins => ins.OpCode == OpCodes.Mul,
            ins => ins.OpCode == OpCodes.Ldarg_0,
            ins => ins.MatchLdfld<Player>("onCollideV"),
            ins => ins.OpCode == OpCodes.Ldnull,
            ins => ins.MatchCallOrCallvirt<Actor>("MoveV"),
            ins => ins.OpCode == OpCodes.Pop
        );

        BreakPoints.CreateFull(PlayerOrigUpdate, "PlayerOrigUpdate_CameraUpdate", 0, NullAction, cursor => { cursor.Index += 3; cursor.MoveAfterLabels(); },
            ins => ins.OpCode == OpCodes.Ldarg_0,
            ins => ins.MatchLdfld<Player>("ForceCameraUpdate"),
            ins => ins.OpCode == OpCodes.Brfalse
        ).AddAutoSkip();

        PlayerOrigUpdate_PlayerCollideCheckBegin_UID = BreakPoints.CreateFull(PlayerOrigUpdate, "PlayerOrigUpdate_PlayerColliderCheck begin", 0, NullAction, cursor => { cursor.Index += 8; cursor.MoveAfterLabels(); },
            ins => ins.OpCode == OpCodes.Ldarg_0,
            ins => ins.MatchCallOrCallvirt<Player>("get_Dead"),
            ins => ins.OpCode == OpCodes.Brtrue,
            ins => ins.OpCode == OpCodes.Ldarg_0,
            ins => ins.OpCode == OpCodes.Ldfld,
            ins => ins.MatchCallOrCallvirt<StateMachine>("get_State"),
            ins => ins.MatchLdcI4(21)
        ).UID;

        BreakPoints.Create(PlayerOrigUpdate, "PlayerOrigUpdate_LevelEnforceBounds",
            ins => ins.OpCode == OpCodes.Ldarg_0,
            ins => ins.MatchLdfld<Player>("level"),
            ins => ins.OpCode == OpCodes.Ldarg_0,
            ins => ins.MatchCallOrCallvirt<Level>("EnforceBounds")
        ).AddAutoSkip();

        BreakPoints.Create(EngineUpdate, "EngineUpdate_SceneAfterUpdate begin",
            ins => ins.OpCode == OpCodes.Ldarg_0,
            ins => ins.MatchLdfld<Engine>("scene"),
            ins => ins.MatchCallOrCallvirt<Scene>("AfterUpdate")
        ).AddAutoSkip();

        BreakPoints.ForEachBreakPoints_EntityList.Create();
        BreakPoints.ForEachBreakPoints_EntityList.MarkEndingSpecial();
        BreakPoints.ForEachBreakPoints_EntityList.AddTarget("Player", true);

        /*
         * examples:
        BreakPoints.ForEachBreakPoints.AddTarget("CrystalStaticSpinner[f-11:902]");
        BreakPoints.ForEachBreakPoints.AddTarget("BadelineBoost");
        BreakPoints.ForEachBreakPoints_EntityList.AddTarget("Each");
        */

        BreakPoints.ForEachBreakPoints_PlayerCollider.Create();
        BreakPoints.ForEachBreakPoints_PlayerCollider.AddTarget(BreakPoints.ForEachBreakPoints_PlayerCollider.autoStopString);
        /*
         * examples:
        BreakPoints.ForEachBreakPoints_PlayerCollider.AddTarget("Spring");
        */

        SpringBoard.Create(EngineUpdate);
        SpringBoard.Create(LevelUpdate);
        SpringBoard.Create(SceneUpdate);
        SpringBoard.CreateSpecial();
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
                cursor.Index += 2;
                cursor.EmitDelegate(PretendPressHotkey);
                cursor.Goto(-1);
                cursor.EmitDelegate(StopPretendPressHotkey);
                // frame advance hotkey is also used to undo OoO_Core, so we restore its value
            }
        }, manualConfig);

    }

    private static ILHook hookTASIsPaused;

    private static ILHook hookManagerUpdate;

    private static bool prepareToUndoAll = false;

    public static void ApplyAll() {
        BreakPoints.ApplyAll();
        SpringBoard.RefreshAll();
        BreakPoints.ForEachBreakPoints_EntityList.Apply();
        BreakPoints.ForEachBreakPoints_PlayerCollider.Apply();
        hookTASIsPaused.Apply();
        hookManagerUpdate.Apply();
        Applied = true;
        ResetLongtermState();
        ResetTempState();
        ActualCollideHitboxDelegatee.StopActualCollideHitbox();
        SendTextImmediately("OoO Stepping begin");
    }

    [Unload]
    public static void UndoAll() {
        SpringBoard.UndoAll();
        BreakPoints.UndoAll();
        BreakPoints.ForEachBreakPoints_EntityList.Undo();
        BreakPoints.ForEachBreakPoints_PlayerCollider.Undo();
        hookTASIsPaused.Undo();
        hookManagerUpdate.Undo();
        Applied = false;
        ResetLongtermState();
        ResetTempState();
        overrideStepping = false;
        prepareToUltraFastForwarding = false;
        ActualCollideHitboxDelegatee.RecoverActualCollideHitbox();
        SendTextImmediately("OoO Stepping end");
    }

    private static void PretendPressHotkey() {
        Utils.ReflectionExtensions.SetPropertyValue(Hotkeys.FrameAdvance, "Check", true);
        Utils.ReflectionExtensions.SetPropertyValue(Hotkeys.FrameAdvance, "LastCheck", false);
    }

    private static void StopPretendPressHotkey() {
        Utils.ReflectionExtensions.SetPropertyValue(Hotkeys.FrameAdvance, "Check", false);
    }

    private static void ResetLongtermState() {
        prepareToUndoAll = false;
        LevelUpdate_Entry.SubMethodPassed = false;
        SceneUpdate_Entry.SubMethodPassed = false;
        EntityListUpdate_Entry.SubMethodPassed = false;
        EntityListUpdate_ForEach_Entry.SubMethodPassed = false;
        PlayerOrigUpdate_Entry.SubMethodPassed = false;
        BreakPoints.latestBreakpointBackup.Clear();
        BreakPoints.ForEachBreakPoints_EntityList.Reset();
        BreakPoints.ForEachBreakPoints_PlayerCollider.Reset();
    }
    private static void ResetTempState() {
        BreakPoints.ForEachBreakPoints_EntityList.ResetTemp();
        BreakPoints.ForEachBreakPoints_PlayerCollider.ResetTemp();
        BreakPoints.passedBreakpoints.Clear();
    }

    private static BreakPoints LevelUpdate_Entry;
    private static BreakPoints SceneUpdate_Entry;
    private static BreakPoints EntityListUpdate_Entry;
    private static BreakPoints EntityListUpdate_ForEach_Entry;
    private static BreakPoints PlayerOrigUpdate_Entry;

    private class BreakPoints {

        // if a BreakPoint has subBreakPoints, make sure it's exactly before the method call, and emit Ret exactly after the method call
        // also, make sure if we jump to a breakpoint from start, the stack behavior is ok (i.e. no temp variable lives from before a breakpoint to after it)

        public const string Prefix = "TAS Helper OoO_Core::";

        public static readonly Dictionary<string, BreakPoints> dictionary = new();

        public static readonly HashSet<string> HashPassedBreakPoints = new HashSet<string>();

        public static readonly Dictionary<MethodBase, string> latestBreakpointBackup = new();

        public static readonly List<string> passedBreakpoints = new();

        public static readonly Dictionary<MethodBase, HashSet<BreakPoints>> detoursOnThisMethod = new();

#if OoO_Debug
        public static readonly HashSet<string> failedHooks = new();
#endif

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
#if OoO_Debug
                else {
                    failedHooks.Add($"\n {label}");
                }
#endif
            };
            return CreateImpl(method, label, manipulator, RetShift);
        }

        internal static BreakPoints CreateImpl(MethodBase method, string label, Func<string, Action<ILCursor, ILContext>> manipulator, int RetShift = 0) {
            string ID = CreateUID(label);
            IDetour detour;
            using (new DetourContext { After = new List<string> { "*", "CelesteTAS-EverestInterop", "TASHelper", "TAS Helper OoO_Core Ending" }, ID = "TAS Helper OoO_Core BreakPoints" }) {
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
            } while (dictionary.ContainsKey(result));
            return result;
        }
        internal static void RecordLabel(string label) {
            passedBreakpoints.Add(label);
            SendText(label); // if several labels are recorded in same frame, then the last one will be the output
        }

        public BreakPoints AddAutoSkip() {
            AutoSkippedBreakpoints.Add(this.UID);
            return this;
        }

        public BreakPoints RemoveAutoSkip() {
            AutoSkippedBreakpoints.Remove(this.UID);
            return this;
        }

        public static BreakPoints MarkEnding(MethodBase method, string label, Action? afterRetAction = null, bool EmitRet = true, MoveType moveType = MoveType.AfterLabel) {
            string ID = CreateUID(label);
            IDetour detour;
            using (new DetourContext { After = new List<string> { "*", "CelesteTAS-EverestInterop", "TASHelper" }, ID = "TAS Helper OoO_Core Ending" }) {
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
            ForEachBreakPoints_EntityList.Dispose();
        }

        public static class ForEachBreakPoints_EntityList {

            private static readonly HashSet<string> targets = new();

            private static readonly HashSet<string> targets_withBreakpoints = new();

            private static readonly HashSet<Entity> removed_targets = new();

            internal static string curr_target_withBreakpoint;

            internal static string curr_target_withoutBreakpoint;

            private static int passed_targets = 0;

            private static int expected_passed_targets = 0;

            internal static bool ultraFastForwarding = false;

            public const string eachString = "Each";

            private static bool checkEach = false;

            private static IDetour detour;

            internal static void Create() {
                using (new DetourContext { Before = new List<string> { "*", "CelesteTAS-EverestInterop", "TASHelper" }, ID = "TAS Helper OoO_Core ForEachBreakPoints_EntityList" }) {
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
                                cursor.EmitDelegate(IsRunNormally);
                                cursor.Emit(OpCodes.Brtrue, Ins_run_normally);
                                cursor.Emit(OpCodes.Ldloc_1);
                                cursor.EmitDelegate(TargetEntityUpdate);
                                cursor.Emit(OpCodes.Leave_S, Ins_ret);
                            }
#if OoO_Debug
                            else {
                                failedHooks.Add($"\n ForEachBreakPoints_EntityList");
                            }
#endif

                        }
                    }, manualConfig);
                }
            }

            private static string localvar_entityId;

            private static bool localvar_contain;

            internal static void MarkEndingSpecial() {
                // instead of emit this at the Ret, we emit it at the Leave_S which points to Ret (which exits the try-block)
                // cause it seems hard to emit before Ret (maybe some issue with try-catch-finally block, idk)

                Func<string, Action<ILCursor, ILContext>> manipulator = (label) => (cursor, _) => {
                    if (cursor.TryGotoNext(MoveType.Before, ins => ins.OpCode == OpCodes.Leave_S, ins => ins.MatchLdloca(0), ins => ins.OpCode == OpCodes.Constrained, ins => ins.MatchCallOrCallvirt<IDisposable>("Dispose"), ins => ins.OpCode == OpCodes.Endfinally)) {
                        cursor.Emit(OpCodes.Ldstr, label);
                        cursor.EmitDelegate(RecordLabelWrapper);
                        cursor.EmitDelegate(() => {
                            EntityListUpdate_Entry.SubMethodPassed = true;
                            BreakPoints.ForEachBreakPoints_EntityList.Reset();
                        });
                    }
                };
                endingLabel = CreateImpl(EntityListUpdate, "EntityListUpdate end", manipulator, RetShiftDoNotEmit).AddAutoSkip().UID;
            }

            internal static string endingLabel;

            private static void RecordLabelWrapper(string str) {
                RecordLabel(str);
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

            public static void AddTarget(string entityUID, bool hasBreakPoints = false) {
                if (entityUID == eachString) {
                    checkEach = true;
                    return;
                }

                targets.Add(entityUID);
                if (hasBreakPoints) {
                    targets_withBreakpoints.Add(entityUID);
                }
            }

            [Command_StringParameter("ooo_add_target", "Add the entity as a for-each breakpoint in EntityList.Update() (TAS Helper OoO Stepping)")]
            public static void CmdAddTarget(string UID) {
                if (UID.StartsWith("Player") && (UID == "Player" || (player is not null && UID == GetUID(player)))) {
                    AddTarget("Player", true);
                }
                else {
                    AddTarget(UID, false);
                }
                // it's not easy to add a target with breakpoints via cmd (and unncessary), so i only provide this
            }

            [Command_StringParameter("ooo_remove_target", "Remove a for-each breakpoint in EntityList.Update() (TAS Helper OoO Stepping)")]
            public static void RemoveTarget(string UID) {
                if (UID == eachString) {
                    checkEach = false;
                    return;
                }
                targets.Remove(UID);
                targets_withBreakpoints.Remove(UID);
            }

            [Command("ooo_show_target", "Show all for-each breakpoints in EntityList.Update() (TAS Helper OoO Stepping)")]
            public static void ShowTarget() {
                foreach (string s in targets) {
                    Celeste.Commands.Log(s);
                }
                if (checkEach) {
                    Celeste.Commands.Log(eachString);
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
                detour?.Dispose();
            }

            public static void Reset() {
                removed_targets.Clear();
                expected_passed_targets = 0;
                passed_targets = 0;
                ultraFastForwarding = false;
                firstLoop = true;
                flag1 = flag2 = true;
            }

            public static void ResetTemp() {
                passed_targets = 0;
                trackedEntity = null;
            }

            private static void EntityUpdateWithBreakPoints(Entity entity) {
                if (flag1) {
                    trackedEntity = entity;
                    expected_passed_targets++;
                    entity._PreUpdate();
                    flag1 = false;
                }
                if (entity.Active) {
                    entity.Update();
                    if (flag2) {
                        return;
                    }
                }
                entity._PostUpdate();
                /*
                 * EntityUpdate with BreakPoints are added manually, and will have a MarkEnding to call MarkSubMethodPassed()
                 * So there's already a SendText call
                */
                flag1 = flag2 = true;
                removed_targets.Add(entity);
            }

            private static bool flag1 = true;
            private static bool flag2 = true;

            public static void MarkSubMethodPassed() {
                flag2 = false;
            }

            private static bool firstLoop = true;
            private static void EntityUpdateWithoutBreakPoints(Entity entity) {
                if (firstLoop) {
                    expected_passed_targets++;
                    if (!checkEach) { 
                        SendText($"{curr_target_withoutBreakpoint} Update begin");
                        firstLoop = false;
                        return;
                    }
                }


                entity._PreUpdate();
                if (entity.Active) {
                    entity.Update();
                }
                entity._PostUpdate();


                firstLoop = true;
                if (prepareToUltraFastForwarding) {
                    ultraFastForwarding = true;
                }
                if (checkEach) {
                    SendText($"{curr_target_withoutBreakpoint} Update");
                }
                else{
                    SendText($"{curr_target_withoutBreakpoint} Update end");
                }
                removed_targets.Add(entity);
            }

            public static bool CheckContain(Entity entity, out string id) {
                string ID = GetID(entity);
                id = ID;
                if (entity.GetEntityData()?.ToEntityId().ToString() is { } entityID) {
                    id = $"{entity.GetType().Name}[{entityID}]";
                }
                return checkEach || targets.Contains(id) || targets.Contains(ID);
            }

            private static bool IsGotoContinue(Entity entity) {
                if (localvar_contain = CheckContain(entity, out localvar_entityId)) {
                    passed_targets++;
                    if (removed_targets.Contains(entity)) {
                        return true;
                    }
                }
                return passed_targets < expected_passed_targets;
            }

            private static bool IsRunNormally() {
                return ultraFastForwarding || !localvar_contain;
            }

            private static void TargetEntityUpdate(Entity entity) {
                if (targets_withBreakpoints.Contains(GetID(entity)) || targets_withBreakpoints.Contains(localvar_entityId)) {
                    /*
                     * EntityUpdate with BreakPoints are added manually, and will usually mark the beginning
                     * so we don't need to send text here
                     */
                    curr_target_withBreakpoint = localvar_entityId;
                    EntityUpdateWithBreakPoints(entity);
                }
                else {
                    trackedEntity = entity;
                    curr_target_withoutBreakpoint = localvar_entityId;
                    EntityUpdateWithoutBreakPoints(entity);
                }
            }

            internal static Entity trackedEntity;
        }


        public static class ForEachBreakPoints_PlayerCollider {

            private static readonly HashSet<string> targets = new();

            private static readonly HashSet<PlayerCollider> removed_targets = new();

            internal static string curr_target;

            private static int passed_targets = 0;

            private static int expected_passed_targets = 0;

            internal static bool ultraFastForwarding = false;

            public const string eachString = "Each";

            public const string autoStopString = "Auto";

            private static IDetour detour;

            internal static void Create() {
                using (new DetourContext { Before = new List<string> { "*", "CelesteTAS-EverestInterop", "TASHelper" }, ID = "TAS Helper OoO_Core ForEachBreakPoints_PlayerCollider" }) {
                    detour = new ILHook(PlayerOrigUpdate, il => {
                    ILCursor cursor = new ILCursor(il);
                        cursor.TryGotoNext(
                        ins => ins.OpCode == OpCodes.Ldarg_0,
                        ins => ins.MatchCallOrCallvirt<Player>("get_Dead"),
                        ins => ins.OpCode == OpCodes.Brtrue,
                        ins => ins.OpCode == OpCodes.Ldarg_0,
                        ins => ins.OpCode == OpCodes.Ldfld,
                        ins => ins.MatchCallOrCallvirt<StateMachine>("get_State"),
                        ins => ins.MatchLdcI4(21));
                        cursor.Index += 7;
                        cursor.Next.MatchBeq(out ILLabel normalExitTarget);
                        
                        if (cursor.TryGotoNext(MoveType.AfterLabel,
                            ins => ins.OpCode == OpCodes.Ldarg_0,
                            ins => ins.MatchCallOrCallvirt<Entity>("get_Collider"),
                            ins => ins.OpCode == OpCodes.Stloc_S,
                            ins => ins.OpCode == OpCodes.Ldarg_0,
                            ins => ins.OpCode == OpCodes.Ldarg_0,
                            ins => ins.MatchLdfld<Player>("hurtbox"),
                            ins => ins.MatchCallOrCallvirt<Entity>("set_Collider")
                        )) {
                            cursor.Emit(OpCodes.Ldarg_0);
                            cursor.EmitDelegate(PCCheck);
                            cursor.Emit(OpCodes.Brtrue, normalExitTarget);
                            cursor.Emit(OpCodes.Ret);
                        }
#if OoO_Debug
                        else {
                            failedHooks.Add($"\n ForEachBreakPoints_PlayerCollider");
                        }
#endif
                    }, manualConfig);
                }
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private static bool PCCheck(Player player) {
                if (leaveInNextLoop) {
                    leaveInNextLoop = false;
                    return true;
                }
                switch (PCCheckCore(player)) {
                    case ReturnType.EarlyReturn: {
                        RecoverHitbox(player);
                        return false;
                    }
                    case ReturnType.DeathReturn: {
                        CompleteEvent();
                        return false;
                    }
                    case ReturnType.LateReturn: {
                        CompleteEvent();
                        return false;
                    }
                }
                return true;
            }

            private static bool leaveInNextLoop = false;

            private static bool firstEnter = true;
            private static void CompleteEvent() {
                SendText("PlayerOrigUpdate_PlayerColliderCheck end");
                Reset();
                leaveInNextLoop = true;
            }

            private static void RecoverHitbox(Player player) {
                if (player.Collider == player.hurtbox) {
                    player.Collider = stored_Hitbox;
                    recoverHurtbox = true;
                }
            }

            private static bool recoverHurtbox = false;

            private enum ReturnType { EarlyReturn, LateReturn, DeathReturn };
            private static ReturnType PCCheckCore(Player player) {
                if (firstEnter) {
                    firstEnter = false;
                    stored_Hitbox = player.Collider;
                    player.Collider = player.hurtbox;
                }
                else if (recoverHurtbox) {
                    player.Collider = player.hurtbox;
                    recoverHurtbox = false;
                }

                foreach (PlayerCollider pc in player.Scene.Tracker.GetComponents<PlayerCollider>()) {
                    if (IsGotoContinue(pc, out string entityId, out bool contain)) {
                        continue;
                    }
                    if (ultraFastForwarding) {
                        if (pc.Check(player) && player.Dead) {
                            player.Collider = stored_Hitbox;
                            trackedPC = pc;
                            return ReturnType.DeathReturn;
                        }
                    }
                    else if (!contain) {
                        if (pc.Check(player)) {
                            trackedPC = pc;
                            if (player.Dead) {
                                player.Collider = stored_Hitbox;
                                return ReturnType.DeathReturn;
                            }
                            else if (autoStop) {
                                SendText($"{entityId}'s PlayerCollider check");
                                removed_targets.Add(pc);
                                if (prepareToUltraFastForwarding) {
                                    ultraFastForwarding = true;
                                }
                                return ReturnType.EarlyReturn;
                            }
                        }
                    }
                    else {
                        curr_target = entityId;
                        trackedPC = pc;
                        if (waitForNextLoop) {
                            expected_passed_targets++;
                            SendText($"{entityId}'s PlayerCollider check begin");
                            waitForNextLoop = false;
                            return ReturnType.EarlyReturn;
                        }
                        else {
                            waitForNextLoop = true;
                            if (pc.Check(player) && player.Dead) {
                                player.Collider = stored_Hitbox;
                                return ReturnType.DeathReturn;
                            }
                            else {
                                SendText($"{entityId}'s PlayerCollider check end");
                                removed_targets.Add(pc);
                                if (prepareToUltraFastForwarding) {
                                    ultraFastForwarding = true;
                                }
                                return ReturnType.EarlyReturn;
                            }
                        }
                    }
                }
                if (player.Collider == player.hurtbox) {
                    player.Collider = stored_Hitbox;
                }
                return ReturnType.LateReturn;
            }

            private static Collider stored_Hitbox = new Hitbox(8f, 11f, -4f, -11f);

            private static bool waitForNextLoop = true;

            private static bool autoStop = false;

            private static bool checkEach = false;

            internal static PlayerCollider trackedPC;

            public static bool CheckContain(Entity entity, out string id) {
                string ID = entity.GetType().Name;
                id = ID;
                if (entity.GetEntityData()?.ToEntityId().ToString() is { } entityID) {
                    id = $"{entity.GetType().Name}[{entityID}]";
                }
                return checkEach || targets.Contains(id) || targets.Contains(ID);
            }

            private static bool IsGotoContinue(PlayerCollider pc, out string entityId, out bool contain) {
                if (pc.Entity is not Entity entity) {
                    entityId = "";
                    contain = false;
                    return false;
                }
                if (contain = CheckContain(entity, out entityId)) {
                    passed_targets++;
                }
                if (removed_targets.Contains(pc)) { // removed_targets may be added by autostop (thus may not pass CheckContain), so we move it out here
                    return true;
                }
                return passed_targets < expected_passed_targets;
            }


            [Command_StringParameter("ooo_add_target_pc", "Add the entity as a for-each breakpoint in PlayerCollider checks (TAS Helper OoO Stepping)")]
            public static void AddTarget(string UID) {
                if (UID == autoStopString) {
                    autoStop = true;
                }
                else if (UID == eachString) {
                    checkEach = true;
                }
                else {
                    targets.Add(UID);
                }

                if (haveTryToApply && !applied) {
                    detour.Apply();
                    Reset();
                    applied = true;
                }
            }

            [Command_StringParameter("ooo_remove_target_pc", "Remove a for-each breakpoint in PlayerCollider checks (TAS Helper OoO Stepping)")]
            public static void RemoveTarget(string UID) {
                if (UID == autoStopString) {
                    autoStop = false;
                }
                else if (UID == eachString) {
                    checkEach = false;
                }
                else { 
                    targets.Remove(UID);
                }

                if (!ShouldApply && applied) {
                    detour.Undo();
                    Reset();
                    applied = false;
                }
            }

            [Command("ooo_show_target_pc", "Show all for-each breakpoints in PlayerCollider checks (TAS Helper OoO Stepping)")]
            public static void ShowTarget() {
                foreach (string s in targets) {
                    Celeste.Commands.Log(s);
                }
                if (checkEach) {
                    Celeste.Commands.Log(eachString) ;
                }
                if (autoStop) {
                    Celeste.Commands.Log(autoStopString);
                }
            }

            private static bool haveTryToApply = false;
            internal static bool applied = false;

            public static bool ShouldApply => targets.IsNotNullOrEmpty() || checkEach || autoStop;
    
            public static void Apply() {
                if (ShouldApply) {
                    detour.Apply();
                    applied = true;
                }
                haveTryToApply = true;
                Reset();
            }

            public static void Undo() {
                haveTryToApply = false;
                applied = false;
                detour.Undo();
                Reset();
            }

            [Unload]
            public static void Dispose() {
                detour?.Dispose();
            }

            public static void Reset() {
                removed_targets.Clear();
                expected_passed_targets = 0;
                passed_targets = 0;
                ultraFastForwarding = false;
                stored_Hitbox = null;
                waitForNextLoop = true;
                leaveInNextLoop = false;
                firstEnter = true;
                recoverHurtbox = false;
            }

            public static void ResetTemp() {
                passed_targets = 0;
                trackedPC = null;
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
            using (new DetourContext { After = new List<string> { "*", "CelesteTAS-EverestInterop", "TASHelper", "TAS Helper OoO_Core BreakPoints", "TAS Helper OoO_Core Ending" }, ID = "TAS Helper OoO_Core SpringBoard" }) {
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

#if OoO_Debug
                        jumpLog.Add($"\n {label.Replace(BreakPoints.Prefix, "")} | Finished: {recordRemoved}");
#endif
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

        internal static void CreateSpecial() {
            // i have no idea, but the try-catch block is so fucky
            IDetour detour;
            MethodBase methodBase = OoO_Core.EntityListUpdate;
            using (new DetourContext { After = new List<string> { "*", "CelesteTAS-EverestInterop", "TASHelper", "TAS Helper OoO_Core BreakPoints", "TAS Helper OoO_Core Ending" }, ID = "TAS Helper OoO_Core SpringBoard" }) {
                detour = new ILHook(methodBase, il => {
                    ILCursor cursor = new(il);
                    if (BreakPoints.HashPassedBreakPoints.Contains(BreakPoints.ForEachBreakPoints_EntityList.endingLabel)) {
                        cursor.Emit(OpCodes.Ret);
#if OoO_Debug
                        jumpLog.Add($"\n EntityListUpdate end | Finished: True");
#endif
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

#if OoO_Debug
                        jumpLog.Add($"\n {label.Replace(BreakPoints.Prefix, "")} | Finished: {recordRemoved}");
#endif
                        cursor.Goto(0);
                        cursor.Emit(OpCodes.Br, target);
                    }

                }, manualConfig);
            }
            dictionary[methodBase] = detour;
        }

#if OoO_Debug
        public static List<string> jumpLog = new();
#endif

        public static void RefreshAll() {
#if OoO_Debug
            SpringBoard.jumpLog.Clear();
#endif
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
        using (new DetourContext { After = new List<string> { "*", "CelesteTAS-EverestInterop" }, Before = new List<string> { "TASHelper" }, ID = "TAS Helper OoO_Core OnLevelRender" }) {
            On.Celeste.Level.Render += OnLevelRender;
        }
        On.Monocle.EntityList.DebugRender += PatchEntityListDebugRender;
    }

    [Unload]
    public static void Unload() {
        On.Celeste.Level.Render -= OnLevelRender;
        On.Monocle.EntityList.DebugRender -= PatchEntityListDebugRender;
    }
    private static void OnLevelRender(On.Celeste.Level.orig_Render orig, Level self) {
        if (Applied) {
            if (prepareToUndoAll) {
                UndoAll();
            }
            else {
                if (lastText == PlayerOrigUpdate_PlayerCollideCheckBegin_UID && !BreakPoints.ForEachBreakPoints_PlayerCollider.applied) {
                    SendTextImmediately("PlayerOrigUpdate_PlayerColliderCheck");
                }
                else if (lastText != "") {
                    SendTextImmediately(lastText.Replace(BreakPoints.Prefix, ""));
                }
            }

            Hotkeys.Update(); // TH_Hotkeys relies on Hotkeys, so it needs update

            if (needGameInfoUpdate) {
                TAS.GameInfo.Update();
                needGameInfoUpdate = false;
            }
        }
        orig(self);
    }

    private static bool allowHotkey => Applied || TasHelperSettings.EnableOoO;

    public static bool TryHardExit = true;
    internal static void OnHotkeysPressed() {
        if (Applied && (Hotkeys.FrameAdvance.Pressed || Hotkeys.SlowForward.Pressed || Hotkeys.PauseResume.Pressed || Hotkeys.StartStop.Pressed)) {
            // in case the user uses OoO unconsciously, and do not know how to exit OoO, we allow user to exit using normal tas hotkeys, as if we were running a tas
            FastForwardToEnding();
        }
        else if (TH_Hotkeys.OoO_Fastforward_Hotkey.Pressed) {
            if (!allowHotkey) {
                SendTextImmediately("Order-of-Operation stepping NOT Enabled");
            }
            else if (!Applied && TAS.Manager.Running && !FrameStep) {
                SendTextImmediately("TAS is running, refuse to OoO step");
            }
            else {
                FastForwardToEnding();
            }
        }
        else if (TH_Hotkeys.OoO_Step_Hotkey.Pressed) {
            if (!allowHotkey) {
                SendTextImmediately("Order-of-Operation stepping NOT Enabled");
            }
            else if (TAS.Manager.Running && !FrameStep) {
                SendTextImmediately("TAS is running, refuse to OoO step");
            }
            else {
                StopFastForward();
                Step();
            }
        }
        else if (TryAutoSkip()) {
            Step();
        }
        lastText = ""; // lastText is needed in TryAutoSkip, that's why we clear it now instead of in OnLevelRender

    }

    private static void SendTextImmediately(string str) {
        HotkeyWatcher.Refresh(str);
    }

    private static bool needGameInfoUpdate = false;

    private static string PlayerOrigUpdate_PlayerCollideCheckBegin_UID;

    private static void PatchEntityListDebugRender(On.Monocle.EntityList.orig_DebugRender orig, EntityList self, Camera camera) {
        orig(self, camera);
        if (Applied) {
            OoO_Highlighter.UpdateTime();
            OoO_Highlighter.DebugRender(camera);
        }
    }
    private static class OoO_Highlighter{

        public static float oscillator => Monocle.Calc.YoYo(time);

        public static float time = 0f;

        public static float speed = 0.033f;

        public static void UpdateTime() {
            time += speed;
            if (time > 1f) {
                time -= 1f;
            }
        }

        public static Entity trackedEntity => BreakPoints.ForEachBreakPoints_EntityList.trackedEntity;

        public static PlayerCollider trackedPC => BreakPoints.ForEachBreakPoints_PlayerCollider.trackedPC;

        public static Color HighLightColor => Color.Lerp(Color.Cyan, Color.Red, oscillator);

        public static void DebugRender(Camera camera) {
            // originally, i debugrender this via an entity, whose depth is the trackedEntity/PC's depth -1
            // however, if we do so, then we need to call EntityList.UpdateList()
            // in some worst cases, this will lead to Entities removed from scene
            // thus interfere the for-each breakpoints running
            // e.g. an entity which updates before Player, and become removed, will make Player's update only called once
            if (trackedPC?.Entity is { } entity) {
                Collider collider = trackedPC.Collider;
                if (trackedPC.FeatherCollider != null && player is not null && player.StateMachine.state == 19) {
                    collider = trackedPC.FeatherCollider;
                }
                collider ??= entity.Collider;
                collider?.Render(camera, HighLightColor);
            }
            else if (trackedEntity is not null) {
                if (trackedEntity.Collider is not null) {
                    trackedEntity.Collider.Render(camera, HighLightColor);
                }
                else {
                    Monocle.Draw.Point(trackedEntity.Position, HighLightColor);
                }
            }
        }
    }
}
