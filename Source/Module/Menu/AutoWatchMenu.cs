using Celeste.Mod.TASHelper.Gameplay.AutoWatchEntity;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.TASHelper.Module.Menu;
public static class AutoWatchMenu {

    internal static List<TextMenu.Item> Create_Page_OnOff(TextMenu menu) {
        List<TextMenu.Item> page = new List<TextMenu.Item>();
        page.Add(new TextMenu.OnOff("Auto Watch MainEnable".ToDialogText(), TasHelperSettings.AutoWatchEnable).Change(value => TasHelperSettings.AutoWatchEnable = value));
        page.Add(new HLine(Color.Gray));
        return page;
    }

    internal static List<TextMenu.Item> Create_Page2(TextMenu menu) {
        List<TextMenu.Item> page = new List<TextMenu.Item>();
        page.Add(new EnumerableSliderExt<RenderMode>("Auto Watch Booster".ToDialogText(),
            CreateOptions(), TasHelperSettings.Booster).Change(value => TasHelperSettings.Booster = value));

        page.Add(new EnumerableSliderExt<RenderMode>("Auto Watch Cloud".ToDialogText(),
            CreateOptions(), TasHelperSettings.Cloud).Change(value => TasHelperSettings.Cloud = value));

        page.Add(new EnumerableSliderExt<RenderMode>("Auto Watch FallingBlock".ToDialogText(),
            CreateOptions(), TasHelperSettings.FallingBlock).Change(value => TasHelperSettings.FallingBlock = value));

        page.Add(new EnumerableSliderExt<RenderMode>("Auto Watch Glider".ToDialogText(),
            CreateOptions(), TasHelperSettings.Glider).Change(value => TasHelperSettings.Glider = value));

        page.Add(new EnumerableSliderExt<RenderMode>("Auto Watch FloatySpaceBlock".ToDialogText(),
            CreateOptions(), TasHelperSettings.FloatySpaceBlock).Change(value => TasHelperSettings.FloatySpaceBlock = value));

        page.Add(new EnumerableSliderExt<RenderMode>("Auto Watch MoveBlock".ToDialogText(),
            CreateOptions(), TasHelperSettings.MoveBlock).Change(value => TasHelperSettings.MoveBlock = value));

        page.Add(new EnumerableSliderExt<RenderMode>("Auto Watch Refill".ToDialogText(),
            CreateOptions(), TasHelperSettings.Refill).Change(value => TasHelperSettings.Refill = value));

        page.Add(new EnumerableSliderExt<RenderMode>("Auto Watch SwapBlock".ToDialogText(),
            CreateOptions(), TasHelperSettings.SwapBlock).Change(value => TasHelperSettings.SwapBlock = value));

        page.Add(new EnumerableSliderExt<RenderMode>("Auto Watch TheoCrystal".ToDialogText(),
            CreateOptions(), TasHelperSettings.TheoCrystal).Change(value => TasHelperSettings.TheoCrystal = value));
        page.Add(new EnumerableSliderExt<RenderMode>("Auto Watch ZipMover".ToDialogText(),
            CreateOptions(), TasHelperSettings.ZipMover).Change(value => TasHelperSettings.ZipMover = value));

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
}