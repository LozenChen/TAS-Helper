//#define ShowAllLog
using Microsoft.Xna.Framework;
namespace Celeste.Mod.TASHelper.Module;

public static class WhatsNew {

    public static bool ShouldShowUpdateLog = false;

    public static Dictionary<string, List<string>> UpdateLogs = new();

    public static Version BrokenSaves = new Version(0, 0);
    public static void OnLoadSettings() {
#if ShowAllLog
        TasHelperSettings.LastVersion = new Version(1, 0);
#endif

        if (TasHelperSettings.LastVersion == BrokenSaves) {
            // the saved setting is broken or it's a first install
            ShouldShowUpdateLog = false;
        }
        else {
            ShouldShowUpdateLog = !TasHelperSettings.LastVersion.Equals(TasHelperSettings.CurrentVersion);
        }
        TasHelperSettings.LastVersion = new Version(TasHelperSettings.CurrentVersion.ToString());

        // this feat appears in v1.8.12 and i dont plan to write those past update logs...

        // we only introduce new features / bugfixes here. Optimizations are not included
        UpdateLogs.Add("1.8.12", new List<string>() { "Wind Speed Renderer. Enable it in \"More Options\" -> \"Show Wind Speed\".", "Add the \"What's New!\" page."});
    }
    public static Menu.EaseInSubMenu CreateWhatsNewSubMenu(TextMenu menu) {
        return new Menu.EaseInSubMenu(Menu.TASHelperMenu.ToDialogText("Whats New"), false).Apply(subMenu => {
            foreach (string version in UpdateLogs.Keys) {
                if (TasHelperSettings.LastVersion.CompareTo(new Version(version)) < 0) {
                    subMenu.Add(new TextMenuExt.ButtonExt(version) { TextColor = Color.White, Scale = 0.7f * Vector2.One });
                    foreach (string updateLog in UpdateLogs[version]) {
                        subMenu.Add(new TextMenuExt.ButtonExt(" - " + updateLog) { TextColor = Color.Gray, Scale = 0.6f * Vector2.One});
                    }
                }
            }
        });
    }
}