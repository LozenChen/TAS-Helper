using Celeste.Mod.UI;
using Monocle;
using System.Runtime.CompilerServices;

namespace Celeste.Mod.TASHelper.Gameplay;

public static class ConsoleCommands {
    [Command("spinner_freeze", "Quick command to set Level.TimeActive 524288 (TAS Helper)")]
    public static void CmdSpinnerFreeze(bool on = true) {
        if (Engine.Scene is Level level) {
            level.TimeActive = on ? 524288f : 0f;
        }
    }

    [Command("nearest_timeactive", "Return the nearest possible timeactive of the target time")]
    [MethodImpl(MethodImplOptions.NoOptimization)]
    public static void CmdNearestTimeAcitve(float target, float start = 0f) {
        if (target >= 524288f) {
            Celeste.Commands.Log(524288f);
            return;
        }
        float delta = 1f / 60f;
        float curr = start;
        while (curr < target) {
            curr += delta;
        }
        Celeste.Commands.Log(curr);
    }

    [Command("switch_activate_all", "Activate All Switches (TAS Helper)")]
    public static void CmdSwitchActivateAll() {
        if (Engine.Scene is not Level level) {
            return;
        }
        bool has = false;
        if (level.Tracker.GetComponents<Switch>() is List<Component> list) {
            foreach (Switch sw in list) {
                sw.Activate();
                has = true;
            }
        }
        if (ModUtils.GetType("MaxHelpingHand", "Celeste.Mod.MaxHelpingHand.Entities.FlagTouchSwitch") is { } maxFlagTouchSwitch && maxFlagTouchSwitch.GetMethodInfo("TurnOn") is { } turnOn) {
            foreach (Entity e in level.Tracker.GetEntitiesTrackIfNeeded(maxFlagTouchSwitch)) {
                turnOn.Invoke(e, parameterless);
                has = true;
            }
        }
        if (has) {
            SoundEmitter.Play("event:/game/general/touchswitch_last_oneshot");
        }
    }

    [Command("MainMenu", "Goto Main Menu")]
    public static void GotoMainMenu() {
        Engine.Scene = new OverworldLoaderExt(Overworld.StartMode.MainMenu);
    }

    [Command("Oui_Mod_Update_List", "Goto OuiModUpdateList")]
    public static void GotoOuiModUpdateList() {
        Engine.Scene = OverworldLoaderExt.FastGoto<OuiModUpdateList>();
    }

    [Command("Oui_Mod_Options", "Goto OuiModOptions")]
    public static void GotoOuiModOptions() {
        Engine.Scene = OverworldLoaderExt.FastGoto<OuiModOptions>();
    }

    [Command("Oui_Mod_Toggler", "Goto OuiModToggler")]
    public static void GotoOuiModToggler() {
        Engine.Scene = OverworldLoaderExt.FastGoto<OuiModToggler>();
    }

    [Command("Oui_Map_List", "Goto OuiMapList")]
    public static void GotoOuiMapList() {
        Engine.Scene = OverworldLoaderExt.FastGoto<OuiMapList>();
    }
    /*
    [Command("Oui_Chapter_Panel", "Goto OuiChapterPanel")]
    public static void GotoOuiChapterPanel() {
        Engine.Scene = OverworldLoaderExt.FastGoto<OuiChapterPanel>();
        // may crash if we enter from main menu
    }

    [Command("Oui_Chapter_Select", "Goto OuiChapterSelect")]
    public static void GotoOuiChapterSelect() {
        Engine.Scene = OverworldLoaderExt.FastGoto<OuiChapterSelect>();
    }

    [Command("Oui_Journal", "Goto OuiJournal")]
    public static void GotoOuiJournal() {
        Engine.Scene = OverworldLoaderExt.FastGoto<OuiJournal>();
    }
    */
}


internal class OverworldLoaderExt : OverworldLoader {

    public Action<Overworld> overworldFirstAction;
    public OverworldLoaderExt(Overworld.StartMode startMode, HiresSnow snow = null) : base(startMode, snow) {
        Snow = null;
        fadeIn = false;
    }

    public static OverworldLoaderExt FastGoto<T>() where T : Oui {
        return new OverworldLoaderExt(Overworld.StartMode.MainMenu, null).SetOverworldAction(x => x.Goto<T>());
    }

    public override void Begin() {
        Add(new HudRenderer());
        /*
        Add(Snow);
        if (fadeIn) {
            ScreenWipe.WipeColor = Color.Black;
            new FadeWipe(this, wipeIn: true);
        }
        */
        base.RendererList.UpdateLists();
        Session session = null;
        if (SaveData.Instance != null) {
            session = SaveData.Instance.CurrentSession_Safe;
        }
        Entity entity = new Entity {
            new Coroutine(Routine(session))
        };
        Add(entity);
        activeThread = Thread.CurrentThread;
        activeThread.Priority = ThreadPriority.Lowest;
        RunThread.Start(LoadThreadExt, "OVERWORLD_LOADER_EXT", highPriority: true);
    }

    private void LoadThreadExt() {
        base.LoadThread();
        overworldFirstAction?.Invoke(overworld);
    }

    public OverworldLoaderExt SetOverworldAction(Action<Overworld> action) {
        overworldFirstAction = action;
        return this;
    }
}