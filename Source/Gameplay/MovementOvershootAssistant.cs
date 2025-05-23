using Celeste.Mod.TASHelper.ModInterop;
using Celeste.Mod.TASHelper.Module.Menu;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mod.TASHelper.Gameplay;

internal static class MovementOvershootAssistant {

    public static bool Enabled => TasHelperSettings.EnableMovementOvershootAssistant;

    public static bool AbovePlayer => TasHelperSettings.MOAAbovePlayer;

    // basic idea: visualize the difference between speed (if no obs) and velocity  (here, obs = obstruction)
    // but retained speed is also considered as speed ...? NO, unless it's the frame when you collide. We dont need this visualization for the remaining frames
    // the frame when you collide with ceiling, your vertical speed before colliding is also considered as speed
    // an onCollideH/V corner correction, should also be visualized

    // ok so let's make a relatively clear definition:
    // your no_obs_position is what your position will be after MoveH/V(Speed.X/Y * Engine.DeltaTime, onCollideH/V), AS IF onCollideH/V is null.
    // Movements after this are not considered (e.g. player collider, moving block pushing)

    // it behaves similar to ActualCollideHitbox if there's only moving blocks pushing/carrying you, but a bit different if you collide into solids on your own

    [Initialize]
    private static void Initialize() {
        ILHookConfig config = default;
        config.After = new List<string>() { "*" };
        config.ID = "TAS Helper Movement Overshoot Assistant";
        config.ManualApply = true;

        detour = new ILHook(typeof(Player).GetMethod("orig_Update"), HookOrigUpdate, config);
        UpdateDetourState();
    }

    private static IDetour detour;

    private static bool detourApplied = false;

    public static void UpdateDetourState() {
        UpdateDetourStateImpl(Enabled);
    }
    private static void UpdateDetourStateImpl(bool apply) {
        if (detour is null) {
            return;
        }
        if (apply && !detourApplied) {
            detour.Apply();
            detourApplied = true;
            if (Engine.Scene is Level level) {
                if (MOA_Renderer.Instance is not { } renderer || renderer.Scene != level) {
                    MOA_Renderer.OnLoadLevel(level);
                }
            }
        }
        else if (!apply && detourApplied) {
            detour.Undo();
            detourApplied = false;
            if (MOA_Renderer.Instance is { } renderer) {
                renderer.Visible = false;
            }
        }
    }

    [Unload]
    private static void Unload() {
        detour?.Dispose();
        detourApplied = false;
    }

    private static void HookOrigUpdate(ILContext il) {
        ILCursor cursor = new(il);
        if (cursor.TryGotoNext(ins => ins.OpCode == OpCodes.Ldarg_0, ins => ins.OpCode == OpCodes.Ldc_I4_0, ins => ins.MatchCallvirt<Player>("set_Ducking"))) {
            cursor.Index += 3;

            if (cursor.TryGotoNext(ins => ins.OpCode == OpCodes.Ldarg_0, ins => ins.MatchLdfld<Player>(nameof(Player.StateMachine)))) {
                cursor.MoveAfterLabels();
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate(GetNoObsPosition);
                cursor.Index += 2;

                if (cursor.TryGotoNext(ins => ins.OpCode == OpCodes.Ldarg_0, ins => ins.MatchLdfld<Player>(nameof(Player.StateMachine)), ins => ins.MatchCallvirt<StateMachine>("get_State"), ins => ins.OpCode == OpCodes.Ldc_I4_3)) {
                    cursor.MoveAfterLabels();
                    cursor.Emit(OpCodes.Ldarg_0);
                    cursor.EmitDelegate(ComparePosition);
                }
            }
        }
    }

    private static void GetNoObsPosition(Player player) {
        if (MOA_Renderer.Instance is not { } renderer) {
            return;
        }
        IsDreamDash = player.StateMachine.State == 9 || player.StateMachine.State == 22 || player.StateMachine.state.IsDreamTunnelDashState();
        if (IsDreamDash) {
            renderer.Visible = false;
            return;
        }
        NoObsPosition = NaiveMove(player.Position, player.movementCounter, player.Speed.GetGravityAffectedVector2() * Engine.DeltaTime);
        renderer.unselectableCollider = player.Collider.Clone();
        renderer.Position = NoObsPosition;
    }

    private static void ComparePosition(Player player) {
        if (!IsDreamDash && MOA_Renderer.Instance is { } renderer) {
            renderer.Visible = IsDifferent(player.Position, NoObsPosition) || IsDifferent(player.Collider.TopLeft, renderer.unselectableCollider.TopLeft) || IsDifferent(player.Collider.BottomRight, renderer.unselectableCollider.BottomRight + renderer.Position);
        }
    }

    public static Vector2 NoObsPosition; // does not include subpixel (though its calculation needs subpixel)

    private static bool IsDreamDash;

    private static Vector2 NaiveMove(Vector2 position, Vector2 movementCounter, Vector2 move) {
        position.X += (int)Math.Round(movementCounter.X + move.X, MidpointRounding.ToEven);
        position.Y += (int)Math.Round(movementCounter.Y + move.Y, MidpointRounding.ToEven);
        return position;
    }

    private static bool IsDifferent(Vector2 vec1, Vector2 vec2) {
        return Math.Abs(vec1.X - vec2.X) > 0.0001f || Math.Abs(vec1.Y - vec2.Y) > 0.0001f;
    }

    internal class MOA_Renderer : Entity {

        public static MOA_Renderer Instance = null;

        public static Color HitboxColor => CustomColors.MovementOvershootAssistantColor;

        public Collider unselectableCollider;

        public MOA_Renderer() {
            Depth = AbovePlayer ? -1 : 1;
            unselectableCollider = new Hitbox(8f, 11f, -4f, -11f); // we use this to avoid captured by FindClickedEntities
            Collidable = false;
            Visible = false;
            if (Instance is not null) {
                Instance.Visible = false;
                Engine.Scene.Remove(Instance);
            }

            // bug report: https://discord.com/channels/403698615446536203/666197738026827786/1233483296827117618
            if (this is not null) {
                Instance = this;
            }
        }

        public override void DebugRender(Camera camera) {
            if (Visible && Scene is Level level && !level.Transitioning && unselectableCollider is Hitbox hb) {
                DrawHollowRect(hb.Left + X, hb.Top + Y, hb.Width, hb.Height, HitboxColor);  // we use this to avoid captured by ActualCollideHitbox
            }
        }

        [LoadLevel]
        internal static void OnLoadLevel(Level self) {
            if (Enabled) {
                self.Add(new MOA_Renderer());
            }
        }

        private static void DrawHollowRect(float x, float y, float width, float height, Color color) {
            int fx = (int)Math.Floor(x);
            int fy = (int)Math.Floor(y);
            int cw = (int)Math.Ceiling(width + x - fx);
            int cy = (int)Math.Ceiling(height + y - fy);
            OrigHollowRect(fx, fy, cw, cy, color);
        }

        private static Rectangle rect = new Rectangle();
        private static Texture2D texture2d;
        private static Rectangle clip;
        private static SpriteBatch sb => Monocle.Draw.SpriteBatch;

        [Initialize]
        private static void Initialize() {
            // bug report: https://discord.com/channels/403698615446536203/666197738026827786/1233483296827117618
            // previously the init of texture2d is not here, maybe that causes a crash? idk, that never happens to me
            texture2d = Monocle.Draw.Pixel.Texture.Texture_Safe;
            clip = Monocle.Draw.Pixel.ClipRect;
        }

        private static void OrigHollowRect(int x, int y, int width, int height, Color color) {
            rect.X = x;
            rect.Y = y;
            rect.Width = width;
            rect.Height = 1;
            sb.Draw(texture2d, rect, clip, color);
            rect.Y += height - 1;
            sb.Draw(texture2d, rect, clip, color);
            rect.Y -= height - 1;
            rect.Width = 1;
            rect.Height = height;
            sb.Draw(texture2d, rect, clip, color);
            rect.X += width - 1;
            sb.Draw(texture2d, rect, clip, color);
        }
    }
}