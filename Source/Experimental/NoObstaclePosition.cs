using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;

namespace Celeste.Mod.TASHelper.Experimental;

internal static class NoObstaclePosition {
    public static bool Enabled = true;

    // basic idea: visualize the difference between speed and velocity
    // it's just a rough idea, as it's hard to say, if riding a moving block is considered as obstructed, or if wind move should be considered
    // in the case of colliding with a dream block, we still need to show it

    [Initialize]
    private static void Initialize() {
        typeof(Level).GetMethod("LoadNewPlayerForLevel", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).IlHook((cursor, _) => {
            while (cursor.TryGotoNext(MoveType.AfterLabel, i => i.OpCode == OpCodes.Ret)) {
                cursor.EmitDelegate(AddActionToPlayer);
                cursor.Index++;
            }
        });
    }

    private static Player AddActionToPlayer(Player player) {
        player.PreUpdate += GetBeforeUpdateData;
        player.PostUpdate += GetAfterUpdateData;
        return player;
    }


    private static void GetBeforeUpdateData(Entity entity) {
        if (entity is Player player) {
            lastX = player.ExactPosition.X;
            wallSpeedRetentionTimer = player.wallSpeedRetentionTimer;
        }
    }

    private static void GetAfterUpdateData(Entity entity) {
        if (entity is Player player) {
            if (wallSpeedRetentionTimer > 0f || player.wallSpeedRetentionTimer > 0f) {
                // it may happen that it just collides this frame
                noObsExtraX = lastX + player.wallSpeedRetained * Engine.DeltaTime - player.ExactPosition.X;
                hasData = true;
                return;
            }
        }
        hasData = false;
    }

    private static float lastX;

    private static float wallSpeedRetentionTimer;

    private static float noObsExtraX;

    private static bool hasData = true;

    internal class NoObsPositionRenderer : Entity {

        public bool Updated;

        public NoObsPositionRenderer() {
            Depth = 10;
            Collider = new Hitbox(8f, 11f, -4f, -11f);
        }

        public override void Update() {
            base.Update();
            Updated = false;
        }
        public void UpdateWhenRender() {
            if (hasData && playerInstance is Player player) {
                Visible = true;
                Collider = player.Collider.Clone();
                Position = new Vector2((float)Math.Round(player.ExactPosition.X + noObsExtraX), player.Position.Y);
            }
            else {
                Visible = false;
            }
        }

        public override void DebugRender(Camera camera) {
            if (!Updated) {
                UpdateWhenRender();
                Updated = true;
            }
            if (Visible) {
                base.DebugRender(camera);
            }
        }

        [LoadLevel]
        private static void OnLoadLevel(Level self) {
            self.Add(new NoObsPositionRenderer());
        }
    }
}