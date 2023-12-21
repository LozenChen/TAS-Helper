//#define ShowAllLog
using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework;
namespace Celeste.Mod.TASHelper.Module;

public static class WhatsNew {

    public static bool NewUpdateLogExist = false;

    public static bool ShouldShow => NewUpdateLogExist && TasHelperSettings.SubscribeWhatsNew && UpdateLogs.Keys.IsNotEmpty(); // in case i forget to write update logs lol

    public static Dictionary<string, List<string>> UpdateLogs = new();

    public static Version CurrentVersion => TASHelperModule.Instance.Metadata.Version;

    [Initialize]
    public static void Initialize() {
#if ShowAllLog
        TasHelperSettings.LastVersion = new Version(1, 0);
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
    }

    private static Version LastVersion;

    
    private static void AddLog(string version, List<string> updateLogs) {
        if (LastVersion < new Version(version)) {
            UpdateLogs.Add(version, updateLogs);
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
            foreach (string version in UpdateLogs.Keys) {
                subMenu.Add(new TextMenuExt.ButtonExt(version) { TextColor = Color.White, Scale = 0.7f * Vector2.One });
                foreach (string updateLog in UpdateLogs[version]) {
                    subMenu.Add(new TextMenuExt.ButtonExt(" - " + updateLog) { TextColor = Color.Gray, Scale = 0.6f * Vector2.One});
                }
            }
            subMenu.OnPressed += () => {
                TasHelperSettings.LastVersion = CurrentVersion.ToString();
            };
        });
    }
}