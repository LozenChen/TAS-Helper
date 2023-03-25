using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using System.Reflection;
using VivEntites = VivHelper.Entities;

namespace Celeste.Mod.TASHelper.Utils;
internal static class PlayerHelper {

    internal static Player? player;
    internal static Vector2 PreviousCameraPos = Vector2.Zero;
    internal static Vector2 CameraPosition = Vector2.Zero;
    internal static float CameraZoom = 1f;
    // only change when there is entity which needs Camera.Zoom in its InView() and ApplyCameraZoom is on
    internal static Vector2 CameraTowards = Vector2.Zero;
    internal static Vector2 PlayerPosition = Vector2.Zero;
    // Player's position when Hazards update
    internal static Vector2 PreviousPlayerPosition = Vector2.Zero;
    internal static Vector2 PlayerPositionBeforeCameraUpdate = Vector2.Zero;
    internal static int PlayerPositionChangedCount = 0;

    public static void Initialize() {
        if (ModUtils.FrostHelperInstalled) {
            PatchFrostSpinnerUpdate();
        }
        if (ModUtils.VivHelperInstalled) {
            PatchVivSpinnerUpdate();
        }
    }

    public static void Load() {
        On.Monocle.Scene.BeforeUpdate += PatchBeforeUpdate;
        On.Celeste.CrystalStaticSpinner.Update += PatchCrysSpinnerUpdate;
        On.Celeste.Lightning.Update += PatchLightningUpdate;
        On.Celeste.DustStaticSpinner.Update += PatchDustUpdate;
        On.Monocle.Scene.AfterUpdate += PatchAfterUpdate;
        typeof(Player).GetMethod("orig_Update").IlHook(PlayerPositionBeforeCameraUpdateIL);
    }

    public static void Unload() {
        On.Monocle.Scene.BeforeUpdate -= PatchBeforeUpdate;
        On.Celeste.CrystalStaticSpinner.Update -= PatchCrysSpinnerUpdate;
        On.Celeste.Lightning.Update -= PatchLightningUpdate;
        On.Celeste.DustStaticSpinner.Update -= PatchDustUpdate;
        On.Monocle.Scene.AfterUpdate -= PatchAfterUpdate;
    }

    private static void PatchBeforeUpdate(On.Monocle.Scene.orig_BeforeUpdate orig, Scene self) {
        orig(self);
        if (self is Level level) {
            PlayerPositionChangedCount = 0;
            PreviousCameraPos = level.Camera.Position;
            player = self.Tracker.GetEntity<Player>();
            CameraZoom = 1f;
        }
    }

    private static void PatchAfterUpdate(On.Monocle.Scene.orig_AfterUpdate orig, Scene self) {
        if (self is Level level) {
            CameraPosition = level.Camera.Position;
            if (player != null) {
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
            cursor.Emit(OpCodes.Ldfld, typeof(Entity).GetField("Position"));
            cursor.Emit(OpCodes.Stsfld, typeof(PlayerHelper).GetField("PlayerPositionBeforeCameraUpdate", BindingFlags.NonPublic | BindingFlags.Static));
        }
    }

    private static void PatchCrysSpinnerUpdate(On.Celeste.CrystalStaticSpinner.orig_Update orig, CrystalStaticSpinner self) {
        // some mod (like PandorasBox mod) will hook CrystalStaticSpinner.Update() (which still use orig(self) and thus should use Entity.Update()?)
        // i don't know why but it seems in this case, if we hook Entity.Update, it will not work
        // also note some Hazards (like CrysSpinner) will not always call base.Update()
        // frosthelper spinners even never call base.Update()
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

    private static void PatchFrostSpinnerUpdate() {
        typeof(FrostHelper.CustomSpinner).GetMethod("Update").IlHook((cursor, _) => {
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Action<Entity>>(PatchHazardUpdate);
        });
    }

    private static void PatchVivSpinnerUpdate() {
        typeof(VivEntites.CustomSpinner).GetMethod("Update").IlHook((cursor, _) => {
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Action<Entity>>(GetCameraZoom);
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Action<Entity>>(PatchHazardUpdate);
        });
    }

    private static void GetCameraZoom(Entity self) {
        if (TasHelperSettings.ApplyCameraZoom) {
            CameraZoom = (self.Scene as Level).Camera.Zoom;
        }
    }


    private static void PatchHazardUpdate(Entity self) {
        if (SpinnerHelper.HazardType(self) != null && player != null) {
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

}
