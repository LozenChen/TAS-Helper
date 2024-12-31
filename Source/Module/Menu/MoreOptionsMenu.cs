using Celeste.Mod.TASHelper.Gameplay;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.TASHelper.Module.Menu;
public static class MoreOptionsMenu {

    internal static List<TextMenu.Item> Create_Page1(TextMenu menu) {
        List<TextMenu.Item> page = new List<TextMenu.Item>();
        page.Add(new TextMenu.OnOff("Spawn Point".ToDialogText(), TasHelperSettings.UsingSpawnPoint).Change((value) => TasHelperSettings.UsingSpawnPoint = value));
        page.Add(new IntSliderExt("Current Spawn Point Opacity".ToDialogText(), 1, 9, TasHelperSettings.CurrentSpawnPointOpacity).Change((value) => TasHelperSettings.CurrentSpawnPointOpacity = value));
        page.Add(new IntSliderExt("Other Spawn Point Opacity".ToDialogText(), 0, 9, TasHelperSettings.OtherSpawnPointOpacity).Change((value) => TasHelperSettings.OtherSpawnPointOpacity = value));
        page.Add(new HLine(Color.Gray));
        TextMenu.Item moaItem;
        page.Add(moaItem = new TextMenu.OnOff("Movement Overshoot Assistant".ToDialogText(), TasHelperSettings.EnableMovementOvershootAssistant).Change((value) => TasHelperSettings.EnableMovementOvershootAssistant = value));
        page.AddDescriptionOnEnter(menu, moaItem, "MOA Description".ToDialogText());
        page.Add(new TextMenu.OnOff("MOA Above Player".ToDialogText(), TasHelperSettings.MOAAbovePlayer).Change((value) => TasHelperSettings.MOAAbovePlayer = value));
        page.Add(new HLine(Color.Gray));
        TextMenu.Item cassetteBlock;
        page.Add(cassetteBlock = new TextMenu.OnOff("Cassette Block Helper".ToDialogText(), TasHelperSettings.EnableCassetteBlockHelper).Change((value) => TasHelperSettings.EnableCassetteBlockHelper = value));
        page.AddDescriptionOnEnter(menu, cassetteBlock, "Cassette Block Description".ToDialogText());
        page.Add(new TextMenu.OnOff("Cassette Block Helper Extra Info".ToDialogText(), TasHelperSettings.CassetteBlockHelperShowExtraInfo).Change((value) => {
            TasHelperSettings.CassetteBlockHelperShowExtraInfo = value;
            CassetteBlockHelper.CassetteBlockVisualizer.needReAlignment = true;
        }));
        page.Add(new EnumerableSliderExt<CassetteBlockHelper.Alignments>("Cassette Info Alignment".ToDialogText(),
            CreateCassetteBlockHelperAlignmentsOptions(), TasHelperSettings.CassetteBlockInfoAlignment).Change(value => TasHelperSettings.CassetteBlockInfoAlignment = value));
        page.Add(new HLine(Color.Gray));
        page.Add(new TextMenu.OnOff("Enable Pixel Grid".ToDialogText(), TasHelperSettings.EnablePixelGrid).Change(value => TasHelperSettings.EnablePixelGrid = value));
        page.Add(new IntSliderExt("Pixel Grid Width".ToDialogText(), 0, 50, TasHelperSettings.PixelGridWidth).Change(value => TasHelperSettings.PixelGridWidth = value));
        page.Add(new IntSliderExt("Pixel Grid Opacity".ToDialogText(), 1, 10, TasHelperSettings.PixelGridOpacity).Change(value => TasHelperSettings.PixelGridOpacity = value));
        page.Add(new HLine(Color.Gray));
        return page;
    }


    internal static List<TextMenu.Item> Create_Page2(TextMenu menu) {
        List<TextMenu.Item> page = new List<TextMenu.Item>();
        page.Add(new TextMenu.OnOff("Camera Target".ToDialogText(), TasHelperSettings.UsingCameraTarget).Change(value => TasHelperSettings.UsingCameraTarget = value));
        page.Add(new IntSliderExt("Camera Target Vector Opacity".ToDialogText(), 1, 9, TasHelperSettings.CameraTargetLinkOpacity).Change(value => TasHelperSettings.CameraTargetLinkOpacity = value));
        page.Add(new HLine(Color.Gray));
        page.Add(new TextMenu.OnOff("FireBall Track".ToDialogText(), TasHelperSettings.UsingFireBallTrack).Change(value => TasHelperSettings.UsingFireBallTrack = value));
        page.Add(new TextMenu.OnOff("RotateSpinner Track".ToDialogText(), TasHelperSettings.UsingRotateSpinnerTrack).Change(value => TasHelperSettings.UsingRotateSpinnerTrack = value));
        page.Add(new TextMenu.OnOff("TrackSpinner Track".ToDialogText(), TasHelperSettings.UsingTrackSpinnerTrack).Change(value => TasHelperSettings.UsingTrackSpinnerTrack = value));
        page.Add(new HLine(Color.Gray));
        TextMenu.Item OoOItem;
        page.Add(OoOItem = new TextMenu.OnOff("Order of Operation Stepping".ToDialogText(), TasHelperSettings.EnableOoO).Change(value => TasHelperSettings.EnableOoO = value));
        page.AddDescriptionOnEnter(menu, OoOItem, "Order of Operation Description".ToDialogText());
        page.Add(new HLine(Color.Gray));
        return page;
    }

    internal static List<TextMenu.Item> Create_Page3(TextMenu menu) {
        List<TextMenu.Item> page = new List<TextMenu.Item>();
        page.Add(new TextMenu.OnOff("Show Wind Speed".ToDialogText(), TasHelperSettings.ShowWindSpeed).Change(value => TasHelperSettings.ShowWindSpeed = value));
        TextMenu.Item EntityActivatorReminderItem;
        page.Add(EntityActivatorReminderItem = new TextMenu.OnOff("Entity Activator Reminder".ToDialogText(), TasHelperSettings.EntityActivatorReminder).Change((value) => TasHelperSettings.EntityActivatorReminder = value));
        page.AddDescriptionOnEnter(menu, EntityActivatorReminderItem, "Entity Activator Reminder Description".ToDialogText());
        // page.Add(new TextMenu.OnOff("Open Console In Tas".ToDialogText(), TasHelperSettings.EnableOpenConsoleInTas).Change(value => TasHelperSettings.EnableOpenConsoleInTas = value));
        // page.Add(new TextMenu.OnOff("Scrollable History Log".ToDialogText(), TasHelperSettings.EnableScrollableHistoryLog).Change(value => TasHelperSettings.EnableScrollableHistoryLog = value));
        
        /*
        TextMenu.Item betterInvincible;
        page.Add(betterInvincible = new TextMenu.OnOff("Better Invincibility".ToDialogText(), TasHelperSettings.BetterInvincible).Change(value => {
            TasHelperSettings.BetterInvincible = value;
            BetterInvincible.Invincible = false; // in case that value doesn't get reset for some unknown reason... yeah i have such bug report
        }));
        page.AddDescriptionOnEnter(menu, betterInvincible, "Better Invincible Description".ToDialogText());
        */

        page.Add(new IntSliderExt("SpeedrunTimer Opacity when TAS Pauses".ToDialogText(), 0, 10, TasHelperSettings.SpeedrunTimerDisplayOpacity).Change(value => TasHelperSettings.SpeedrunTimerDisplayOpacity = value));
        page.Add(new HLine(Color.Gray));
        TextMenu.Item subscribeWhatsNew;
        page.Add(subscribeWhatsNew = new TextMenu.OnOff("Subscribe Whats New".ToDialogText(), TasHelperSettings.SubscribeWhatsNew).Change(value => TasHelperSettings.SubscribeWhatsNew = value));
        page.AddDescriptionOnEnter(menu, subscribeWhatsNew, "Subscribe Whats New Description".ToDialogText());
        page.Add(new HLine(Color.Gray));
        return page;
    }

    private static IEnumerable<KeyValuePair<CassetteBlockHelper.Alignments, string>> CreateCassetteBlockHelperAlignmentsOptions() {
        return new List<KeyValuePair<CassetteBlockHelper.Alignments, string>> {
            new(CassetteBlockHelper.Alignments.TopRight, "Cassette Info TopRight".ToDialogText()),
            new(CassetteBlockHelper.Alignments.BottomRight, "Cassette Info BottomRight".ToDialogText()),
            new(CassetteBlockHelper.Alignments.TopLeft, "Cassette Info TopLeft".ToDialogText()),
            new(CassetteBlockHelper.Alignments.BottomLeft, "Cassette Info BottomLeft".ToDialogText()),
            new(CassetteBlockHelper.Alignments.None, "Cassette Info None".ToDialogText()),
        };
    }
}