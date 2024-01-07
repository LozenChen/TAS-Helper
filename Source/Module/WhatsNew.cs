//#define ShowAllLog
using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework;
using Monocle;
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
        // we only introduce new features / bugfixes here. Optimizations are not included unless that's a major one

        UpdateLogs.Clear();
        LastVersion = new Version(TasHelperSettings.LastVersion);
        AddLog("1.8.10", "\"More Options\" -> \"Scrollable Console History Log\"");
        AddLog("1.8.11", "\"More Options\" -> \"Better Invincibility\".");
        AddLog("1.8.12", "Wind Speed Renderer. Enable it in \"More Options\" -> \"Show Wind Speed\".", "Add the \"What's New!\" page. You can unsubscribe it in \"More Options\" -> \"Subscribe What's New!\"");
        AddLog("1.8.13", "Now in Predictor, you can use (Dotted) Polygon Line instead of Hitbox per Frame to show your future track. Enable it in \"Predictor\" -> \"Other\" -> \"Timeline Finest Scale\" -> \"(Dotted) Polygon Line\"", "Simplified Triggers, which will hide unimportant triggers.", "Now Camera-Related Triggers have a different color.Enable it in \"Custom Colors Config\" -> \"Switches\" -> \"Camera-Related Triggers Color\"");
        AddLog("1.8.14", "Bugfix: If you use Predictor \"Predict on Tas File Changed\" and edit any content before the current frame in tas, then the cursor in CelesteStudio will jump around.");

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