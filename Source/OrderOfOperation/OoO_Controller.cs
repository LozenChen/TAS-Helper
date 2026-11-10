using Microsoft.Xna.Framework;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using TAS;

namespace Celeste.Mod.TASHelper.OrderOfOperation;

// New OoO engine (clean-room design B): one OoO frame is exactly one normal CelesteTAS FrameAdvance.
// Breakpoints are plain "call OoO_Controller.Break(label)" sites; blocking + frame pumping lives in OoO_Suspender.
internal static class OoO_Controller {
    internal static bool Active;
    internal static bool Skipping;
    internal static bool ExitAfterFrame;

    private static bool blocking => OoO_Suspender.Blocking;
    private static MonoMod.RuntimeDetour.ILHook entityListLoopHook;
    private static bool hooksAttached;

    internal static readonly HashSet<string> AutoSkippedBreakpoints = new();

    // Breakpoints that are skipped automatically during a normal step (same default set as the old engine).
    private static readonly string[] DefaultAutoSkipped = {
        "EngineUpdate_SceneBeforeUpdate begin",
        "LevelUpdate end",
        "SceneUpdate end",
        "PlayerUpdate end",
        "PlayerOrigUpdate_CameraUpdate",
        "PlayerOrigUpdate_LevelEnforceBounds",
        "EngineUpdate_SceneAfterUpdate begin"
    };

    [Load]
    public static void Load() {
        foreach (string label in DefaultAutoSkipped) {
            AutoSkippedBreakpoints.Add(label);
        }
    }

    // OoO is an infrequent, opt-in feature: install no detour in the normal state. Hooks are attached on
    // Enter() and disposed at the next render-time HandleHotkeys() call (when none of the hooked update
    // methods are on the stack), so other mods see stock code unless OoO stepping is actually used.
    internal static void AttachHooks() {
        if (hooksAttached) {
            return;
        }
        using (DetourContextHelper.ManagedHook("TAS Helper OoO Controller").AppliedAfter(DetourContextHelper.CelesteTAS).Use()) {
            On.Monocle.Engine.Update += OnEngineUpdate;
            On.Monocle.Scene.BeforeUpdate += OnSceneBeforeUpdate;
            On.Celeste.Level.Update += OnLevelUpdate;
            On.Monocle.Scene.Update += OnSceneUpdate;
            On.Monocle.EntityList.Update += OnEntityListUpdate;
            On.Celeste.Player.Update += OnPlayerUpdate;
            On.Monocle.Scene.AfterUpdate += OnSceneAfterUpdate;
        }

        entityListLoopHook = HookHelper.ManualAppliedILHook(typeof(EntityList).GetMethod("Update", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic), il => {
            ILCursor cursor = new(il);
            if (cursor.TryGotoNext(MoveType.After, ins => ins.Operand is MethodReference m && m.Name == "get_Current")) {
                Instruction stloc = cursor.Next;
                VariableDefinition entityLocal = stloc.OpCode.Code switch {
                    Code.Stloc_0 => il.Body.Variables[0],
                    Code.Stloc_1 => il.Body.Variables[1],
                    Code.Stloc_2 => il.Body.Variables[2],
                    Code.Stloc_3 => il.Body.Variables[3],
                    _ => stloc.Operand as VariableDefinition
                };
                if (entityLocal is not null) {
                    cursor.Index++;
                    cursor.Emit(OpCodes.Ldloc, entityLocal);
                    cursor.EmitDelegate<System.Action<Entity>>(OoO_ForEach.OnEntityLoopHead);
                }
            }
        }, DetourContextHelper.ILHook("TAS Helper OoO EntityListLoop").AppliedAfter(DetourContextHelper.CelesteTAS).ToConfig());
        entityListLoopHook.Apply();

        OoO_PlayerHooks.Attach();
        OoO_MoonBlock.Attach();
        hooksAttached = true;
    }

    internal static void DetachHooks() {
        if (!hooksAttached) {
            return;
        }
        On.Monocle.Engine.Update -= OnEngineUpdate;
        On.Monocle.Scene.BeforeUpdate -= OnSceneBeforeUpdate;
        On.Celeste.Level.Update -= OnLevelUpdate;
        On.Monocle.Scene.Update -= OnSceneUpdate;
        On.Monocle.EntityList.Update -= OnEntityListUpdate;
        On.Celeste.Player.Update -= OnPlayerUpdate;
        On.Monocle.Scene.AfterUpdate -= OnSceneAfterUpdate;
        entityListLoopHook?.Dispose();
        entityListLoopHook = null;
        OoO_PlayerHooks.Detach();
        OoO_MoonBlock.Detach();
        hooksAttached = false;
    }

    [Unload]
    public static void Unload() {
        DetachHooks();
    }

    internal static void Enter() {
        if (Active) {
            return;
        }
        if (TAS.Manager.CurrState != TAS.Manager.State.Paused) {
            global::Celeste.Mod.TASHelper.Entities.HotkeyWatcher.Refresh("Order-of-Operation: TAS is not paused");
            return;
        }
        try {
            AttachHooks();
        }
        catch (System.Exception e) {
            Logger.Log(LogLevel.Error, "OoO", "failed to attach OoO hooks: " + e);
            DetachHooks();
            return;
        }
        Active = true;
        Skipping = false;
        ExitAfterFrame = false;
        Logger.Log("OoO", $"enter frame={Engine.FrameCounter}");
        OoO_ForEach.ResetRun();
        OoO_PlayerCollider.ResetRun();
        OoO_Highlight.ResetRun();
        OoO_MoonBlock.ResetRun();
    }

    internal static void Exit() {
        if (!Active) {
            return;
        }
        Active = false;
        Skipping = false;
        ExitAfterFrame = false;
        Logger.Log("OoO", $"exit frame={Engine.FrameCounter}");
        OoO_Suspender.AutoStepping = false;
        OoO_Suspender.Blocking = false;
    }

    // Called from TASHelperSettings.SettingsHotkeysPressed on every Level.Render.
    [Command_StringParameter("ooo_add_autoskip", "Autoskip a normal breakpoint (not a for-each breakpoint)(TAS Helper)")]
    public static void AddAutoSkip(string uid) {
        AutoSkippedBreakpoints.Add(NormalizeLabel(uid));
    }

    [Command_StringParameter("ooo_remove_autoskip", "Stop autoskipping a normal breakpoint (TAS Helper)")]
    public static void RemoveAutoSkip(string uid) {
        AutoSkippedBreakpoints.Remove(NormalizeLabel(uid));
    }

    [Command("ooo_show_autoskip", "Show all autoskipped breakpoints (TAS Helper)")]
    public static void ShowAutoSkip() {
        foreach (string label in AutoSkippedBreakpoints) {
            Celeste.Commands.Log(label);
        }
    }

    [Command("ooo_show_breakpoint", "Show all normal breakpoints (TAS Helper)")]
    public static void ShowBreakpoint() {
        foreach (string label in AllLabels) {
            Celeste.Commands.Log(label);
        }
    }

    private static string NormalizeLabel(string label) {
        return label.Replace("TAS Helper OoO_Core::", "");
    }

    internal static readonly string[] AllLabels = {
        "EngineUpdate begin",
        "EngineUpdate_SceneBeforeUpdate begin",
        "LevelUpdate begin",
        "LevelUpdate end",
        "SceneUpdate begin",
        "SceneUpdate end",
        "SceneUpdate_EntityListUpdate begin",
        "SceneUpdate_EntityListUpdate end",
        "PlayerUpdate begin",
        "PlayerUpdate end",
        "PlayerOrigUpdate begin",
        "PlayerOrigUpdate_ClimbHop",
        "PlayerOrigUpdate_BaseUpdate begin",
        "PlayerOrigUpdate_BaseUpdate end",
        "PlayerOrigUpdate_MoveH",
        "PlayerOrigUpdate_MoveV",
        "PlayerOrigUpdate_CameraUpdate",
        "PlayerOrigUpdate_PlayerColliderCheck begin",
        "PlayerOrigUpdate_LevelEnforceBounds",
        "PlayerOrigUpdate end",
        "EngineUpdate_SceneAfterUpdate begin",
        "EngineUpdate end"
    };

    internal static void HandleHotkeys() {
        if (blocking) {
            return; // keys while blocked are polled by OoO_Suspender directly
        }
        if (!Active && hooksAttached) {
            DetachHooks();
        }
        if (!Active && global::Celeste.Mod.TASHelper.Module.Menu.TH_Hotkeys.OoO_Step_Hotkey.Pressed) {
            if (!TasHelperSettings.EnableOoO) {
                global::Celeste.Mod.TASHelper.Entities.HotkeyWatcher.Refresh("Order-of-Operation stepping NOT Enabled");
                return;
            }
            Enter();
        }
    }

    internal static void BreakExternal(string label) => Break(label);

    private static void Break(string label) {
        if (!Active) {
            return;
        }
        if (Skipping) {
            global::Celeste.Mod.TASHelper.Entities.HotkeyWatcher.Refresh(label);
            return;
        }
        if (AutoSkippedBreakpoints.Contains(label)) {
            return;
        }
        OoO_Suspender.Block(label);
    }

    private static void OnEngineUpdate(On.Monocle.Engine.orig_Update orig, Engine self, GameTime gameTime) {
        if (!Active) {
            orig(self, gameTime);
            return;
        }

        Break("EngineUpdate begin");
        if (!Active) {
            orig(self, gameTime);
            return;
        }

        if (TAS.Manager.CurrState == TAS.Manager.State.Paused) {
            TAS.Manager.NextState = TAS.Manager.State.FrameAdvance;
        }
        orig(self, gameTime);

        if (Active) {
            Break("EngineUpdate end");
        }
        if (Active) {
            Exit();
        }
    }

    private static void OnSceneBeforeUpdate(On.Monocle.Scene.orig_BeforeUpdate orig, Scene self) {
        Break("EngineUpdate_SceneBeforeUpdate begin");
        orig(self);
    }

    private static void OnLevelUpdate(On.Celeste.Level.orig_Update orig, Level self) {
        Break("LevelUpdate begin");
        orig(self);
        Break("LevelUpdate end");
    }

    private static void OnSceneUpdate(On.Monocle.Scene.orig_Update orig, Scene self) {
        Break("SceneUpdate begin");
        orig(self);
        Break("SceneUpdate end");
    }

    private static void OnEntityListUpdate(On.Monocle.EntityList.orig_Update orig, EntityList self) {
        Break("SceneUpdate_EntityListUpdate begin");
        orig(self);
        Break("SceneUpdate_EntityListUpdate end");
    }

    private static void OnPlayerUpdate(On.Celeste.Player.orig_Update orig, Player self) {
        Break("PlayerUpdate begin");
        orig(self);
        Break("PlayerUpdate end");
    }

    private static void OnSceneAfterUpdate(On.Monocle.Scene.orig_AfterUpdate orig, Scene self) {
        Break("EngineUpdate_SceneAfterUpdate begin");
        orig(self);
    }
}
