﻿using Celeste.Mod.TASHelper.Predictor;
using Microsoft.Xna.Framework;
using static Celeste.Mod.TASHelper.Module.TASHelperSettings;
using static Celeste.TextMenuExt;

namespace Celeste.Mod.TASHelper.Module.Menu;
public static class PredictorMenu {

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

    private static Color hlineColor = Color.Lerp(Color.Gray, Color.Black, 0.2f);

    private static HLine CreateHLine() {
        return new HLine(Color.Gray);
    }
    private static HLine CreateHLine(string text) {
        return new HLine(hlineColor, 20f, 0f, text, 0.4f);
    }

    internal static List<TextMenu.Item> Create_PageOnOff(TextMenu menu, bool inGame) {
        List<TextMenu.Item> page = new List<TextMenu.Item>();
        TextMenu.Item PredictItem;
        page.Add(PredictItem = new TextMenu.OnOff("Predictor Main Switch".ToDialogText(), TasHelperSettings.PredictFutureEnabled).Change((value) => TasHelperSettings.PredictFutureEnabled = value));
        page.AddDescriptionOnEnter(menu, PredictItem, "Predictor Description".ToDialogText());
        page.Add(new IntSliderExt("Timeline Length".ToDialogText(), 1, 999, TasHelperSettings.TimelineLength).Change((value) => {
            TasHelperSettings.TimelineLength = value;
            Predictor.PredictorCore.InitializeCachePeriod();
        }));
        page.Add(CreateHLine("Predict Start Conditions".ToDialogText()));
        page.Add(new TextMenu.OnOff("Predict On Frame Step".ToDialogText(), TasHelperSettings.PredictOnFrameStep).Change(value => TasHelperSettings.PredictOnFrameStep = value));
        page.Add(new TextMenu.OnOff("Predict On File Change".ToDialogText(), TasHelperSettings.PredictOnFileChange).Change(value => TasHelperSettings.PredictOnFileChange = value));
        page.Add(new TextMenu.OnOff("Predict On Hotkey Pressed".ToDialogText(), TasHelperSettings.PredictOnHotkeyPressed).Change(value => TasHelperSettings.PredictOnHotkeyPressed = value));
        page.Add(CreateHLine());
        return page;
    }

    internal static List<TextMenu.Item> Create_PageKeyframe_1(TextMenu menu, bool inGame) {
        List<TextMenu.Item> page = new List<TextMenu.Item>();
        TextMenu.Item mainSwitchItem = new TextMenu.OnOff("Use Key Frame".ToDialogText(), TasHelperSettings.UseKeyFrame).Change(value => TasHelperSettings.UseKeyFrame = value);
        page.Add(mainSwitchItem);
        page.AddDescriptionOnEnter(menu, mainSwitchItem, "Keyframe Description".ToDialogText());
        page.Add(new TextMenu.OnOff("Use Key Frame Time".ToDialogText(), TasHelperSettings.UseKeyFrameTime).Change(value => TasHelperSettings.UseKeyFrameTime = value));
        page.Add(CreateHLine("Key Frame Flags".ToDialogText()));
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
        page.Add(CreateHLine());
        return page;
    }

    internal static List<TextMenu.Item> Create_PageKeyframe_2(TextMenu menu, bool inGame) {
        List<TextMenu.Item> page = new List<TextMenu.Item>();
        page.Add(CreateHLine("Key Frame Flags".ToDialogText()));
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

        page.Add(new TextMenu.OnOff("Get Retained", TasHelperSettings.UseFlagGetRetained).Change(value => TasHelperSettings.UseFlagGetRetained = value));
        page.Add(new TextMenu.OnOff("Refill Dash", TasHelperSettings.UseFlagRefillDash).Change(value => TasHelperSettings.UseFlagRefillDash = value));
        page.Add(new TextMenu.OnOff("Respawn Point Change", TasHelperSettings.UseFlagRespawnPointChange).Change(value => TasHelperSettings.UseFlagRespawnPointChange = value));
        page.Add(new TextMenu.OnOff("Dead", TasHelperSettings.UseFlagDead).Change(value => TasHelperSettings.UseFlagDead = value));
        page.Add(CreateHLine());
        return page;
    }

    internal static List<TextMenu.Item> Create_PageStyle(TextMenu menu, bool inGame) {
        List<TextMenu.Item> page = new List<TextMenu.Item>();
        page.Add(new EnumerableSlider<TimelineFinestStyle>("Timeline Finest Scale".ToDialogText(), CreatePredictorScalesFinestOptions(), TasHelperSettings.TimelineFinestScale).Change(value => TasHelperSettings.TimelineFinestScale = value));
        page.Add(new EnumerableSlider<TimelineScales>("Timeline Fine Scale".ToDialogText(), CreatePredictorScalesOptions(), TasHelperSettings.TimelineFineScale).Change(value => TasHelperSettings.TimelineFineScale = value));
        page.Add(new EnumerableSlider<TimelineScales>("Timeline Coarse Scale".ToDialogText(), CreatePredictorScalesOptions(), TasHelperSettings.TimelineCoarseScale).Change(value => TasHelperSettings.TimelineCoarseScale = value));
        TextMenu.Item fadeoutItem;
        page.Add(fadeoutItem = new TextMenu.OnOff("Timeline FadeOut".ToDialogText(), TasHelperSettings.TimelineFadeOut).Change(value => TasHelperSettings.TimelineFadeOut = value));
        page.AddDescriptionOnEnter(menu, fadeoutItem, "Only Apply To Hitbox".ToDialogText());
        page.Add(new IntSliderExt("Predictor Line Width".ToDialogText(), 0, 20, TasHelperSettings.PredictorLineWidth).Change(value => TasHelperSettings.PredictorLineWidth = value));
        page.Add(new IntSliderExt("Predictor Point Size".ToDialogText(), 0, 20, TasHelperSettings.PredictorPointSize).Change(value => TasHelperSettings.PredictorPointSize = value));

        page.Add(new IntSliderExt("Predictor Font Size".ToDialogText(), 1, 20, TasHelperSettings.PredictorHiresFontSize).Change(value => { TasHelperSettings.PredictorHiresFontSize = value; PredictorTextRenderer.UpdateSettings(); }));
        page.Add(new IntSliderExt("Predictor Font Stroke".ToDialogText(), 0, 20, TasHelperSettings.PredictorHiresFontStroke).Change(value => { TasHelperSettings.PredictorHiresFontStroke = value; PredictorTextRenderer.UpdateSettings(); }));

        page.Add(CreateHLine());
        return page;
    }

    internal static List<TextMenu.Item> Create_PageOther(TextMenu menu, bool inGame) {
        List<TextMenu.Item> page = new List<TextMenu.Item>();
        page.Add(new TextMenu.OnOff("Autodrop Prediction".ToDialogText(), TasHelperSettings.DropPredictionWhenTasFileChange).Change(value => TasHelperSettings.DropPredictionWhenTasFileChange = value));

        page.Add(new TextMenu.OnOff("Allow Start Predict When Transition".ToDialogText(), TasHelperSettings.StartPredictWhenTransition).Change(value => {
            TasHelperSettings.StartPredictWhenTransition = value;
            Predictor.PredictorCore.InitializeChecks();
        }));
        page.Add(new TextMenu.OnOff("Stop Predict When Transition".ToDialogText(), TasHelperSettings.StopPredictWhenTransition).Change(value => {
            TasHelperSettings.StopPredictWhenTransition = value;
            Predictor.PredictorCore.InitializeChecks();
        }));
        page.Add(new TextMenu.OnOff("Stop Predict When Death".ToDialogText(), TasHelperSettings.StopPredictWhenDeath).Change(value => {
            TasHelperSettings.StopPredictWhenDeath = value;
            Predictor.PredictorCore.InitializeChecks();
        }));
        page.Add(new TextMenu.OnOff("Stop Predict When Keyframe".ToDialogText(), TasHelperSettings.StopPredictWhenKeyframe).Change(value => {
            TasHelperSettings.StopPredictWhenKeyframe = value;
            Predictor.PredictorCore.InitializeChecks();
        }));
        TextMenu.Item ultraSpeedItem = new IntSliderExt("Ultra Speed Lower Limit".ToDialogText(), 0, 325, TasHelperSettings.UltraSpeedLowerLimit).Change((value) => TasHelperSettings.UltraSpeedLowerLimit = value);
        page.Add(ultraSpeedItem);
        page.AddDescriptionOnEnter(menu, ultraSpeedItem, "Ultra Speed Lower Limit Description".ToDialogText());
        page.Add(CreateHLine());

        return page;
    }

    internal static IEnumerable<KeyValuePair<TimelineFinestStyle, string>> CreatePredictorScalesFinestOptions() {
        return new List<KeyValuePair<TimelineFinestStyle, string>> {
            new(TimelineFinestStyle.NotApplied, "Not Applied".ToDialogText()),
            new(TimelineFinestStyle.HitboxPerFrame, "Hitbox per Frame".ToDialogText()),
            new(TimelineFinestStyle.PolygonLine, "Polygon Line".ToDialogText()),
            new(TimelineFinestStyle.DottedPolygonLine, "Dotted Polygon Line".ToDialogText())
        };
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