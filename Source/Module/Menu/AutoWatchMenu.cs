using Celeste.Mod.TASHelper.Gameplay.AutoWatchEntity;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.TASHelper.Module.Menu;
public static class AutoWatchMenu {

    internal static List<TextMenu.Item> Create_Page1(TextMenu menu) {
        List<TextMenu.Item> page = new List<TextMenu.Item>();
        TextMenu.Item zipMover;
        page.Add(zipMover = new EnumerableSliderExt<RenderMode>("Auto Watch ZipMover".ToDialogText(),
            CreateOptions(), Config.ZipMover).Change(value => Config.ZipMover = value));

        page.AddDescriptionOnEnter(menu, zipMover, "Auto Watch ZipMover Description".ToDialogText());

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