using Celeste.Mod.Core;
using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework.Input;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using TAS;
using CMCore = Celeste.Mod.Core;

namespace Celeste.Mod.TASHelper.Gameplay;

public static class ConsoleEnhancement {

    private static bool openConsole = false;

    private static bool lastOpen = false;

    private static int historyLineShift = 0;

    private static int origValue;

    private static bool historyScrollEnabled => TasHelperSettings.EnableScrollableHistoryLog;
    public static void SetOpenConsole() {
        if (Manager.Running && !lastOpen) {
            openConsole = true;
        }
    }
    public static bool GetOpenConsole() { // openConsole.getter may not be called (e.g. when there's a shortcut), so we can't modify its value here
        return openConsole;
    }

    public static void GoBackToBottom() {
        historyLineShift = 0;
    }

    [Load]
    public static void Load() {
        IL.Monocle.Commands.UpdateClosed += ILCommandUpdateClosed;
        On.Celeste.Level.BeforeRender += OnLevelBeforeRender;
        IL.Monocle.Commands.Render += ILCommandsRender;
        On.Monocle.Commands.UpdateOpen += OnCommandUpdateOpen;
    }

    [Unload]
    public static void Unload() {
        IL.Monocle.Commands.UpdateClosed -= ILCommandUpdateClosed;
        On.Celeste.Level.BeforeRender -= OnLevelBeforeRender;
        IL.Monocle.Commands.Render -= ILCommandsRender;
        On.Monocle.Commands.UpdateOpen -= OnCommandUpdateOpen;
    }

    [Initialize]
    public static void Initialize() {
        typeof(Manager).GetMethod("Update").HookAfter(UpdateCommands);
        typeof(Manager).GetMethod("DisableRun").HookAfter(MinorBugFixer);
    }

    private static void ILCommandsRender(ILContext context) {
        ILCursor cursor = new ILCursor(context);
        if (cursor.TryGotoNext(
            ins => ins.MatchLdarg(0),
            ins => ins.MatchLdfld<Monocle.Commands>("drawCommands"),
            ins => ins.MatchCallOrCallvirt<List<Monocle.Commands.Line>>("get_Count"),
            ins => ins.MatchLdcI4(0),
            ins => ins.OpCode == OpCodes.Ble,
            ins => ins.MatchLdloc(1))) {
            cursor.Index += 5;
            ILLabel end = (ILLabel) cursor.Prev.Operand;
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate(BeforeAction);
            cursor.GotoLabel(end);
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate(AfterAction);
        }
    }

    private static void BeforeAction(Monocle.Commands commands) {
        origValue = commands.firstLineIndexToDraw;
        commands.firstLineIndexToDraw = Calc.Clamp(commands.firstLineIndexToDraw + historyLineShift, 0, Math.Max(commands.drawCommands.Count - 1, 0));
    }

    private static void AfterAction(Monocle.Commands commands) {
        if (commands.drawCommands.Count > 0) {
            historyLineShift = commands.firstLineIndexToDraw - origValue; // this automatically bounds our shift
            commands.firstLineIndexToDraw = origValue;
        }
    }

    private static int repeatCounter = 0;

    private static void OnCommandUpdateOpen(On.Monocle.Commands.orig_UpdateOpen orig, Monocle.Commands commands) {
        orig(commands);
        if (historyScrollEnabled) {
            bool controlPressed = commands.currentState[Keys.LeftControl] == KeyState.Down || commands.currentState[Keys.RightControl] == KeyState.Down;
            if (commands.currentState[Keys.PageUp] == KeyState.Down && commands.oldState[Keys.PageUp] == KeyState.Up) {
                historyLineShift += (Engine.ViewHeight - 100) / 30;
            }
            else if (commands.currentState[Keys.PageDown] == KeyState.Down && commands.oldState[Keys.PageDown] == KeyState.Up) {
                if (controlPressed) {
                    historyLineShift = 0;
                }
                else {
                    historyLineShift -= (Engine.ViewHeight - 100) / 30;
                }
            }
            else if (commands.currentState[Keys.Up] == KeyState.Down && controlPressed) {
                repeatCounter += 1;
                while (repeatCounter >= 6) {
                    repeatCounter -= 2;
                    historyLineShift += 1;
                }
            }
            else if (commands.currentState[Keys.Down] == KeyState.Down && controlPressed) {
                repeatCounter += 1;
                while (repeatCounter >= 6) {
                    repeatCounter -= 2;
                    historyLineShift -= 1;
                }
            }
            else {
                repeatCounter = 0;
            }
        }
    }

    private static void MinorBugFixer() {
        // if open debugconsole and close it when in tas, then exit tas (without running any frame), debugconsole will show up

        /* order of operation:
         * MInput.Update, inside which is Manager.Update
         * Manager.Update, which calls DisableRun
         * stuff after MInput.Update, coz now Manager.Running == false
         * including Commands.UpdateClosed(), which opens debugconsole
         */

        // don't know why these bindings get pressed... at least the bug is fixed

        if (TasHelperSettings.EnableOpenConsoleInTas && (CoreModule.Settings.DebugConsole.Pressed || CoreModule.Settings.ToggleDebugConsole.Pressed) && !Engine.Commands.Open) {
            Engine.Commands.canOpen = false;
        }
    }
    private static void OnLevelBeforeRender(On.Celeste.Level.orig_BeforeRender orig, Level level) {
        openConsole = false;
        orig(level);
    }
    private static void UpdateCommands() {
        if (Manager.Running && TasHelperSettings.EnableOpenConsoleInTas) {
            lastOpen = Engine.Commands.Open;
            if (Engine.Commands.Open) {
                Engine.Commands.UpdateOpen();
            }
            else if (Engine.Commands.Enabled) {
                Engine.Commands.UpdateClosed();
            }
        }
    }

    private static void ILCommandUpdateClosed(ILContext context) {
        ILCursor cursor = new ILCursor(context);
        if (cursor.TryGotoNext(MoveType.AfterLabel,
            ins => ins.MatchCallOrCallvirt<CMCore.CoreModule>("get_Settings"),
            ins => ins.MatchCallOrCallvirt<CMCore.CoreModuleSettings>("get_DebugConsole"),
            ins => ins.MatchCallOrCallvirt<ButtonBinding>("get_Pressed"))) {
            ILLabel target;
            if (cursor.Next.Next.Next.Next.OpCode == OpCodes.Brtrue_S) { // depends on version of Everest
                target = (ILLabel)cursor.Next.Next.Next.Next.Operand;
            }
            else if (cursor.Prev.OpCode == OpCodes.Brtrue_S) {
                target = (ILLabel)cursor.Prev.Operand;
            }
            else {
                return;
            }
            cursor.EmitDelegate(GetOpenConsole);
            cursor.Emit(OpCodes.Brtrue_S, target);
        }
    }
}