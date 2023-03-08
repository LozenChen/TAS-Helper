using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using TAS.Module;

namespace Celeste.Mod.TASHelper;

public class TASHelperModule : EverestModule {

    public static TASHelperModule Instance;
    public TASHelperModule() {
        Instance = this;
    }

    public override Type SettingsType => typeof(TASHelperSettings);
    public override void Load() {
        On.Monocle.Scene.BeforeUpdate += PatchBeforeUpdate;
        On.Monocle.Entity.Update += PatchUpdate;
        On.Monocle.Scene.AfterUpdate += PatchAfterUpdate;
        On.Monocle.Entity.DebugRender += PatchDebugRender;
        On.Celeste.Level.Render += HotkeysPressed;
        On.Celeste.CrystalStaticSpinner.ctor_Vector2_bool_CrystalColor += SpinnerRenderKiller;
        On.Celeste.CrystalStaticSpinner.Awake += CoreModeListenerKiller;
    }

    public override void Unload() {
        On.Monocle.Scene.BeforeUpdate -= PatchBeforeUpdate;
        On.Monocle.Entity.Update -= PatchUpdate;
        On.Monocle.Scene.AfterUpdate -= PatchAfterUpdate;
        On.Monocle.Entity.DebugRender -= PatchDebugRender;
        On.Celeste.Level.Render -= HotkeysPressed;
        On.Celeste.CrystalStaticSpinner.ctor_Vector2_bool_CrystalColor -= SpinnerRenderKiller;
        On.Celeste.CrystalStaticSpinner.Awake -= CoreModeListenerKiller;
    }

    public override void Initialize() {
        RenderHelper.Initialize();
        SpinnerHelper.Initialize();
    }

    public override void LoadContent(bool firstLoad) {
    }

    private static void PatchBeforeUpdate(On.Monocle.Scene.orig_BeforeUpdate orig, Scene self) {
        orig(self);
        if (self is Level) {
            PlayerPositionChanged = 0;
            PreviousCameraPos = (self as Level).Camera.Position;
            player = self.Tracker.GetEntity<Player>();
            TimeActive = self.TimeActive;
        }

    }
    private static void PatchUpdate(On.Monocle.Entity.orig_Update orig, Entity self) {
        orig(self);
        if (player != null && SpinnerHelper.HazardType(self) != null) {
            if (PlayerPositionChanged == 0) {
                PlayerPositionChanged++;
                PlayerPosition = player.Position;
                return;
            }
            else if (player.Position != PlayerPosition) {
                if (PlayerPositionChanged == 1) {
                    PreviousPlayerPosition = PlayerPosition;
                }
                PlayerPositionChanged++;
                PlayerPosition = player.Position;
            }
        }
    }
    private static void PatchAfterUpdate(On.Monocle.Scene.orig_AfterUpdate orig, Scene self) {
        if (self is Level) {
            CameraPosition = (self as Level).Camera.Position;
            if (player != null) {
                if (PlayerPositionChanged == 0) {
                    PlayerPositionChanged++;
                    PlayerPosition = player.Position;
                }
                CameraTowards = player.Position + (self as Level).CameraOffset;
            }
        }
        orig(self);
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

    #region some mess
    private static Vector2 PreviousCameraPos = Vector2.Zero;
    private static Vector2 CameraPosition = Vector2.Zero;
    private static Vector2 CameraTowards = Vector2.Zero;
    private static Vector2 PlayerPosition = Vector2.Zero;
    private static Vector2 PreviousPlayerPosition = Vector2.Zero;
    private static int PlayerPositionChanged = 0;
    private static Player? player;
    private static float TimeActive = 0;
    #endregion


    private static void PatchDebugRender(On.Monocle.Entity.orig_DebugRender orig, Entity self, Camera camera) {
        if (SpinnerHelper.HazardType(self) != null) {
            PatchDebugRenderHazard(orig, self, camera);
        }
        else if (self is Player) {
            PatchDebugRenderPlayer(orig, self, camera);
        }
        else {
            orig(self, camera);
        }
    }
    private static void PatchDebugRenderHazard(On.Monocle.Entity.orig_DebugRender orig, Entity self, Camera camera) {
        float offset = SpinnerHelper.GetOffset(self).Value;
        RenderHelper.DrawCycleHitboxColor(self, camera, TimeActive, offset, CameraPosition);
        // camera.Position is a bit different from CameraPosition
        if (TasHelperSettings.isUsingLoadRange) {
            RenderHelper.DrawLoadRangeCollider(self.Position, self.Width, self.Height, CameraPosition, SpinnerHelper.isLightning(self));
        }
        if (TasHelperSettings.isUsingCountDown && !SpinnerHelper.FarFromRange(self, PlayerPosition, CameraPosition, 0.25f)) {
            Vector2 CountdownPos;
            if (SpinnerHelper.isLightning(self)) {
                CountdownPos = self.Center + new Vector2(-2f, -4f);
            }
            else {
                CountdownPos = self.Position + (TasHelperSettings.isUsingLoadRange ? new Vector2(-2f, 2f) : new Vector2(-2f, -4f));
            }
            RenderHelper.DrawCountdown(CountdownPos, SpinnerHelper.PredictCountdown(TimeActive, offset, SpinnerHelper.isDust(self)));
        }
    }

    private static void PatchDebugRenderPlayer(On.Monocle.Entity.orig_DebugRender orig, Entity self, Camera camera) {
        if (TasHelperSettings.isUsingCameraTarget) {
            RenderHelper.DrawCameraTarget(PreviousCameraPos, CameraPosition, CameraTowards);
        }
        if (TasHelperSettings.isUsingNearPlayerRange) {
            RenderHelper.DrawNearPlayerRange(PlayerPosition, PreviousPlayerPosition, PlayerPositionChanged);
        }
        if (TasHelperSettings.isUsingInViewRange) {
            RenderHelper.DrawInViewRange(CameraPosition);
        }
        orig(self, camera);
        return;
    }


    private static void HotkeysPressed(On.Celeste.Level.orig_Render orig, Level self) {
        orig(self);
        TasHelperSettings.SettingsHotkeysPressed();
        TasHelperSettings.UpdateAuxiliaryVariable();
        // if you call Instance.SaveSettings() here, then the game will crash if you open Menu-Mod Options in a Level and close the menu.
        // i don't know why, but just never do this.
    }
}










