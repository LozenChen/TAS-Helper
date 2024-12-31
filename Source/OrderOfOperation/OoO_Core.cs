//#define OoO_Debug

using Celeste.Mod.TASHelper.Entities;
using Celeste.Mod.TASHelper.Module.Menu;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System.Reflection;
using TAS;
using TAS.EverestInterop;

namespace Celeste.Mod.TASHelper.OrderOfOperation;
internal static class OoO_Core {

    internal static readonly MethodInfo EngineUpdate = typeof(Engine).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly MethodInfo LevelUpdate = typeof(Level).GetMethod("Update");
    private static readonly MethodInfo SceneUpdate = typeof(Scene).GetMethod("Update");
    internal static readonly MethodInfo PlayerUpdate = typeof(Player).GetMethod("Update");
    internal static readonly MethodInfo PlayerOrigUpdate = typeof(Player).GetMethod("orig_Update");
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
                /*
                foreach (MethodBase method in BreakPoints.detoursOnThisMethod.Keys) {
                    Utils.CILCodeHelper.CILCodeLogger(method, 9999);
                }
                */
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

    internal static bool GetStepping() {
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
        if (ForEachBreakPoints_EntityList.ultraFastForwarding || ForEachBreakPoints_PlayerCollider.ultraFastForwarding) {
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

    internal static bool prepareToUltraFastForwarding = false;

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

    internal static void SendText(string str) {
        lastText = str;
    }

    private static bool stepping = false;

    internal static ILHookConfig manualConfig => Utils.HookHelper.manualConfig;

    internal static readonly Action<ILCursor> NullAction = (cursor) => { };

    [Initialize]
    public static void Initialize() {

        BreakPoints.MarkEnding(EngineUpdate, "EngineUpdate end", () => prepareToUndoAll = true); // i should have configured detourcontext... don't know why, but MarkEnding must be called at first (at least before those breakpoints on same method)

        BreakPoints.MarkEnding(LevelUpdate, "LevelUpdate end", () => LevelUpdate_Entry.SubMethodPassed = true).AddAutoSkip();

        BreakPoints.MarkEnding(SceneUpdate, "SceneUpdate end", () => SceneUpdate_Entry.SubMethodPassed = true).AddAutoSkip();


        BreakPoints.MarkEnding(PlayerUpdate, "PlayerUpdate end", ForEachBreakPoints_EntityList.MarkSubMethodPassed).AddAutoSkip();

        BreakPoints.MarkEnding(PlayerOrigUpdate, "PlayerOrigUpdate end", () => { PlayerOrigUpdate_Entry.SubMethodPassed = true; playerUpdated = true; });

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

        EntityListUpdate_Entry = BreakPoints.CreateFull(SceneUpdate, "SceneUpdate_EntityListUpdate begin", 3, NullAction, NullAction,
            ins => ins.OpCode == OpCodes.Ldarg_0,
            ins => ins.MatchCallOrCallvirt<Scene>("get_Entities"),
            ins => ins.MatchCallOrCallvirt<EntityList>("Update")
        );

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

        ForEachBreakPoints_EntityList.AddTarget("Player", true);
        ForEachBreakPoints_EntityList.AddTarget(ForEachBreakPoints_EntityList.autoStopString);

        /*
         * examples:
        BreakPoints.ForEachBreakPoints.AddTarget("CrystalStaticSpinner[f-11:902]");
        BreakPoints.ForEachBreakPoints.AddTarget("BadelineBoost");
        ForEachBreakPoints_EntityList.AddTarget("Each");
        */

        ForEachBreakPoints_PlayerCollider.Create();
        ForEachBreakPoints_PlayerCollider.AddTarget(ForEachBreakPoints_PlayerCollider.autoStopString);
        /*
         * examples:
        ForEachBreakPoints_PlayerCollider.AddTarget("Spring");
        */

        SpringBoard.Create(EngineUpdate);
        SpringBoard.Create(LevelUpdate);
        SpringBoard.Create(SceneUpdate);
        SpringBoard.Create(PlayerUpdate);
        SpringBoard.Create(PlayerOrigUpdate);

        hookTASIsPaused = new ILHook(typeof(TAS.EverestInterop.Core).GetMethod("IsPaused", BindingFlags.NonPublic | BindingFlags.Static), il => {
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
        ForEachBreakPoints_EntityList.Apply();
        ForEachBreakPoints_PlayerCollider.Apply();
        hookTASIsPaused.Apply();
        hookManagerUpdate.Apply();
        Applied = true;
        ForEachBreakPoints_EntityList.Load();
        ResetLongtermState();
        ResetTempState();
        //Gameplay.Spinner.ActualCollideHitboxDelegatee.StopActualCollideHitbox();
        SendTextImmediately("OoO Stepping begin");
    }

    public static void UndoAll() {
        SpringBoard.UndoAll();
        BreakPoints.UndoAll();
        ForEachBreakPoints_EntityList.Undo();
        ForEachBreakPoints_PlayerCollider.Undo();
        hookTASIsPaused.Undo();
        hookManagerUpdate.Undo();
        Applied = false;
        ForEachBreakPoints_EntityList.Unload();
        ResetLongtermState();
        ResetTempState();
        overrideStepping = false;
        prepareToUltraFastForwarding = false;
        //Gameplay.Spinner.ActualCollideHitboxDelegatee.RecoverActualCollideHitbox();
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
        PlayerOrigUpdate_Entry.SubMethodPassed = false;
        playerUpdated = false;
        BreakPoints.latestBreakpointBackup.Clear();
        ForEachBreakPoints_EntityList.Reset();
        ForEachBreakPoints_PlayerCollider.Reset();
    }
    private static void ResetTempState() {
        ForEachBreakPoints_EntityList.ResetTemp();
        ForEachBreakPoints_PlayerCollider.ResetTemp();
        BreakPoints.passedBreakpoints.Clear();
    }

    private static BreakPoints LevelUpdate_Entry;
    private static BreakPoints SceneUpdate_Entry;
    internal static BreakPoints EntityListUpdate_Entry;
    private static BreakPoints PlayerOrigUpdate_Entry;


    [Load]
    public static void Load() {
        using (new DetourContext { After = new List<string> { "*", "CelesteTAS-EverestInterop" }, Before = new List<string> { "TASHelper" }, ID = "TAS Helper OoO_Core OnLevelRender" }) {
            On.Celeste.Level.Render += OnLevelRender;
        }
    }

    [Unload]
    public static void Unload() {
        On.Celeste.Level.Render -= OnLevelRender;
        hookTASIsPaused?.Dispose();
        hookManagerUpdate?.Dispose();
    }
    private static void OnLevelRender(On.Celeste.Level.orig_Render orig, Level self) {
        if (Applied) {
            TAS.Gameplay.Hitboxes.ActualCollideHitbox.playerUpdated = playerUpdated; // may remove it if we change SpringBoard to IL.Monocle/Celeste hooks
            if (prepareToUndoAll) {
                UndoAll();
            }
            else {
                if (lastText == PlayerOrigUpdate_PlayerCollideCheckBegin_UID && !ForEachBreakPoints_PlayerCollider.applied) {
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

    private static bool playerUpdated = false;
    private static bool allowHotkey => Applied || TasHelperSettings.EnableOoO;

    public static bool TryHardExit = true;
    internal static void OnHotkeysPressed() {
        if (Applied && (Hotkeys.FrameAdvance.Pressed || Hotkeys.SlowForward.Pressed || Hotkeys.PauseResume.Pressed || Hotkeys.StartStop.Pressed || TH_Hotkeys.FrameStepBack.Pressed)) {
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

}
