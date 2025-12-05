//#define ShowAllLog
using Microsoft.Xna.Framework;
using System.Text;

namespace Celeste.Mod.TASHelper.Module;

public static class WhatsNew {

    public static bool NewUpdateLogExist = false;

    public static bool ShouldShow => NewUpdateLogExist && TasHelperSettings.SubscribeWhatsNew && UpdateLogs.IsNotEmpty(); // in case i forget to write update logs lol

    public static List<Tuple<string, List<string>>> UpdateLogs = new();

    public static Version CurrentVersion => TASHelperModule.Instance.Metadata.Version;

    [Initialize]
    public static void Initialize() {
#if ShowAllLog
        TasHelperSettings.LastVersion = new Version(1, 0).ToString();
#endif

        if (TasHelperSettings.FirstInstall) {
            NewUpdateLogExist = true;
            TasHelperSettings.LastVersion = new Version(1, 0).ToString();
        }
        else {
            NewUpdateLogExist = new Version(TasHelperSettings.LastVersion) < CurrentVersion;
        }

        if (NewUpdateLogExist) {
            CreateUpdateLog();
        }
    }


    public static void CreateUpdateLog() {
        // this feat appears in v1.8.12 and i dont plan to write those update logs several versions ago...
        // we only introduce new features / bugfixes here. Optimizations are not included unless that's a major one, or unless we have nothing to say

        UpdateLogs.Clear();
        LastVersion = new Version(TasHelperSettings.LastVersion);
        AddLog("1.8.10", "\"More Options\" -> \"Scrollable Console History Log\"");
        AddLog("1.8.11", "\"More Options\" -> \"Better Invincibility\".");
        AddLog("1.8.12", "Wind Speed Renderer. Enable it in \"More Options\" -> \"Show Wind Speed\".", "Add the \"What's New!\" page. You can unsubscribe it in \"More Options\" -> \"Subscribe What's New!\"");
        AddLog("1.8.13", "Now in Predictor, you can use (Dotted) Polygon Line instead of Hitbox per Frame to show your future track. Enable it in \"Predictor\" -> \"Other\" -> \"Timeline Finest Scale\" -> \"(Dotted) Polygon Line\"", "Simplified Triggers, which will hide unimportant triggers.", "Now Camera-Related Triggers have a different color.Enable it in \"Custom Colors Config\" -> \"Switches\" -> \"Camera-Related Triggers Color\"");
        AddLog("1.8.14", "Bugfix: If you use Predictor \"Predict on Tas File Changed\" and edit any content before the current frame in tas, then the cursor in CelesteStudio will jump around.");
        AddLog("1.8.15", "Bugfix: Game Crashes when encountering AcidLightning of Glyph mod.");
        AddLog("1.8.16", "Simplified Triggers feature now can hide StyleMaskHelper masks.");
        AddLog("1.8.17", "Cassette Block Helper. Enable it in \"More Options\".");
        AddLog("1.8.18", "Cassette Block Helper now supports the cassette maps in Spring Collab 2020 and Into the Jungle");
        AddLog("1.8.19", "Cassette Block Helper now shows more info. Enable it in \"More Options\" -> \"Cassette Block Extra Info\".");
        AddLog("1.8.20", "Hazard Countdown now has a new mode: ExactGroup, which is useful when you need to manip a spinner drift.");
        AddLog("1.8.21", "Cassette Block Info now can be placed at top/bottom-left/right. Configurate it in \"More Options\".");
        AddLog("1.8.22", "Performance Optimization. It improves by 25%.");
        AddLog("1.9.0", "Feature: Movement Overshoot Assistant. This shows how far player would go if there were no walls.", "Add OUI console commands, so you can goto some common OUIs very quickly.");
        AddLog("1.9.1", "Rename some internal class names to resolve some custom info issues.");
        AddLog("1.9.2", "Bugfix: Predictor doesn't work properly when encountering Strawberry Jam Wonky Cassette Blocks.");
        AddLog("1.9.3", "Bugfix: Predictor makes BGSwitch related tas desync.", "Feature: Predictor now supports most common commands. (\"Set\", \"Invoke\", \"Console\", \"Mouse\", \"Press\", \"Gun\", \"EvalLua\")");
        AddLog("1.9.4", "MovementOvershootAssistant now supports Inverted Gravity and DreamTunnelDashState.");
        AddLog("1.9.5", "Support GhostModForTas.");
        AddLog("1.9.6", "Cassette Block Helper supports QuantumMechanics mod cassette blocks.");
        AddLog("1.9.7", "Some daily maintenance.");
        AddLog("1.9.8", "ModInterop: Export predictor's SL action.");
        AddLog("1.9.9", "New Hotkey: Reverse Frame Advance. This makes tas re-run but up to the previous frame. (Feature Request from @Vamp)");
        AddLog("1.9.10", "Optimize Reverse Frame Advance.");
        AddLog("1.9.11", "Bugfix: Better Invincible still persists even if a \"Set Invincible true\" command after breakpoint ***S is removed, if you use the RESTART hotkey. (Thanks @ayalyyn)");
        AddLog("1.9.12", "Bugfix: InView Range does not work properly if you are doing a bino control storage. Now InView Range will handle OoO issues properly. (Thanks @atpx8)");
        AddLog("1.9.13", "Bugfix: Game crashes in wavedash.ppt. (Thanks @trans_alexa)");
        AddLog("1.9.14", "Feature: Make SpeedrunTimer become transparent when TAS pauses. Default is ON. Edit it in \"More Options\".", "Divide \"More Options\" into three pages.", "Feature: Allow PageUp/PageDown in subMenus.");
        AddLog("1.9.15", "Feature: Show the hitbox of Hollow Knight Nail from FlaglinesAndSuch.", "Feature: Spinner related features now support ChroniaHelper's SeamlessSpinner.");
        AddLog("2.0.0", "Migrate to Everest Core, and target psyGamer's branch of CelesteTAS.");
        AddLog("2.0.1", "Bugfix: Game crashes when there is simplified spinner / lightning. (thanks @trans_alexa)");
        AddLog("2.0.2", "Feature: AutoWatchEntity put into use. (the name comes from @XMinty77)", "Optimization: Now you can enter OptionSubMenu by just pressing down.");
        AddLog("2.0.3", "Bugfix: resolve incompatibility with SpeedrunTool", "Addition: Add more options to AutoWatch.");
        AddLog("2.0.4", "Bugfix: If Cassette tempo = 0 and there's freeze frame, then game gets stuck (thanks @trans_alexa)");
        AddLog("2.0.5", "Bugfix: Cutscene is auto-watched even if Auto Watch is not enabled. (thanks @socksygen)");
        AddLog("2.0.6", "Feature: Auto-Watch now supports Triggers, and vanilla / Everest ones are handled well.");
        AddLog("2.0.7", "Feature: Auto-Watch now supports CrumbleWallOnRumble.");
        AddLog("2.0.8", "Change: Target CelesteTAS v3.41.0", "Feature: Auto-Watch supports more triggers.");
        AddLog("2.0.9", "Feature: SubMenus support Mod Options' Search Box.", "Bugfix: Predictor results are not cleared after loadstate.", "Change: Don't initialize predictor unless necessary."); // note items inside submenus can't be fetched, coz they are not items of the main menu.
        AddLog("2.0.10", "Bugfix: Predictor results are not cleared after re-run the tas.");
        AddLog("2.0.11", "Rewrite some codes to be compatible with ghost mod.");
        AddLog("2.0.12", "Feature: Auto-Watch now decrypts Auroras Hashed DashCode Trigger.", "Support Stunning Helper's hazards. (thanks @trans_alexa)");
        AddLog("2.0.13", "Feature: Auto-Watch supports SwitchGate.");
        AddLog("2.0.14", "Feature: Add Spinner Drift Speed (in Hazard Countdown). (thanks @trans_alexa)", "Bugfix: Simplified Spinner doesn't handle old versions of XaphanHelper properly, and crash with latest XaphanHelper.");
        AddLog("2.1.0", "Remove: SimplifiedTrigger merges into CelesteTAS.", "Remove: BetterInvincible merges into CelesteTAS.", "Remove: OpenConsoleInTas merges into CelesteTAS.", "Remove: Scrollable Console temporarily removed.", "Refactor: Coz CelesteTAS refactors quite a lot.");
        AddLog("2.1.1", "Bugfix: fix a bug caused by CelesteTAS refactor.");
        AddLog("2.1.2", "Bugfix: Predictor not working properly, caused by CelesteTAS refactor.");
        AddLog("2.1.3", "Bugfix: Incompatibility with MotionSmoothing in event ch09_goto_the_future. (thanks @cameryn)");
        AddLog("2.1.4", "Bugfix: Fastforward makes spinner colors change. (thanks @trans_alexa)");
        AddLog("2.1.5", "Bugfix: fix a bug caused by CelesteTAS refactor.");
        AddLog("2.1.6", "Bugfix: When predictor Update-on-Tas-File-Changed enabled, and edit a line above the current frame, it'll move selection to the current frame. (thanks @richconnergmn)");
        AddLog("2.1.7", "Feature: Predictor now can set its own font size. (thanks @richconnergmn)");
        AddLog("2.1.8", "Bugfix: OoO stepper not working. (thanks @ella.melon)");
        AddLog("2.1.9", "Bugfix: Glyph Acid Lightning is not properly handled. (thanks @mathhacker, @trans_alexa)");
        AddLog("2.1.10", "Bugfix: fix a bug caused by CelesteTAS ABI change.");
        AddLog("2.2.0", "Refactor: Predictor now uses SpeedrunTool multiple saveslots. Note this needs SpeedrunTool v3.25.0 or higher, and SpeedrunTool v3.25.0 will be released a weak later. So this feature is temporarily disabled.");
        AddLog("2.2.1", "Remove: feature \"Inverse Frame Advance\" is now merged into CelesteTAS.");
        AddLog("2.2.2", "Bugfix: Hazard Countdown and LoadRangeCollider don't respect Order of Operations.");
        AddLog("2.2.3", "Feature: Now Countdown uses the infinity symbol when spinner freeze.", "Re-implement MovingEntityTrack.", "Use Everest's new features to replace some internal methods.", "Feature: Add console command \"switch_activate_all\"");
        AddLog("2.2.4", "Bugfix: Fix StarJumpBlock.orig_Render crash. (thanks @Catabilities)");
        AddLog("2.2.5", "Bugfix: Fix a NullReferenceException. (thanks @Michal 338)", "Feature: AutoWatch supports CrumblePlatform.");
        AddLog("2.2.6", "Feature: AutoWatch support MaxHelpingHand's CustomizableCrumblePlatform.");
        AddLog("2.2.7", "Bugfix: Support latest Chronia Helper update.");
        AddLog("2.2.8", "Bugfix: Make OoO Stepper work again.");
        AddLog("2.2.9", "Bugfix: Fix a crash related to ExtendedVariantMode v0.47.0.");
        AddLog("2.2.10", "Feature: OoO Stepper supports FloatySpaceBlock.");
        AddLog("2.2.11", "Feature: Enhance AutoWatch MoonBlock.");
        AddLog("2.2.12", "Bugfix: Simplified Spinner doesn't work well with latest ChroniaHelper. (thanks @Socks)");
        UpdateLogs.Sort((x, y) => new Version(y.Item1).CompareTo(new Version(x.Item1)));
    }

    private static Version LastVersion;


    private static void AddLog(string version, List<string> updateLogs) {
        if (LastVersion < new Version(version)) {
            UpdateLogs.Add(new Tuple<string, List<string>>(version, updateLogs));
        }
    }
    private static void AddLog(string version, string log1) {
        AddLog(version, new List<string>() { log1 });
    }

    private static void AddLog(string version, string log1, string log2) {
        AddLog(version, new List<string>() { log1, log2 });
    }

    private static void AddLog(string version, string log1, string log2, string log3) {
        AddLog(version, new List<string>() { log1, log2, log3 });
    }
    private static void AddLog(string version, string log1, string log2, string log3, string log4) {
        AddLog(version, new List<string>() { log1, log2, log3, log4 });
    }
    private static void AddLog(string version, string log1, string log2, string log3, string log4, string log5) {
        AddLog(version, new List<string>() { log1, log2, log3, log4, log5 });
    }
    public static Menu.EaseInSubMenu CreateWhatsNewSubMenu(TextMenu menu) {
        return new Menu.EaseInSubMenu(Menu.TASHelperMenu.ToDialogText("Whats New"), false).Apply(subMenu => {
            foreach (Tuple<string, List<string>> tuple in UpdateLogs) {
                subMenu.Add(new TextMenuExt.ButtonExt(tuple.Item1) { TextColor = Color.White, Scale = 0.7f * Vector2.One });
                foreach (string updateLog in tuple.Item2) {
                    subMenu.Add(new LogItem(" - " + updateLog) { TextColor = Color.Gray, Scale = 0.6f * Vector2.One });
                }
            }
            subMenu.OnPressed += () => {
                TasHelperSettings.LastVersion = CurrentVersion.ToString();
            };
        });
    }

    private class LogItem : TextMenuExt.ButtonExt {

        public int lines = 1;
        public LogItem(string label, int width = 90) : base(string.Join("\n", label.Split('\n').Select(x => WordWrap(x, width)))) {
            lines = 1 + this.Label.Count(x => x == '\n');
        }

        public static string WordWrap(string str, int width) {
            string[] words = str.Split(' ').Select(x => x + " ").ToArray();
            int curLineLength = 0;
            StringBuilder sb = new();
            for (int i = 0; i < words.Length; i++) {
                string word = words[i];
                if (curLineLength + word.Length > width) {
                    if (curLineLength > 0) {
                        sb.Append("\n");
                        curLineLength = 0;
                    }
                    while (word.Length > width) {
                        sb.Append(word.Substring(0, width - 1) + "-\n");
                        word = word.Substring(width - 1);
                    }
                    word = word.TrimStart();
                }
                sb.Append(word);
                curLineLength += word.Length;
            }
            return sb.ToString();
        }

        public override float Height() {
            return base.Height() * lines;
        }

        public override float LeftWidth() {
            return 0f;
        }

        public override float RightWidth() {
            return 0f;
        }
    }
}