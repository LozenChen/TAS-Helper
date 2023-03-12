using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using FMOD.Studio;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using System.Reflection;
using TAS.Module;
using YamlDotNet.Core;

namespace Celeste.Mod.TASHelper;

public class TASHelperModule : EverestModule {

    public static TASHelperModule Instance;
    public TASHelperModule() {
        Instance = this;
    }

    public override Type SettingsType => typeof(TASHelperSettings);

    public static bool FrostHelperInstalled = false;
    public override void Load() {
        On.Monocle.Scene.BeforeUpdate += PatchBeforeUpdate;
        On.Celeste.CrystalStaticSpinner.Update += PatchCrysSpinnerUpdate;
        On.Celeste.Lightning.Update += PatchLightningUpdate;
        On.Celeste.DustStaticSpinner.Update += PatchDustUpdate;
        if (FrostHelperInstalled) {
            typeof(FrostHelper.CustomSpinner).GetMethod("Update").IlHook(PatchCustomHazardUpdate);
        }
        On.Monocle.Scene.AfterUpdate += PatchAfterUpdate;
        On.Monocle.Entity.DebugRender += PatchDebugRender;
        On.Celeste.Level.Render += HotkeysPressed;
        On.Monocle.EntityList.DebugRender += PatchEntityListDebugRender;
        IL.Celeste.Player.Update += PlayerPositionBeforeCameraUpdateIL;
        RenderHelper.Load();
        SpinnerSpritesHelper.Load();
        TASHelper.Utils.Debug.DebugHelper.Load();
    }

    public override void Unload() {
        On.Monocle.Scene.BeforeUpdate -= PatchBeforeUpdate;
        On.Celeste.CrystalStaticSpinner.Update -= PatchCrysSpinnerUpdate;
        On.Celeste.Lightning.Update -= PatchLightningUpdate;
        On.Celeste.DustStaticSpinner.Update -= PatchDustUpdate;
        On.Monocle.Scene.AfterUpdate -= PatchAfterUpdate;
        On.Monocle.Entity.DebugRender -= PatchDebugRender;
        On.Celeste.Level.Render -= HotkeysPressed;
        On.Monocle.EntityList.DebugRender -= PatchEntityListDebugRender;
        IL.Celeste.Player.Update -= PlayerPositionBeforeCameraUpdateIL;
        RenderHelper.Unload();
        SpinnerSpritesHelper.Unload(); 
        HookHelper.Unload();
        TASHelper.Utils.Debug.DebugHelper.Unload();
    }

    public override void Initialize() {
        RenderHelper.Initialize();
        SpinnerHelper.Initialize();
        FrostHelperInstalled = ModUtils.IsInstalled("FrostHelper");
    }

    public override void LoadContent(bool firstLoad) {
    }

    internal static Player? player;

    public override void CreateModMenuSection(TextMenu menu, bool inGame, EventInstance snapshot) {
        CreateModMenuSectionHeader(menu, inGame, snapshot);
        TASHelperMenu.CreateMenu(this, menu, inGame);
    }

    private static void PatchBeforeUpdate(On.Monocle.Scene.orig_BeforeUpdate orig, Scene self) {
        orig(self);
        if (self is Level level) {
            PlayerPositionChangedCount = 0;
            PreviousCameraPos = level.Camera.Position;
            TimeActive = self.TimeActive;
            player = self.Tracker.GetEntity<Player>();
        }
    }

    private static void PatchHazardUpdate(Entity self) {
        if (SpinnerHelper.HazardType(self)!= null && player != null) {
            if (PlayerPositionChangedCount == 0) {
                PlayerPositionChangedCount++;
                PlayerPosition = player.Position;
                return;
            }
            else if (player.Position != PlayerPosition) {
                if (PlayerPositionChangedCount == 1) {
                    PreviousPlayerPosition = PlayerPosition;
                }
                PlayerPositionChangedCount++;
                PlayerPosition = player.Position;
            }
        }
    }
    private static void PatchCrysSpinnerUpdate(On.Celeste.CrystalStaticSpinner.orig_Update orig, CrystalStaticSpinner self) {
        // some mod (like PandorasBox mod) will hook CrystalStaticSpinner.Update() (which still use orig(self) and thus should use Entity.Update()?)
        // i don't know why but it seems in this case, if we hook Entity.Update, it will not work
        // also note some Hazards (like CrysSpinner) will not always call base.Update()
        // so let's just hook them individually
        orig(self);
        PatchHazardUpdate(self);
    }
    private static void PatchDustUpdate(On.Celeste.DustStaticSpinner.orig_Update orig, DustStaticSpinner self) {
        orig(self);
        PatchHazardUpdate(self);
    }
    private static void PatchLightningUpdate(On.Celeste.Lightning.orig_Update orig, Lightning self) {
        orig(self);
        PatchHazardUpdate(self);
    }
    private static void PatchCustomHazardUpdate(ILContext il) {
        ILCursor ilcursor = new (il);
        ilcursor.Emit(OpCodes.Ldarg_0);
        ilcursor.EmitDelegate<Action<Entity>>(PatchHazardUpdate);
}
    
    private static void PatchAfterUpdate(On.Monocle.Scene.orig_AfterUpdate orig, Scene self) {
        if (self is Level level) {
            CameraPosition = level.Camera.Position;
            if (player!= null) {
                if (PlayerPositionChangedCount == 0) {
                    PlayerPositionChangedCount++;
                    PlayerPosition = player.Position;
                }
                CameraTowards = PlayerPositionBeforeCameraUpdate + level.CameraOffset;
            }
        }
        orig(self);
    }

    private static void PlayerPositionBeforeCameraUpdateIL(ILContext il) {
        ILCursor cursor = new ILCursor(il);
        if (cursor.TryGotoNext(MoveType.After,
                ins => ins.OpCode == OpCodes.Stfld && ins.Operand.ToString() == "System.Boolean Celeste.Player::StrawberriesBlocked"
                // stfld bool Celeste.Player::StrawberriesBlocked
            )) {
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldfld, typeof(Entity).GetField("Position"));
            cursor.Emit(OpCodes.Stfld, typeof(TASHelperModule).GetField("PlayerPositionBeforeCameraUpdate"));
        }
    }

    #region some mess
    private static Vector2 PreviousCameraPos = Vector2.Zero;
    private static Vector2 CameraPosition = Vector2.Zero;
    private static Vector2 CameraTowards = Vector2.Zero;
    private static Vector2 PlayerPosition = Vector2.Zero;
    // Player's position when Hazards update
    private static Vector2 PreviousPlayerPosition = Vector2.Zero;
    public static Vector2 PlayerPositionBeforeCameraUpdate = Vector2.Zero;
    private static int PlayerPositionChangedCount = 0;
    private static float TimeActive = 0;
    #endregion


    private static void PatchDebugRender(On.Monocle.Entity.orig_DebugRender orig, Entity self, Camera camera) {
        if (SpinnerHelper.HazardType(self) != null) {
            PatchDebugRenderHazard(orig, self, camera);
        }
        else {
            orig(self, camera);
        }
    }
    private static void PatchDebugRenderHazard(On.Monocle.Entity.orig_DebugRender orig, Entity self, Camera camera) {
        if (!TasHelperSettings.Enabled) {
            // let Celeste TAS do everything
            orig(self, camera);
            return;
        }
        if (SpinnerHelper.GetOffset(self) is not float offset) {
            return;
        }
        
        RenderHelper.DrawCycleHitboxColor(self, camera, TimeActive, offset, CameraPosition);
        // camera.Position is a bit different from CameraPosition, if you use CelesteTAS's center camera
        if (TasHelperSettings.UsingLoadRange) {
            RenderHelper.DrawLoadRangeCollider(self.Position, self.Width, self.Height, CameraPosition, SpinnerHelper.isLightning(self));
        }
        if (TasHelperSettings.isUsingCountDown && !SpinnerHelper.FarFromRange(self, PlayerPosition, CameraPosition, 0.25f)) {
            Vector2 CountdownPos;
            if (SpinnerHelper.isLightning(self)) {
                CountdownPos = self.Center + new Vector2(-2f, -4f);
            }
            else {
                CountdownPos = self.Position + (TasHelperSettings.UsingLoadRange ? new Vector2(-2f, 2f) : new Vector2(-2f, -4f));
            }
            RenderHelper.DrawCountdown(CountdownPos, SpinnerHelper.PredictCountdown(TimeActive, offset, SpinnerHelper.isDust(self)));
        }
    }

    private static void PatchEntityListDebugRender(On.Monocle.EntityList.orig_DebugRender orig, EntityList self, Camera camera) {
        orig(self, camera);
        if (TasHelperSettings.UsingCameraTarget) {
            RenderHelper.DrawCameraTarget(PreviousCameraPos, CameraPosition, CameraTowards);
        }
        if (TasHelperSettings.isUsingNearPlayerRange) {
            // to see whether it works, teleport to Farewell [a-01] and updash
            // (teleport modifies your actualDepth, otherwise you need to set depth, or just die in this room)
            RenderHelper.DrawNearPlayerRange(PlayerPosition, PreviousPlayerPosition, PlayerPositionChangedCount);
        }
        if (TasHelperSettings.isUsingInViewRange) {
            RenderHelper.DrawInViewRange(CameraPosition);
        }
    }


    private static void HotkeysPressed(On.Celeste.Level.orig_Render orig, Level self) {
        orig(self);
        TasHelperSettings.SettingsHotkeysPressed();
        TasHelperSettings.UpdateAuxiliaryVariable();
        // if you call Instance.SaveSettings() here, then the game will crash if you open Menu-Mod Options in a Level and close the menu.
        // i don't know why, but just never do this.
    }
}










