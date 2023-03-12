using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;

namespace Celeste.Mod.TASHelper.Utils;
internal static class SpinnerSpritesHelper {

    public static void Load() {
        On.Celeste.CrystalStaticSpinner.ctor_Vector2_bool_CrystalColor += SpinnerRenderKiller;
        On.Celeste.CrystalStaticSpinner.Awake += CoreModeListenerKiller;
    }

    public static void Unload() {
        On.Celeste.CrystalStaticSpinner.ctor_Vector2_bool_CrystalColor -= SpinnerRenderKiller;
        On.Celeste.CrystalStaticSpinner.Awake -= CoreModeListenerKiller;
    }

    private static void SpinnerRenderKiller(On.Celeste.CrystalStaticSpinner.orig_ctor_Vector2_bool_CrystalColor orig, CrystalStaticSpinner self, Vector2 position, bool attachToSolid, CrystalColor color) {
        orig(self, position, attachToSolid, color);
        if (TasHelperSettings.EnableSimplifiedSpinner && TasHelperSettings.ClearSpinnerSprites) {
            DynamicData SpinnerData = DynamicData.For(self);
            SpinnerData.Set("expanded", true);
        }
    }

    private static void CoreModeListenerKiller(On.Celeste.CrystalStaticSpinner.orig_Awake orig, CrystalStaticSpinner self, Scene scene) {
        if (TasHelperSettings.EnableSimplifiedSpinner && TasHelperSettings.ClearSpinnerSprites) {
            if (self.Components != null) {
                foreach (Component component in self.Components) {
                    component.EntityAwake();
                }
            }
        }
        else {
            orig(self, scene);
        }
    }


}