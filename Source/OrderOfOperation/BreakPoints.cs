//#define OoO_Debug

using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System.Reflection;
using static Celeste.Mod.TASHelper.OrderOfOperation.OoO_Core;

namespace Celeste.Mod.TASHelper.OrderOfOperation;

internal class BreakPoints {

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

    public ILHook labelEmitter;

    public MethodBase method;

    public bool? SubMethodPassed = null;

    internal const int RetShiftDoNotEmit = -100;

    private BreakPoints(string ID, ILHook detour, MethodBase method) {
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
        DetourConfig config = DetourContextHelper.Create(After: new List<string> { "*", "CelesteTAS-EverestInterop", "TASHelper", "TAS Helper OoO_Core Ending" }, ID: "TAS Helper OoO_Core BreakPoints");
        ILHook detour = HookHelper.ManualAppliedILHook(method, il => {
            ILCursor cursor = new(il);
            manipulator(ID)(cursor, il);
        }, config);
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
        DetourConfig config = DetourContextHelper.Create(After: new List<string> { "*", "CelesteTAS-EverestInterop", "TASHelper" }, ID: "TAS Helper OoO_Core Ending");
        ILHook detour = HookHelper.ManualAppliedILHook(method, il => {
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
        }, config);
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
    }
}


