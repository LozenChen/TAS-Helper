//#define OoO_Debug

using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System.Reflection;
using static Celeste.Mod.TASHelper.OrderOfOperation.OoO_Core;

namespace Celeste.Mod.TASHelper.OrderOfOperation;

internal class SpringBoard {
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