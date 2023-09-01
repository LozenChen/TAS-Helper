using Microsoft.Xna.Framework;
using Monocle;
using static Celeste.Mod.TASHelper.Predictor.Core;

namespace Celeste.Mod.TASHelper.Predictor;
public class PredictorRenderer : Entity {

    public static Color ColorFinal = Color.Green * 0.8f;

    public static Color ColorSegment = Color.Gold * 0.5f;

    public static Color ColorNormal = Color.Red * 0.2f;
    public override void DebugRender(Camera camera) {
        if (!TasHelperSettings.PredictFuture) {
            return;
        }

        foreach (RenderData data in futures) {
            if (data.visible) {
                Draw.HollowRect(data.x, data.y, data.width, data.height, data.KeyframeColor.GetValueOrDefault(ColorSelector(data.index, futures.Count)));
            }
        }
    }

    public static Color ColorSelector(int index, int count) {
        if (index == count) {
            return ColorFinal;
        }
        if (index % 5 == 0) {
            return ColorSegment;
        }
        return ColorNormal * (1 - 0.5f * (float)index / (float)count);
    }

    public static void Load() {
        On.Celeste.Level.LoadLevel += OnLoadLevel;
    }

    public static void Unload() {
        On.Celeste.Level.LoadLevel -= OnLoadLevel;
    }

    private static void OnLoadLevel(On.Celeste.Level.orig_LoadLevel orig, Level self, Player.IntroTypes introTypes, bool isFromLoader) {
        orig(self, introTypes, isFromLoader);
        self.Add(new PredictorRenderer());
    }
}
