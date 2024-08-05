using Celeste.Mod.TASHelper.Gameplay.Spinner;
using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using TAS.EverestInterop;

namespace Celeste.Mod.TASHelper.Gameplay;
internal static class ActualPosition {

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
    internal static Vector2 CenterCameraPosition = Vector2.Zero;

    [Initialize]
    public static void Initialize() {
        RegularHook("FrostHelper", "FrostHelper.CustomSpinner");
        RegularHook("XaphanHelper", "Celeste.Mod.XaphanHelper.Entities.CustomSpinner");
        RegularHook("ChroniaHelper", "ChroniaHelper.Entities.SeamlessSpinner");
        RegularHook("ChronoHelper", "Celeste.Mod.ChronoHelper.Entities.ShatterSpinner");
        RegularHook("ChronoHelper", "Celeste.Mod.ChronoHelper.Entities.DarkLightning");

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

        void RegularHook(string modName, string typeName) {
            if (ModUtils.GetType(modName, typeName) is { } type) {
                type.GetMethod("Update").IlHook((cursor, _) => {
                    if (cursor.TryGotoNext(MoveType.AfterLabel, ins => ins.OpCode == OpCodes.Ret)) {
                        cursor.Emit(OpCodes.Ldarg_0);
                        cursor.EmitDelegate(PatchHazardUpdate);
                    }
                });
            }
        }
    }

    private static Type CassetteSpinnerType;
    private static bool IsCassetteSpinner(Entity self) {
        return self.GetType().IsSameOrSubclassOf(CassetteSpinnerType);
    }

    [Load]
    public static void Load() {
        EventOnHook._Scene.BeforeUpdate += PatchBeforeUpdate;
        EventOnHook._Scene.AfterUpdate += PatchAfterUpdate;
        On.Celeste.Lightning.Update += PatchLightningUpdate;
        On.Celeste.DustStaticSpinner.Update += PatchDustUpdate;
        typeof(Player).GetMethod("orig_Update").IlHook(PlayerPositionBeforeCameraUpdateIL);
        using (new DetourContext { After = new List<string> { "*" }, ID = "TAS Helper ActualPosition" }) { // ensure this is even before other mod hooks
            On.Celeste.Player.Update += OnPlayerUpdate;
        }
    }

    private static void OnPlayerUpdate(On.Celeste.Player.orig_Update orig, Player self) {
        PlayerPositionBeforeSelfUpdate = self.Position;
        orig(self);
    }

    [Unload]
    public static void Unload() {
        On.Celeste.Lightning.Update -= PatchLightningUpdate;
        On.Celeste.DustStaticSpinner.Update -= PatchDustUpdate;
        On.Celeste.Player.Update -= OnPlayerUpdate;
    }

    private static void PatchBeforeUpdate(Scene self) {
        if (TasHelperSettings.Enabled && self is Level level) {
            PlayerPositionChangedCount = 0;
            PreviousCameraPos = level.Camera.Position;
            CameraZoom = 1f;
            CameraPositionDict.Clear();
            InViewBoost = TasHelperSettings.UsingInViewRange;
        }
    }

    private static bool InViewBoost = false;

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
        if (TasHelperSettings.Enabled && !UltraFastForwarding && self.IsHazard() && playerInstance is Player player) {
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
}
