using Microsoft.Xna.Framework;
using static Celeste.Mod.TASHelper.Module.TASHelperSettings;
using static Celeste.TextMenuExt;

namespace Celeste.Mod.TASHelper.Module.Menu;
public static class PredictorMenu {
    private static void AddDescriptionOnEnter(this List<TextMenu.Item> page, TextMenu menu, TextMenu.Item item, string description) {
        EaseInSubHeaderExt descriptionText = new(description, false, menu) {
            TextColor = Color.Gray,
            HeightExtra = 0f
        };
        page.Add(descriptionText);
        item.OnEnter += () => descriptionText.FadeVisible = true;
        item.OnLeave += () => descriptionText.FadeVisible = false;
    }

    private static void AddDescriptionBothControl(this List<TextMenu.Item> page, TextMenu menu, TextMenu.Item itemAbove, TextMenu.Item itemBelow, string description) {
        EaseInSubHeaderExt descriptionText = new(description, false, menu) {
            TextColor = Color.Gray,
            HeightExtra = 0f
        };
        page.Add(descriptionText);
        itemAbove.OnEnter += () => descriptionText.FadeVisible = true;
        itemBelow.OnEnter += () => descriptionText.FadeVisible = true;
        itemAbove.OnLeave += () => descriptionText.FadeVisible = false;
        itemBelow.OnLeave += () => descriptionText.FadeVisible = false;
    }

    internal static List<TextMenu.Item> Create_PageOnOff(TextMenu menu, bool inGame) {
        List<TextMenu.Item> page = new List<TextMenu.Item>();
        TextMenu.Item PredictItem;
        page.Add(PredictItem = new TextMenu.OnOff("Predictor Main Switch".ToDialogText(), TasHelperSettings.PredictFutureEnabled).Change((value) => TasHelperSettings.PredictFutureEnabled = value));
        page.AddDescriptionOnEnter(menu, PredictItem, "Predictor Description".ToDialogText());
        page.Add(new IntSlider("Timeline Length".ToDialogText(), 1, 999, TasHelperSettings.TimelineLength).Change((value) => {
            TasHelperSettings.TimelineLength = value;
            Predictor.Core.InitializeCachePeriod();
        }));

        page.Add(new SubHeaderExt("Predict Start Conditions".ToDialogText()) {
            TextColor = Color.Gray,
            HeightExtra = 0f
        });
        page.Add(new TextMenu.OnOff("Predict On Frame Step".ToDialogText(), TasHelperSettings.PredictOnFrameStep).Change(value => TasHelperSettings.PredictOnFrameStep = value));
        page.Add(new TextMenu.OnOff("Predict On Hotkey Pressed".ToDialogText(), TasHelperSettings.PredictOnHotkeyPressed).Change(value => TasHelperSettings.PredictOnHotkeyPressed = value));
        page.Add(new TextMenu.OnOff("Predict On File Change".ToDialogText(), TasHelperSettings.PredictOnFileChange).Change(value => TasHelperSettings.PredictOnFileChange = value));
        return page;
    }

    internal static List<TextMenu.Item> Create_PageKeyframe_1(TextMenu menu, bool inGame) {
        List<TextMenu.Item> page = new List<TextMenu.Item>();
        TextMenu.Item mainSwitchItem = new TextMenu.OnOff("Use Key Frame".ToDialogText(), TasHelperSettings.UseKeyFrame).Change(value => TasHelperSettings.UseKeyFrame = value);
        page.Add(mainSwitchItem);
        page.AddDescriptionOnEnter(menu, mainSwitchItem, "Keyframe Description".ToDialogText());
        page.Add(new TextMenu.OnOff("Use Key Frame Time".ToDialogText(), TasHelperSettings.UseKeyFrameTime).Change(value => TasHelperSettings.UseKeyFrameTime = value));
        page.Add(new SubHeaderExt("Key Frame Flags".ToDialogText()) {
            TextColor = Color.Gray,
            HeightExtra = 0f
        });
        TextMenu.Item gainLevelControlItem = new TextMenu.OnOff("Gain Level Control", TasHelperSettings.UseFlagGainLevelControl).Change(value => TasHelperSettings.UseFlagGainLevelControl = value);
        TextMenu.Item loseLevelControlItem = new TextMenu.OnOff("Lose Level Control", TasHelperSettings.UseFlagLoseLevelControl).Change(value => TasHelperSettings.UseFlagLoseLevelControl = value);

        TextMenu.Item gainPlayerControlItem = new TextMenu.OnOff("Gain Player Control", TasHelperSettings.UseFlagGainPlayerControl).Change(value => TasHelperSettings.UseFlagGainPlayerControl = value);
        TextMenu.Item losePlayerControlItem = new TextMenu.OnOff("Lose Player Control", TasHelperSettings.UseFlagLosePlayerControl).Change(value => TasHelperSettings.UseFlagLosePlayerControl = value);

        page.Add(gainLevelControlItem);
        page.AddDescriptionBothControl(menu, gainLevelControlItem, loseLevelControlItem, "Predictor Level Control Description".ToDialogText());
        page.Add(loseLevelControlItem);
        page.Add(gainPlayerControlItem);
        page.AddDescriptionBothControl(menu, gainPlayerControlItem, losePlayerControlItem, "Predictor Player Control Description".ToDialogText());
        page.Add(losePlayerControlItem);
        page.Add(new TextMenu.OnOff("Begin Engine Freeze", TasHelperSettings.UseFlagGainFreeze).Change(value => TasHelperSettings.UseFlagGainFreeze = value));
        page.Add(new TextMenu.OnOff("End Engine Freeze", TasHelperSettings.UseFlagLoseFreeze).Change(value => TasHelperSettings.UseFlagLoseFreeze = value));

        return page;
    }

    internal static List<TextMenu.Item> Create_PageKeyframe_2(TextMenu menu, bool inGame) {
        List<TextMenu.Item> page = new List<TextMenu.Item>();
        page.Add(new SubHeaderExt("Key Frame Flags".ToDialogText()) {
            TextColor = Color.Gray,
            HeightExtra = 0f
        });

        page.Add(new TextMenu.OnOff("Gain On Ground", TasHelperSettings.UseFlagGainOnGround).Change(value => TasHelperSettings.UseFlagGainOnGround = value));

        page.Add(new TextMenu.OnOff("Lose On Ground", TasHelperSettings.UseFlagLoseOnGround).Change(value => TasHelperSettings.UseFlagLoseOnGround = value));


        page.Add(new TextMenu.OnOff("Gain Ultra", TasHelperSettings.UseFlagGainUltra).Change(value => TasHelperSettings.UseFlagGainUltra = value));

        page.Add(new TextMenu.OnOff("On Bounce", TasHelperSettings.UseFlagOnBounce).Change(value => TasHelperSettings.UseFlagOnBounce = value));

        TextMenu.Item onEntityStateItem = new TextMenu.OnOff("On Entity State", TasHelperSettings.UseFlagOnEntityState).Change(value => TasHelperSettings.UseFlagOnEntityState = value);
        page.Add(onEntityStateItem);
        page.AddDescriptionOnEnter(menu, onEntityStateItem, "Predictor On Entity State Description".ToDialogText());

        page.Add(new TextMenu.OnOff("CanDash in StLaunch", TasHelperSettings.UseFlagCanDashInStLaunch).Change(value => TasHelperSettings.UseFlagCanDashInStLaunch = value));

        page.Add(new TextMenu.OnOff("Gain Crouched", TasHelperSettings.UseFlagGainCrouched).Change(value => TasHelperSettings.UseFlagGainCrouched = value));

        page.Add(new TextMenu.OnOff("Lose Crouched", TasHelperSettings.UseFlagLoseCrouched).Change(value => TasHelperSettings.UseFlagLoseCrouched = value));

        page.Add(new TextMenu.OnOff("Refill Dash", TasHelperSettings.UseFlagRefillDash).Change(value => TasHelperSettings.UseFlagRefillDash = value));
        page.Add(new TextMenu.OnOff("Respawn Point Change", TasHelperSettings.UseFlagRespawnPointChange).Change(value => TasHelperSettings.UseFlagRespawnPointChange = value));

        page.Add(new TextMenu.OnOff("Dead", TasHelperSettings.UseFlagDead).Change(value => TasHelperSettings.UseFlagDead = value));


        return page;
    }



    internal static List<TextMenu.Item> Create_PageOther(TextMenu menu, bool inGame) {
        List<TextMenu.Item> page = new List<TextMenu.Item>();
        page.Add(new EnumerableSlider<bool>("Timeline Finest Scale".ToDialogText(), CreateFinestScalesOptions(), TasHelperSettings.TimelineFinestScale).Change(value => TasHelperSettings.TimelineFinestScale = value));
        page.Add(new EnumerableSlider<TimelineScales>("Timeline Fine Scale".ToDialogText(), CreatePredictorScalesOptions(), TasHelperSettings.TimelineFineScale).Change(value => TasHelperSettings.TimelineFineScale = value));
        page.Add(new EnumerableSlider<TimelineScales>("Timeline Coarse Scale".ToDialogText(), CreatePredictorScalesOptions(), TasHelperSettings.TimelineCoarseScale).Change(value => TasHelperSettings.TimelineCoarseScale = value));

        page.Add(new TextMenu.OnOff("Timeline FadeOut".ToDialogText(), TasHelperSettings.TimelineFadeOut).Change(value => TasHelperSettings.TimelineFadeOut = value));

        page.Add(new TextMenu.OnOff("Autodrop Prediction".ToDialogText(), TasHelperSettings.DropPredictionWhenTasFileChange).Change(value => TasHelperSettings.DropPredictionWhenTasFileChange = value));

        page.Add(new TextMenu.OnOff("Allow Start Predict When Transition".ToDialogText(), TasHelperSettings.StartPredictWhenTransition).Change(value => {
            TasHelperSettings.StartPredictWhenTransition = value;
            Predictor.Core.InitializeChecks();
        }));
        page.Add(new TextMenu.OnOff("Stop Predict When Transition".ToDialogText(), TasHelperSettings.StopPredictWhenTransition).Change(value => {
            TasHelperSettings.StopPredictWhenTransition = value;
            Predictor.Core.InitializeChecks();
        }));
        page.Add(new TextMenu.OnOff("Stop Predict When Death".ToDialogText(), TasHelperSettings.StopPredictWhenDeath).Change(value => {
            TasHelperSettings.StopPredictWhenDeath = value;
            Predictor.Core.InitializeChecks();
        }));
        TextMenu.Item ultraSpeedItem = new IntSlider("Ultra Speed Lower Limit".ToDialogText(), 0, 325, TasHelperSettings.UltraSpeedLowerLimit).Change((value) => TasHelperSettings.UltraSpeedLowerLimit = value);
        page.Add(ultraSpeedItem);
        page.AddDescriptionOnEnter(menu, ultraSpeedItem, "Ultra Speed Lower Limit Description".ToDialogText());


        return page;
    }

    internal static IEnumerable<KeyValuePair<TimelineScales, string>> CreatePredictorScalesOptions() {
        return new List<KeyValuePair<TimelineScales, string>> {
            new(TimelineScales.NotApplied, "N/A"),
            new(TimelineScales._2, "2"),
            new(TimelineScales._5, "5"),
            new(TimelineScales._10, "10"),
            new(TimelineScales._15, "15"),
            new(TimelineScales._20, "20"),
            new(TimelineScales._25, "25"),
            new(TimelineScales._30, "30"),
            new(TimelineScales._45, "45"),
            new(TimelineScales._60, "60"),
            new(TimelineScales._100, "100"),
        };
    }

    internal static IEnumerable<KeyValuePair<bool, string>> CreateFinestScalesOptions() {
        return new List<KeyValuePair<bool, string>> {
            new(false, "N/A"),
            new(true, "1"),
        };
    }
}