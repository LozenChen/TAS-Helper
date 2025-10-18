using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;

namespace Celeste.Mod.TASHelper.Gameplay.Spinner.Info;
internal static class PositionHelper {

    internal static Vector2 PreviousCameraPos = Vector2.Zero;
    internal static Vector2 CameraPosition = Vector2.Zero;

    // new impl
    internal static Dictionary<int, Vector2> CameraPositionDict = new();
    internal static List<Vector2> CameraPositionSet = new();
    internal static Vector2 CameraPositionSetLastElement = Vector2.Zero;

    internal static float CameraZoom = 1f;
    // only change when there is entity which needs Camera.Zoom in its InView() and ApplyCameraZoom is on
    internal static Vector2 CameraTowards = Vector2.Zero;
    internal static Vector2 PlayerPosition = Vector2.Zero;
    // Player's position when Hazards update
    internal static Vector2 PlayerPositionBeforeSelfUpdate = Vector2.Zero;
    internal static Vector2 PreviousPlayerPosition = Vector2.Zero;
    internal static Vector2 PlayerPositionBeforeCameraUpdate = Vector2.Zero;
    internal static int PlayerPositionChangedCount = 0;

    private static Dictionary<Entity, Vector2> InviewCheckPosition = new Dictionary<Entity, Vector2>();

    public static Vector2 GetInviewCheckPosition(Entity entity) {
        if (InviewCheckPosition.TryGetValue(entity, out var position)) {
            return position;
        }
        return entity.Position;
    }

    public static Vector2 GetInviewCheckCenter(Entity entity) {
        if (InviewCheckPosition.TryGetValue(entity, out var position)) {
            if (entity.Collider is not null) {
                return position + entity.Collider.Center;
            }
            return position;
        }
        return entity.Center;
    }

    [Initialize(int.MinValue)]
    private static void Initialize() {
        if (ModUtils.GetType("VivHelper", "VivHelper.Entities.CustomSpinner") is { } vivSpinnerType) {
            vivSpinnerType.GetMethod("Update")!.ILHook((cursor, _) => {
                if (cursor.TryGotoNext(MoveType.AfterLabel, ins => ins.OpCode == OpCodes.Ret)) {
                    cursor.Emit(OpCodes.Ldarg_0);
                    cursor.EmitDelegate(GetCameraZoom);
                }
            });
            // also applies to VivHelper.Entities.AnimatedSpinner, MovingSpinner
        }

        typeof(CrystalStaticSpinner).GetMethod("Update")!.ILHook((cursor, _) => {
            if (cursor.TryGotoNext(MoveType.AfterLabel, ins => ins.OpCode == OpCodes.Ret)) {
                if (SpecialInfoHelper.CassetteSpinnerType is not null) {
                    Instruction gotoRet = cursor.Next;
                    cursor.Emit(OpCodes.Ldarg_0);
                    cursor.EmitDelegate(SpecialInfoHelper.IsCassetteSpinner);
                    cursor.Emit(OpCodes.Brtrue, gotoRet); // should ignore BrokemiaHelper.CassetteSpinner
                }
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate(PatchHazardUpdate);
            }
        });
    }

    [Load]
    private static void Load() {
        On.Celeste.Lightning.Update += PatchLightningUpdate;
        On.Celeste.DustStaticSpinner.Update += PatchDustUpdate;
        typeof(Player).GetMethod("orig_Update").ILHook(PlayerPositionBeforeCameraUpdateIL);
        using (DetourContextHelper.Use(After: new List<string> { "*" }, ID: "TAS Helper ActualPosition")) { // ensure this is even before other mod hooks
            On.Celeste.Player.Update += OnPlayerUpdate;
        }
    }

    [Unload]
    private static void Unload() {
        On.Celeste.Lightning.Update -= PatchLightningUpdate;
        On.Celeste.DustStaticSpinner.Update -= PatchDustUpdate;
        On.Celeste.Player.Update -= OnPlayerUpdate;
    }

    private static void OnPlayerUpdate(On.Celeste.Player.orig_Update orig, Player self) {
        PlayerPositionBeforeSelfUpdate = self.Position;
        orig(self);
    }

    [SceneBeforeUpdate]
    private static void PatchBeforeUpdate(Scene self) {
        if (TasHelperSettings.Enabled && self is Level level) {
            PlayerPositionChangedCount = 0;
            PreviousCameraPos = level.Camera.Position;
            CameraZoom = 1f;
            CameraPositionDict.Clear();
            InViewBoost = TasHelperSettings.UsingInViewRange;
            InviewCheckPosition.Clear();
        }
    }

    private static bool InViewBoost = false;

    [SceneAfterUpdate]
    private static void PatchAfterUpdate(Scene self) {
        if (TasHelperSettings.Enabled && self is Level level) {
            CameraPosition = level.Camera.Position;
            CameraPositionSet = CameraPositionDict.Values.Distinct().ToList();
            if (CameraPositionSet.IsEmpty()) {
                CameraPositionSetLastElement = CameraPosition; // add a precise one instead
            }
            else {
                CameraPositionSet.Remove(CameraPositionSetLastElement);
            }

            if (playerInstance is Player player) {
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
        PlayerPositionBeforeCameraUpdate = player.Position;
    }

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
        if (TasHelperSettings.Enabled && !FastForwarding && self.IsHazard() && playerInstance is Player player) {
            InviewCheckPosition[self] = self.Position;
            if (InViewBoost && !self.IsDust() && !CameraPositionDict.ContainsKey(self.Depth) && self.Scene is Level level) {
                TrackCameraPosition(self.Depth, level.Camera.Position);
            }
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

    private static void TrackCameraPosition(int depth, Vector2 vec) {
        CameraPositionDict.Add(depth, vec);
        CameraPositionSetLastElement = vec;
    }

    internal static void Patch(Type type) {
        type.GetMethod("Update")?.ILHook((cursor, _) => {
            if (cursor.TryGotoNext(MoveType.AfterLabel, ins => ins.OpCode == OpCodes.Ret)) {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate(PatchHazardUpdate);
            }
        });
    }

    internal static void OnClone() {
        CameraPositionDict.Clear();
        InviewCheckPosition.Clear();
    }
}