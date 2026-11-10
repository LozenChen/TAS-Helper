using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.TASHelper.OrderOfOperation;

// Pulsing cyan<->red highlight of the entity / PlayerCollider currently being stepped.
internal static class OoO_Highlight {
    internal static Entity TrackedEntity;
    internal static PlayerCollider TrackedCollider;

    private static float time;
    private static readonly System.Diagnostics.Stopwatch clock = System.Diagnostics.Stopwatch.StartNew();
    private static long lastTicks;

    [AddDebugRender]
    private static void PatchEntityListDebugRender(EntityList self, Camera camera) {
        if (!OoO_Controller.Active) {
            return;
        }
        // Advance by wall-clock time, not by render calls: while a breakpoint blocks we pump
        // hundreds of draw calls per second, which would make the pulse flash insanely fast.
        long now = clock.ElapsedTicks;
        float deltaSeconds = lastTicks == 0 ? 0f : (float)((now - lastTicks) / (double)System.Diagnostics.Stopwatch.Frequency);
        lastTicks = now;
        time += deltaSeconds * 2f; // one full cyan<->red cycle per ~0.5s, same as the old 60fps look
        if (time > 1f) {
            time -= 1f;
        }
        DebugRender(camera);
    }

    private static Color HighlightColor => Color.Lerp(Color.Cyan, Color.Red, Calc.YoYo(time));

    private static void DebugRender(Camera camera) {
        if (TrackedCollider?.Entity is { } entity) {
            Collider collider = TrackedCollider.Collider;
            if (TrackedCollider.FeatherCollider != null && Engine.Scene?.Tracker.GetEntity<Player>() is { } player && player.StateMachine.state == 19) {
                collider = TrackedCollider.FeatherCollider;
            }
            collider ??= entity.Collider;
            collider?.Render(camera, HighlightColor);
        }
        else if (TrackedEntity is not null) {
            if (TrackedEntity.Collider is not null) {
                TrackedEntity.Collider.Render(camera, HighlightColor);
            }
            else {
                Monocle.Draw.Point(TrackedEntity.Position, HighlightColor);
            }
        }
    }

    internal static void ResetRun() {
        TrackedEntity = null;
        TrackedCollider = null;
    }
}
