using Celeste.Mod.TASHelper.Gameplay.Spinner;
using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using TAS.EverestInterop;

namespace Celeste.Mod.TASHelper.Gameplay;
internal static class ActualPosition {

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
    internal static Vector2 CenterCameraPosition = Vector2.Zero;

    [Initialize]
    public static void Initialize() {
        if (ModUtils.GetType("FrostHelper", "FrostHelper.CustomSpinner") is { } frostSpinnerType) {
            frostSpinnerType.GetMethod("Update").IlHook((cursor, _) => {
                if (cursor.TryGotoNext(MoveType.AfterLabel, ins => ins.OpCode == OpCodes.Ret)) {
                    cursor.Emit(OpCodes.Ldarg_0);
                    cursor.EmitDelegate(PatchHazardUpdate);
                }
            });
        }
        if (ModUtils.GetType("VivHelper", "VivHelper.Entities.CustomSpinner") is { } vivSpinnerType) {
            vivSpinnerType.GetMethod("Update").IlHook((cursor, _) => {
                if (cursor.TryGotoNext(MoveType.AfterLabel, ins => ins.OpCode == OpCodes.Ret)) {
                    cursor.Emit(OpCodes.Ldarg_0);
                    cursor.EmitDelegate(GetCameraZoom);
                    cursor.Emit(OpCodes.Ldarg_0);
                    cursor.EmitDelegate(PatchHazardUpdate);
                }
            });
            // also applies to VivHelper.Entities.AnimatedSpinner, MovingSpinner
        }
        if (ModUtils.GetType("ChronoHelper", "Celeste.Mod.ChronoHelper.Entities.ShatterSpinner") is { } chronoSpinnerType && ModUtils.GetType("ChronoHelper", "Celeste.Mod.ChronoHelper.Entities.DarkLightning") is { } chronoLightningType) {
            chronoSpinnerType.GetMethod("Update").IlHook((cursor, _) => {
                if (cursor.TryGotoNext(MoveType.AfterLabel, ins => ins.OpCode == OpCodes.Ret)) {
                    cursor.Emit(OpCodes.Ldarg_0);
                    cursor.EmitDelegate(PatchHazardUpdate);
                }
            });
            chronoLightningType.GetMethod("Update").IlHook((cursor, _) => {
                if (cursor.TryGotoNext(MoveType.AfterLabel, ins => ins.OpCode == OpCodes.Ret)) {
                    cursor.Emit(OpCodes.Ldarg_0);
                    cursor.EmitDelegate(PatchHazardUpdate);
                }
            });
        }
        typeof(CrystalStaticSpinner).GetMethod("Update").IlHook((cursor, _) => {
            if (cursor.TryGotoNext(MoveType.AfterLabel, ins => ins.OpCode == OpCodes.Ret)) {
                if (ModUtils.GetType("BrokemiaHelper", "BrokemiaHelper.CassetteSpinner") is { } cassetteSpinnerType) {
                    Instruction gotoRet = cursor.Next;
                    CassetteSpinnerType = cassetteSpinnerType;
                    cursor.Emit(OpCodes.Ldarg_0);
                    cursor.EmitDelegate(IsCassetteSpinner);
                    cursor.Emit(OpCodes.Brtrue, gotoRet);
                }
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate(PatchHazardUpdate);
            }
        });
        if (ModUtils.IsaGrabBagInstalled) {
            // do nothing
        }

        typeof(CenterCamera).GetMethod("CenterTheCamera", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).HookAfter(() => {
            if (Engine.Scene is Level level) {
                CenterCameraPosition = level.Camera.Position;
            }
        });


    }

    private static Type CassetteSpinnerType;
    private static bool IsCassetteSpinner(Entity self) {
        return self.GetType().IsSameOrSubclassOf(CassetteSpinnerType);
    }

    [Load]
    public static void Load() {
        EventOnHook.Scene.BeforeUpdate += PatchBeforeUpdate;
        EventOnHook.Scene.AfterUpdate += PatchAfterUpdate;
        On.Celeste.Lightning.Update += PatchLightningUpdate;
        On.Celeste.DustStaticSpinner.Update += PatchDustUpdate;
        typeof(Player).GetMethod("orig_Update").IlHook(PlayerPositionBeforeCameraUpdateIL);
    }

    [Unload]
    public static void Unload() {
        On.Celeste.Lightning.Update -= PatchLightningUpdate;
        On.Celeste.DustStaticSpinner.Update -= PatchDustUpdate;
    }

    private static void PatchBeforeUpdate(Scene self) {
        if (TasHelperSettings.Enabled && self is Level level) {
            PlayerPositionChangedCount = 0;
            PreviousCameraPos = level.Camera.Position;
            CameraZoom = 1f;
        }
    }

    private static void PatchAfterUpdate(Scene self) {
        if (TasHelperSettings.Enabled && self is Level level) {
            CameraPosition = level.Camera.Position;
            if (player != null) {
                if (PlayerPositionChangedCount == 0) {
                    PlayerPositionChangedCount++;
                    PlayerPosition = player.Position;
                }
                CameraTowards = PlayerPositionBeforeCameraUpdate + level.CameraOffset;
            }
        }
    }

    private static void PlayerPositionBeforeCameraUpdateIL(ILContext il) {
        ILCursor cursor = new ILCursor(il);
        if (cursor.TryGotoNext(MoveType.After,
                ins => ins.OpCode == OpCodes.Stfld && ins.Operand.ToString() == "System.Boolean Celeste.Player::StrawberriesBlocked"
            // stfld bool Celeste.Player::StrawberriesBlocked
            )) {
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate(GetCameraPosition);
        }
    }

    private static void GetCameraPosition(Player player) {
        if (TasHelperSettings.Enabled) {
            PlayerPositionBeforeCameraUpdate = player.Position;
        }
    }

    //private static void PatchCrysSpinnerUpdate(On.Celeste.CrystalStaticSpinner.orig_Update orig, CrystalStaticSpinner self) {
    // some mod (like PandorasBox mod) will hook CrystalStaticSpinner.Update() (which still use orig(self) and thus should use Entity.Update()?)
    // i don't know why but it seems in this case, if we hook Entity.Update, it will not work
    // also note some Hazards (like CrysSpinner) will not always call base.Update()
    // frosthelper spinners even never call base.Update()
    // so let's just hook them individually

    // should not apply to BrokemiaHelper.CassetteSpinner
    // everything is moved to ilhook
    //}

    private static void PatchDustUpdate(On.Celeste.DustStaticSpinner.orig_Update orig, DustStaticSpinner self) {
        orig(self);
        PatchHazardUpdate(self);
    }
    private static void PatchLightningUpdate(On.Celeste.Lightning.orig_Update orig, Lightning self) {
        orig(self);
        PatchHazardUpdate(self);
        // also applies to FrostHelper.AttachedLightning
    }

    private static void GetCameraZoom(Entity self) {
#pragma warning disable CS8602
        if (TasHelperSettings.Enabled && TasHelperSettings.ApplyCameraZoom) {
            CameraZoom = (self.Scene as Level).Camera.Zoom;
        }
#pragma warning restore CS8602
    }


    private static void PatchHazardUpdate(Entity self) {
        if (TasHelperSettings.Enabled && !UltraFastForwarding && self.IsHazard() && player != null) {
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
