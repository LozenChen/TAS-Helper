using Celeste.Mod.Core;
using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using TAS;
using TAS.EverestInterop;
using CMCore = Celeste.Mod.Core;

namespace Celeste.Mod.TASHelper.Gameplay;

public static class ConsoleEnhancement {

    private static bool openConsole = false;

    private static bool lastOpen = false;

    private static int historyLineShift = 0;

    private const int extraExtraHistoryLines = 1000;

    private static int origValue;

    public static float ScrollBarWidth = 10f;
    public static float ScrollBarHeight = 20f;

    private static bool historyScrollEnabled => TasHelperSettings.EnableScrollableHistoryLog;
    public static void SetOpenConsole() {
        if (Manager.Running && !lastOpen) {
            openConsole = true;
        }
    }
    public static bool GetOpenConsole() { // openConsole.getter may not be called (e.g. when there's a shortcut), so we can't modify its value here
        return openConsole;
    }

    [Load]
    public static void Load() {
        IL.Monocle.Commands.UpdateClosed += ILCommandUpdateClosed;
        On.Celeste.Level.BeforeRender += OnLevelBeforeRender;
        IL.Monocle.Commands.Render += ILCommandsRender;
        On.Monocle.Commands.UpdateOpen += OnCommandUpdateOpen;
        IL.Monocle.Commands.Log_object_Color += ILCommandsLog;
    }

    [Unload]
    public static void Unload() {
        IL.Monocle.Commands.UpdateClosed -= ILCommandUpdateClosed;
        On.Celeste.Level.BeforeRender -= OnLevelBeforeRender;
        IL.Monocle.Commands.Render -= ILCommandsRender;
        On.Monocle.Commands.UpdateOpen -= OnCommandUpdateOpen;
        IL.Monocle.Commands.Log_object_Color -= ILCommandsLog;
    }

    [Initialize]
    public static void Initialize() {
        typeof(Manager).GetMethod("Update").HookAfter(UpdateCommands);
        typeof(CenterCamera).GetMethod("ZoomCamera", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).IlHook(il => {
            ILCursor cursor = new ILCursor(il);
            if (cursor.TryGotoNext(MoveType.After, ins => ins.MatchCallOrCallvirt(typeof(MouseButtons).FullName, "get_Wheel"))) {
                cursor.EmitDelegate(PreventZoomCamera);
                cursor.Emit(OpCodes.Mul);
            }
        });
    }

    private static int PreventZoomCamera() {
        return Engine.Commands.Open && historyScrollEnabled && Engine.Commands.drawCommands.Count > (Engine.ViewHeight - 100) / 30 ? 0 : 1;
    }

    private static int ExtraExtraHistoryLines() {
        return historyScrollEnabled ? extraExtraHistoryLines : 0;
    }

    private static void ILCommandsLog(ILContext il) {
        // allow to store more history logs than default setting of Everest, which is not editable in game (can only be editted in savefile)
        ILCursor cursor = new ILCursor(il);
        if (cursor.TryGotoNext(MoveType.After, ins => ins.MatchCallOrCallvirt<CoreModuleSettings>("get_ExtraCommandHistoryLines"))) {
            cursor.EmitDelegate(ExtraExtraHistoryLines);
            cursor.Emit(OpCodes.Add);
        }
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
        if (historyScrollEnabled) {
            origValue = commands.firstLineIndexToDraw;
            commands.firstLineIndexToDraw = Calc.Clamp(commands.firstLineIndexToDraw + historyLineShift, 0, Math.Max(commands.drawCommands.Count - (Engine.ViewHeight - 100) / 30, 0));
        }
    }

    private static void AfterAction(Monocle.Commands commands) {
        if (!historyScrollEnabled) {
            return;
        }
        if (commands.drawCommands.Count > (Engine.ViewHeight - 100) / 30) {
            int num3 = Math.Min((Engine.ViewHeight - 100) / 30, commands.drawCommands.Count - commands.firstLineIndexToDraw);
            float num4 = 10f + 30f * (float)num3;
            Draw.Rect((float)Engine.ViewWidth - 15f - ScrollBarWidth, (float)Engine.ViewHeight - num4 - 60f, ScrollBarWidth, num4, Color.Gray * 0.8f);
            Draw.Rect((float)Engine.ViewWidth - 15f - ScrollBarWidth + 1f, (float)Engine.ViewHeight - 60f - (float)(num4 - ScrollBarHeight) * (float)commands.firstLineIndexToDraw / (float)Math.Max(commands.drawCommands.Count - (Engine.ViewHeight - 100) / 30, 1) - ScrollBarHeight, ScrollBarWidth - 2f, ScrollBarHeight, Color.Silver * 0.8f);
        }
        if (commands.drawCommands.Count > 0) {
            historyLineShift = commands.firstLineIndexToDraw - origValue; // this automatically bounds our shift
            commands.firstLineIndexToDraw = origValue;
        }
    }

    private static void OnCommandUpdateOpen(On.Monocle.Commands.orig_UpdateOpen orig, Monocle.Commands commands) {
        orig(commands);
        if (historyScrollEnabled) {
            bool controlPressed = commands.currentState[Keys.LeftControl] == KeyState.Down || commands.currentState[Keys.RightControl] == KeyState.Down;

            // btw, mouseScroll is already used by Everest to adjust cursor scale
            MouseState mouseState = Mouse.GetState();
            int mouseScrollDelta = mouseState.ScrollWheelValue - commands.mouseScroll;
            if (mouseScrollDelta / 120 != 0) {
                // i dont know how ScrollWheelValue is calculated, for me, it's always a multiple of 120
                // in case for other people, it's lower than 120, we provide Math.Sign as a compensation
                historyLineShift += mouseScrollDelta / 120;
            }
            else {
                historyLineShift += Math.Sign(mouseScrollDelta);
            }

            if (commands.currentState[Keys.PageUp] == KeyState.Down && commands.oldState[Keys.PageUp] == KeyState.Up) {
                if (controlPressed) {
                    historyLineShift = 99999;
                }
                else {
                    historyLineShift += (Engine.ViewHeight - 100) / 30;
                }
            }
            else if (commands.currentState[Keys.PageDown] == KeyState.Down && commands.oldState[Keys.PageDown] == KeyState.Up) {
                if (controlPressed) {
                    historyLineShift = -99999;
                }
                else {
                    historyLineShift -= (Engine.ViewHeight - 100) / 30;
                }
            }
            /*
             * this already exists
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
            */
        }
    }

    [TasDisableRun]
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
        // it seems this feature may break after hot reload
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