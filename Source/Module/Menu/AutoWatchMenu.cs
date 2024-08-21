using Celeste.Mod.TASHelper.Gameplay.AutoWatchEntity;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.TASHelper.Module.Menu;
public static class AutoWatchMenu {

    internal static List<TextMenu.Item> Create_Page_1_OnOff(TextMenu menu) {
        List<TextMenu.Item> page = new List<TextMenu.Item>();
        TextMenu.Item mainSwitch;
        page.Add(mainSwitch = new TextMenu.OnOff("Auto Watch MainEnable".ToDialogText(), TasHelperSettings.AutoWatchEnable).Change(value => TasHelperSettings.AutoWatchEnable = value));
        page.AddDescriptionOnEnter(menu, mainSwitch, "Auto Watch Description".ToDialogText());
        page.Add(new IntSliderExt("Auto Watch Font Size".ToDialogText(), 1, 20, TasHelperSettings.AutoWatch_FontSize).Change(value => TasHelperSettings.AutoWatch_FontSize = value));
        page.Add(new IntSliderExt("Auto Watch Font Stroke".ToDialogText(), 0, 20, TasHelperSettings.AutoWatch_FontStroke).Change(value => TasHelperSettings.AutoWatch_FontStroke = value));

        page.Add(new HLine(Color.Gray));
        return page;
    }


    internal static List<TextMenu.Item> Create_Page2(TextMenu menu) {
        List<TextMenu.Item> page = new List<TextMenu.Item>();
        page.Add(new EnumerableSliderExt<RenderMode>("Auto Watch Player".ToDialogText(),
            CreateOptions(), TasHelperSettings.AutoWatch_Player).Change(value => TasHelperSettings.AutoWatch_Player = value));
        page.Add(new TextMenu.OnOff("Auto Watch DashAttackTimer".ToDialogText(), TasHelperSettings.AutoWatch_ShowDashAttackTimer).Change(value => TasHelperSettings.AutoWatch_ShowDashAttackTimer = value));
        page.Add(new TextMenu.OnOff("Auto Watch DashTimer".ToDialogText(), TasHelperSettings.AutoWatch_ShowDashTimer).Change(value => TasHelperSettings.AutoWatch_ShowDashTimer = value));
        page.Add(new TextMenu.OnOff("Auto Watch DreamDashCanEndTimer".ToDialogText(), TasHelperSettings.AutoWatch_ShowDreamDashCanEndTimer).Change(value => TasHelperSettings.AutoWatch_ShowDreamDashCanEndTimer = value));
        page.Add(new TextMenu.OnOff("Auto Watch GliderBoostTimer".ToDialogText(), TasHelperSettings.AutoWatch_ShowPlayerGliderBoostTimer).Change(value => TasHelperSettings.AutoWatch_ShowPlayerGliderBoostTimer = value));
        page.Add(new TextMenu.OnOff("Auto Watch WallBoostTimer".ToDialogText(), TasHelperSettings.AutoWatch_ShowWallBoostTimer).Change(value => TasHelperSettings.AutoWatch_ShowWallBoostTimer = value));

        page.Add(new EnumerableSliderExt<RenderMode>("Auto Watch Cutscene".ToDialogText(),
            CreateOnlyTwoOptions(), TasHelperSettings.AutoWatch_Cutscene).Change(value => TasHelperSettings.AutoWatch_Cutscene = value));

        page.Add(new HLine(Color.Gray));
        return page;
    }
    internal static List<TextMenu.Item> Create_Page3(TextMenu menu) {
        List<TextMenu.Item> page = new List<TextMenu.Item>();
        page.Add(new EnumerableSliderExt<RenderMode>("Auto Watch Booster".ToDialogText(),
            CreateOptions(), TasHelperSettings.AutoWatch_Booster).Change(value => TasHelperSettings.AutoWatch_Booster = value));

        page.Add(new EnumerableSliderExt<RenderMode>("Auto Watch Cloud".ToDialogText(),
            CreateOptions(), TasHelperSettings.AutoWatch_Cloud).Change(value => TasHelperSettings.AutoWatch_Cloud = value));

        page.Add(new EnumerableSliderExt<RenderMode>("Auto Watch FallingBlock".ToDialogText(),
            CreateOptions(), TasHelperSettings.AutoWatch_FallingBlock).Change(value => TasHelperSettings.AutoWatch_FallingBlock = value));

        page.Add(new EnumerableSliderExt<RenderMode>("Auto Watch Jelly".ToDialogText(),
            CreateOptions(), TasHelperSettings.AutoWatch_Jelly).Change(value => TasHelperSettings.AutoWatch_Jelly = value));

        page.Add(new EnumerableSliderExt<RenderMode>("Auto Watch Kevin".ToDialogText(),
            CreateOptions(), TasHelperSettings.AutoWatch_Kevin).Change(value => TasHelperSettings.AutoWatch_Kevin = value));

        page.Add(new EnumerableSliderExt<RenderMode>("Auto Watch MoonBlock".ToDialogText(),
            CreateOptions(), TasHelperSettings.AutoWatch_MoonBlock).Change(value => TasHelperSettings.AutoWatch_MoonBlock = value));

        page.Add(new EnumerableSliderExt<RenderMode>("Auto Watch MoveBlock".ToDialogText(),
            CreateOptions(), TasHelperSettings.AutoWatch_MoveBlock).Change(value => TasHelperSettings.AutoWatch_MoveBlock = value));


        page.Add(new EnumerableSliderExt<RenderMode>("Auto Watch Refill".ToDialogText(),
            CreateOptions(), TasHelperSettings.AutoWatch_Refill).Change(value => TasHelperSettings.AutoWatch_Refill = value));

        page.Add(new EnumerableSliderExt<RenderMode>("Auto Watch SwapBlock".ToDialogText(),
            CreateOptions(), TasHelperSettings.AutoWatch_SwapBlock).Change(value => TasHelperSettings.AutoWatch_SwapBlock = value));

        page.Add(new EnumerableSliderExt<RenderMode>("Auto Watch TheoCrystal".ToDialogText(),
            CreateOptions(), TasHelperSettings.AutoWatch_TheoCrystal).Change(value => TasHelperSettings.AutoWatch_TheoCrystal = value));
        page.Add(new EnumerableSliderExt<RenderMode>("Auto Watch ZipMover".ToDialogText(),
            CreateOptions(), TasHelperSettings.AutoWatch_ZipMover).Change(value => TasHelperSettings.AutoWatch_ZipMover = value));

        page.Add(new HLine(Color.Gray));
        return page;
    }


    private static IEnumerable<KeyValuePair<RenderMode, string>> CreateOptions() {
        return new List<KeyValuePair<RenderMode, string>> {
            new(RenderMode.Never, "Auto Watch Mode Never".ToDialogText()),
            new(RenderMode.WhenWatched, "Auto Watch Mode When Watched".ToDialogText()),
            new(RenderMode.Always, "Auto Watch Mode Always".ToDialogText()),
        };
    }

    private static IEnumerable<KeyValuePair<RenderMode, string>> CreateOnlyTwoOptions() { // some entity can't be clicked, so "when watched" doesn't make sense
        return new List<KeyValuePair<RenderMode, string>> {
            new(RenderMode.Never, "Auto Watch Mode Never".ToDialogText()),
            new(RenderMode.Always, "Auto Watch Mode Always".ToDialogText()),
        };
    }
}