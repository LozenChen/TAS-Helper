using Celeste.Mod.TASHelper.Gameplay;
using Celeste.Mod.TASHelper.Module;
using Celeste.Mod.TASHelper.Module.Menu;
using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework;
using Monocle;
using static Celeste.Mod.TASHelper.Predictor.Core;

namespace Celeste.Mod.TASHelper.Predictor;
public class PredictorRenderer : Entity {

    public static Color ColorEndpoint => CustomColors.Predictor_EndpointColor;

    public static Color ColorFinestScale => CustomColors.Predictor_FinestScaleColor;

    public static Color ColorFineScale => CustomColors.Predictor_FineScaleColor;

    public static Color ColorCoarseScale => CustomColors.Predictor_CoarseScaleColor;

    public static Color ColorKeyframe => CustomColors.Predictor_KeyframeColor;

    private static readonly List<Tuple<RenderData, Color>> keyframeRenderData = new List<Tuple<RenderData, Color>>();

    public static bool keyframeMessageCached = false;

    private const string textRendererLabel = "PredictorKeyframe";

    public PredictorRenderer() {
        Depth = 1;
    }
    public static void ClearCachedMessage() {
        if (keyframeMessageCached) {
            TempTextRenderer.Clear(textRendererLabel);
            keyframeMessageCached = false;
        }
    }
    public override void DebugRender(Camera camera) {
        if (!TasHelperSettings.PredictFutureEnabled || !FrameStep) {
            ClearCachedMessage();
            return;
        }

        int count = Math.Min(futures.Count, TasHelperSettings.TimelineLength);

        foreach (RenderData data in futures) {
            if (data.index > count) {
                // those extra cached frames
                continue;
            }
            if (data.visible) {
                if (TasHelperSettings.UseKeyFrame && KeyframeColorGetter(data.Keyframe, out bool addTime) is Color colorKeyframe) {
                    keyframeRenderData.Add(new Tuple<RenderData, Color>(data with { addTime = addTime }, colorKeyframe));
                }
                else {
                    if (ColorSelector(data.index, count) is Color color) {
                        Draw.HollowRect(data.x, data.y, data.width, data.height, color);
                    }
                }
            }
        }

        // todo: add descriptions to some keyframeData addTime
        foreach (Tuple<RenderData, Color> data in keyframeRenderData) {
            RenderData keyframeData = data.Item1;
            Draw.HollowRect(keyframeData.x, keyframeData.y, keyframeData.width, keyframeData.height, data.Item2);
            if (!keyframeMessageCached) {
                if (TasHelperSettings.UseKeyFrameTime && keyframeData.addTime) {
                    HiresLevelRenderer.Add(new TempTextRenderer(keyframeData.index.ToString(), new Vector2(keyframeData.x + keyframeData.width / 2, keyframeData.y - 1f) * 6f, textRendererLabel));
                }
            }
            // render keyframes above normal frames
        }
        keyframeMessageCached = true;

        keyframeRenderData.Clear();
    }

    public static Color? KeyframeColorGetter(KeyframeType keyframe, out bool addTime) {
        addTime = true;
        if (TasHelperSettings.UseFlagDead && keyframe.Has(KeyframeType.GainDead)) {
            return ColorKeyframe;
        }
        if (TasHelperSettings.UseFlagGainCrouched && keyframe.Has(KeyframeType.GainDuck)) {
            return ColorKeyframe;
        }
        if (TasHelperSettings.UseFlagGainLevelControl && keyframe.Has(KeyframeType.GainLevelControl)) {
            return ColorKeyframe;
        }
        if (TasHelperSettings.UseFlagGainOnGround && keyframe.Has(KeyframeType.GainOnGround)) {
            return ColorKeyframe;
        }
        if (TasHelperSettings.UseFlagGainPlayerControl && keyframe.Has(KeyframeType.GainPlayerControl)) {
            return ColorKeyframe;
        }
        if (TasHelperSettings.UseFlagGainUltra && keyframe.Has(KeyframeType.GainUltra)) {
            return ColorKeyframe;
        }
        if (TasHelperSettings.UseFlagLoseCrouched && keyframe.Has(KeyframeType.LoseDuck)) {
            return ColorKeyframe;
        }
        if (TasHelperSettings.UseFlagLoseLevelControl && keyframe.Has(KeyframeType.LoseLevelControl)) {
            return ColorKeyframe;
        }
        if (TasHelperSettings.UseFlagLoseOnGround && keyframe.Has(KeyframeType.LoseOnGround)) {
            return ColorKeyframe;
        }
        if (TasHelperSettings.UseFlagLosePlayerControl && keyframe.Has(KeyframeType.LosePlayerControl)) {
            return ColorKeyframe;
        }
        if (TasHelperSettings.UseFlagOnBounce && keyframe.Has(KeyframeType.OnBounce)) {
            return ColorKeyframe;
        }
        if (TasHelperSettings.UseFlagOnEntityState && keyframe.Has(KeyframeType.OnEntityState)) {
            return ColorKeyframe;
        }
        if (TasHelperSettings.UseFlagRefillDash && keyframe.Has(KeyframeType.RefillDash)) {
            return ColorKeyframe;
        }
        if (TasHelperSettings.UseFlagRespawnPointChange && keyframe.Has(KeyframeType.RespawnPointChange)) {
            return ColorKeyframe;
        }
        if (TasHelperSettings.UseFlagCanDashInStLaunch && keyframe.Has(KeyframeType.CanDashInStLaunch)) {
            return ColorKeyframe;
        }
        if (TasHelperSettings.UseFlagGainFreeze && keyframe.Has(KeyframeType.BeginEngineFreeze)) {
            return ColorKeyframe;
        }
        if (TasHelperSettings.UseFlagLoseFreeze && keyframe.Has(KeyframeType.EndEngineFreeze)) {
            return ColorKeyframe;
        }
        if (TasHelperSettings.UseFlagGetRetained && keyframe.Has(KeyframeType.GetRetained)) {
            return ColorKeyframe;
        }

        addTime = false;
        return null;
    }

    public static Color? ColorSelector(int index, int count) {
        if (index == count) {
            return ColorEndpoint;
        }
        if (TasHelperSettings.TimelineCoarseScale != TASHelperSettings.TimelineScales.NotApplied && index % TASHelperSettings.ToInt(TasHelperSettings.TimelineCoarseScale) == 0) {
            return ColorCoarseScale * (TasHelperSettings.TimelineFadeOut ? (1 - 0.1f * Math.Min((float)index / FadeOutCoraseTillThisFrame, 1f)) : 1f);
        }
        if (TasHelperSettings.TimelineFineScale != TASHelperSettings.TimelineScales.NotApplied && index % TASHelperSettings.ToInt(TasHelperSettings.TimelineFineScale) == 0) {
            return ColorFineScale * (TasHelperSettings.TimelineFadeOut ? (1 - 0.3f * Math.Min((float)index / FadeOutFineTillThisFrame, 1f)) : 1f);
        }
        if (TasHelperSettings.TimelineFinestScale) {
            return ColorFinestScale * (TasHelperSettings.TimelineFadeOut ? (1 - 0.5f * Math.Min((float)index / FadeOutFinestTillThisFrame, 1f)) : 1f);
        }
        return null;
    }

    private const float FadeOutFinestTillThisFrame = 50f;

    private const float FadeOutFineTillThisFrame = 100f;

    private const float FadeOutCoraseTillThisFrame = 500f;

    [Load]
    public static void Load() {
        On.Celeste.Level.LoadLevel += OnLoadLevel;
    }

    [Unload]
    public static void Unload() {
        On.Celeste.Level.LoadLevel -= OnLoadLevel;
    }

    private static void OnLoadLevel(On.Celeste.Level.orig_LoadLevel orig, Level self, Player.IntroTypes introTypes, bool isFromLoader) {
        orig(self, introTypes, isFromLoader);
        self.Add(new PredictorRenderer());
    }
}
