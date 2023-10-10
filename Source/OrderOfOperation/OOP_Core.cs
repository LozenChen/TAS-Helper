using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using MonoMod.RuntimeDetour;
using System.Reflection;
using Celeste.Mod.TASHelper.Entities;
using TAS;
using Celeste.Mod.TASHelper.Utils;
using System.Collections.Generic;

namespace Celeste.Mod.TASHelper.OrderOfOperation;
internal static class OOP_Core {

    private static MethodInfo EngineUpdate = typeof(Engine).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
    private static MethodInfo LevelUpdate = typeof(Level).GetMethod("Update");
    private static MethodInfo EntityListUpdate = typeof(EntityList).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
    private static MethodInfo PlayerUpdate = typeof(Player).GetMethod("Update");
    private static MethodInfo PlayerOrigUpdate = typeof(Player).GetMethod("orig_Update");

    public static bool Applied = false;

    public static void Step() {
        if (prepareToUndoAll) {
            UndoAll();
            prepareToUndoAll = false;
            return;
        }
        if (!Applied) {
            ApplyAll();
        }
        else {
            stepping = true;
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

    [Initialize]
    public static void Initialize() {
        manualConfig.ManualApply = true;

        BreakPoints.Create(EngineUpdate, "EngineUpdate_Start", label => (cursor, _) => {
            cursor.Emit(OpCodes.Ldstr, label);
            Instruction mark = cursor.Prev;
            cursor.Emit(OpCodes.Pop);

            cursor.Goto(0);
            cursor.EmitDelegate(GetStepping);
            cursor.Emit(OpCodes.Brtrue, mark);
            cursor.Emit(OpCodes.Ret);
        });
        
        BreakPoints.Create(EngineUpdate, "EngineUpdate_SceneBeforeUpdate_Start", label => (cursor, _) => {
            if (cursor.TryGotoNext(MoveType.AfterLabel,
                ins => ins.OpCode == OpCodes.Ldarg_0,
                ins => ins.MatchLdfld<Engine>("scene"),
                ins => ins.MatchCallOrCallvirt<Scene>("BeforeUpdate")
                )) {
                cursor.Emit(OpCodes.Ldstr, label);
                cursor.Emit(OpCodes.Pop);
                // we add these pops so the hook works
                // and remove these pops when we create springBoards
            }
        });
        BreakPoints SceneUpdate_Start = BreakPoints.Create(EngineUpdate, "EngineUpdate_SceneUpdate_Start", label => (cursor, _) => {
            if (cursor.TryGotoNext(MoveType.AfterLabel,
                ins => ins.OpCode == OpCodes.Ldarg_0,
                ins => ins.MatchLdfld<Engine>("scene"),
                ins => ins.MatchCallOrCallvirt<Scene>("Update")
                )) {
                cursor.Emit(OpCodes.Ldstr, label);
                cursor.Emit(OpCodes.Pop);
            }
        });
        BreakPoints.Create(EngineUpdate, "EngineUpdate_SceneAfterUpdate_Start", label => (cursor, _) => {
            if (cursor.TryGotoNext(MoveType.AfterLabel,
                ins => ins.OpCode == OpCodes.Ldarg_0,
                ins => ins.MatchLdfld<Engine>("scene"),
                ins => ins.MatchCallOrCallvirt<Scene>("AfterUpdate")
                )) {
                cursor.Emit(OpCodes.Ldstr, label);
                cursor.Emit(OpCodes.Pop);
            }
        });
        BreakPoints.Create(PlayerOrigUpdate, "PlayerOrigUpdate_Start", label => (cursor, _) => {
            cursor.Emit(OpCodes.Ldstr, label);
            cursor.Emit(OpCodes.Pop);
        });

        BreakPoints.Create(PlayerOrigUpdate, "PlayerOrigUpdate_BaseUpdate", label => (cursor, _) => {
            if (cursor.TryGotoNext(MoveType.AfterLabel,
                ins => ins.OpCode == OpCodes.Ldarg_0,
                ins => ins.MatchCallOrCallvirt<Actor>("Update")
                )) {
                cursor.Emit(OpCodes.Ldstr, label);
                cursor.Emit(OpCodes.Pop);
            }
        });

        BreakPoints.Create(PlayerOrigUpdate, "PlayerOrigUpdate_MoveH", label => (cursor, _) => {
            if (cursor.TryGotoNext(
                ins => ins.OpCode == OpCodes.Ldarg_0,
                ins => ins.OpCode == OpCodes.Ldc_I4_0,
                ins => ins.MatchCallOrCallvirt<Player>("set_Ducking"),
                ins => ins.OpCode == OpCodes.Ldarg_0,
                ins => ins.OpCode == OpCodes.Ldfld,
                ins => ins.MatchCallOrCallvirt<StateMachine>("get_State"),
                ins => ins.MatchLdcI4(9)
                )) {
                cursor.Index += 3;
                cursor.MoveAfterLabels();
                cursor.Emit(OpCodes.Ldstr, label);
                cursor.Emit(OpCodes.Pop);
            }
        });

        BreakPoints.Create(PlayerOrigUpdate, "PlayerOrigUpdate_EntityCollide", label => (cursor, _) => {
            if (cursor.TryGotoNext(MoveType.AfterLabel,
                ins => ins.OpCode == OpCodes.Ldarg_0,
                ins => ins.MatchCallOrCallvirt<Player>("get_Dead"),
                ins => ins.OpCode == OpCodes.Brtrue,
                ins => ins.OpCode == OpCodes.Ldarg_0,
                ins => ins.OpCode == OpCodes.Ldfld,
                ins => ins.MatchCallOrCallvirt<StateMachine>("get_State"),
                 ins => ins.MatchLdcI4(21)
                )) {
                cursor.Emit(OpCodes.Ldstr, label);
                cursor.Emit(OpCodes.Pop);
            }
        });

        SpringBoard.Refresh(EngineUpdate);// this is based on breakpoints, so it must be refreshed after breakpoints hooks applied
        SpringBoard.Refresh(PlayerOrigUpdate);

        BreakPoints.AssignAsSubmethod(SceneUpdate_Start, PlayerOrigUpdate);
        BreakPoints.BuildSubBreakPoints(EngineUpdate);

        hookTASIsPaused = new ILHook(typeof(TAS.EverestInterop.Core).GetMethod("IsPause", BindingFlags.NonPublic | BindingFlags.Static), il => {
            ILCursor cursor = new ILCursor(il);
            cursor.Emit(OpCodes.Ldc_I4_0);
            cursor.Emit(OpCodes.Ret);
        }, manualConfig);
    }

    private static ILHook hookTASIsPaused;

    public static void ApplyAll() {
        BreakPoints.ApplyAll();
        SpringBoard.ApplyAll();
        hookTASIsPaused.Apply();
        Applied = true;
        SendText("OOP Stepping start");
    }

    public static void UndoAll() {
        BreakPoints.UndoAll();
        SpringBoard.UndoAll();
        hookTASIsPaused.Undo();
        Applied = false;
        SendText("OOP Stepping end");
    }

    private static void PrepareToUndoAll() {
        prepareToUndoAll = true;
    }

    private static bool prepareToUndoAll = false;

    private class BreakPoints {

        // convention: starting breakpoint is necessary, while ending breakpoint is unnecessary (will be auto generated). So if we have N breakpoints, we have N parts of codes to execute
        // if a BreakPoint has subBreakPoints, make sure it's exactly before the method call
        // also, make sure the breakpoint is always reachable (i.e., not in a IF sentence, or no early return)
        // or add hooks to stop OOP_Core if that early return happens
        public const string Prefix = "TAS Helper OOP_Core";

        public static readonly Dictionary<string, BreakPoints> dictionary = new();

        public static readonly HashSet<string> passedBreakPoints = new HashSet<string>();

        public static readonly Dictionary<MethodBase, HashSet<BreakPoints>> detoursOnThisMethod = new();

        public static readonly Dictionary<BreakPoints, MethodBase> subMethodOfBreakpoints = new();

        public string UID;

        public IDetour labelEmitter;

        public HashSet<string> subBreakPointsID = new();

        private int depth = 0;

        public static bool BreakPointIsFinished(string id) {
            if (!dictionary.ContainsKey(id)) {
                return false;
            }
            bool b = passedBreakPoints.Contains(id) && passedBreakPoints.IsSupersetOf(dictionary[id].subBreakPointsID);
            passedBreakPoints.Add(id);
            SendText(id);
            return b;
        }
        private BreakPoints(string ID, IDetour detour) {
            UID = ID;
            labelEmitter = detour;
            dictionary.Add(UID, this);
        }

        public static void AssignAsSubmethod(BreakPoints breakPoints, MethodBase methodBase) {
            subMethodOfBreakpoints[breakPoints] = methodBase;
        }

        public static void BuildSubBreakPoints(MethodBase rootMethod) {
            foreach (BreakPoints point in dictionary.Values) {
                point.depth = 0;
                point.subBreakPointsID.Clear();
            }
            int currentDepth = 0;
            IEnumerable<BreakPoints> CurrDepth = detoursOnThisMethod[rootMethod].Intersect(subMethodOfBreakpoints.Keys);
            HashSet<BreakPoints> NextDepth = new HashSet<BreakPoints>();
            do {
                foreach (BreakPoints breakPointsCurr in CurrDepth) {
                    NextDepth.Union(detoursOnThisMethod[subMethodOfBreakpoints[breakPointsCurr]]);
                }
                currentDepth++;
                foreach (BreakPoints breakPointsNext in NextDepth) {
                    breakPointsNext.depth = currentDepth;
                }
                CurrDepth = NextDepth.Intersect(subMethodOfBreakpoints.Keys);
                NextDepth.Clear();
            } while (CurrDepth.IsNotEmpty());

            currentDepth--;
            while (currentDepth >= 0) {
                foreach (BreakPoints point in dictionary.Values) {
                   if (point.depth == currentDepth && subMethodOfBreakpoints.TryGetValue(point, out MethodBase method)) {
                        foreach (BreakPoints next in detoursOnThisMethod[method]) {
                            point.subBreakPointsID.Union(next.subBreakPointsID);
                            point.subBreakPointsID.Add(next.UID);
                        }
                    }
                }
                currentDepth--;
            }
        }

        public static BreakPoints Create(MethodBase from, string label, Func<string, Action<ILCursor, ILContext>> manipulator) {
            string ID = CreateUID(label);
            IDetour detour;
            using (new DetourContext { After = new List<string> { "*", "CelesteTAS-EverestInterop", "TASHelper" }, ID = "TAS Helper OOP_Core" }) {
                 detour = new ILHook(from, il => {
                    ILCursor ilCursor = new(il);
                    manipulator(ID)(ilCursor, il);
                }, manualConfig);
            }
            BreakPoints breakpoint = new BreakPoints(ID, detour);
            if (!detoursOnThisMethod.ContainsKey(from)) {
                detoursOnThisMethod[from] = new HashSet<BreakPoints>();
            }
            detoursOnThisMethod[from].Add(breakpoint);
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

        public static void Refresh(MethodBase methodBase) {
            if (dictionary.ContainsKey(methodBase)) {
                dictionary[methodBase].Dispose();
            }
            IDetour detour;
            using (new DetourContext { After = new List<string> { "*", "CelesteTAS-EverestInterop", "TASHelper" }, Priority = 10000, ID = "TAS Helper OOP_Core" }) {
                detour = new ILHook(methodBase, il => {
                    ILCursor cursor = new(il);
                    List<Instruction> breakpoints = new();
                    while (cursor.TryGotoNext(ins => ins.OpCode == OpCodes.Ldstr && ((string)ins.Operand).StartsWith(BreakPoints.Prefix), ins => ins.OpCode == OpCodes.Pop)) {
                        if (breakpoints.Count > 0) {
                            cursor.Emit(OpCodes.Ret);
                        }
                        breakpoints.Add(cursor.Next);
                        cursor.Index++;
                        cursor.Remove(); // remove the pop
                    }

                    cursor.Goto(-1, MoveType.AfterLabel);
                    if (methodBase == EngineUpdate) {
                        cursor.EmitDelegate(OOP_Core.PrepareToUndoAll); // we assume OOP_Core only runs when Level.Update is reachable on this frame, so other early return are not possible
                        cursor.Index--;
                    }
                    breakpoints.Add(cursor.Next);

                    int count = breakpoints.Count;
                    for (int i = 0; i < count - 1; i++) {
                        cursor.Goto(breakpoints[i], MoveType.Before);
                        cursor.Index++;
                        cursor.EmitDelegate(BreakPoints.BreakPointIsFinished);
                        cursor.Emit(OpCodes.Brtrue, breakpoints[i+1]);
                    }
                }, manualConfig);
            }
            dictionary[methodBase] = detour;
        }

        public static void ApplyAll() {
            foreach (IDetour detour in dictionary.Values) {
                detour.Apply();
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
}
