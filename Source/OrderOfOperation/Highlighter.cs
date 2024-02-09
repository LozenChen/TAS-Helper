//#define OoO_Debug

using Microsoft.Xna.Framework;
using Monocle;
using static Celeste.Mod.TASHelper.OrderOfOperation.OoO_Core;

namespace Celeste.Mod.TASHelper.OrderOfOperation;

internal static class Highlighter {

    [AddDebugRender]
    private static void PatchEntityListDebugRender(EntityList self, Camera camera) {
        if (Applied) {
            UpdateTime();
            DebugRender(camera);
        }
    }
    public static float oscillator => Monocle.Calc.YoYo(time);

    public static float time = 0f;

    public static float speed = 0.033f;

    public static void UpdateTime() {
        time += speed;
        if (time > 1f) {
            time -= 1f;
        }
    }

    public static Entity trackedEntity => ForEachBreakPoints_EntityList.trackedEntity;

    public static PlayerCollider trackedPC => ForEachBreakPoints_PlayerCollider.trackedPC;

    public static Color HighLightColor => Color.Lerp(Color.Cyan, Color.Red, oscillator);

    public static void DebugRender(Camera camera) {
        // originally, i debugrender this via an entity, whose depth = trackedEntity/PC's depth - 1
        // however, if we do so, then we need to call EntityList.UpdateList()
        // in some worst cases, this will lead to Entities removed from scene
        // thus interfere the for-each breakpoints running
        // e.g. an entity which updates before Player, and become removed, will make Player's update only called once
        // .....
        // oh wait, we can just sort EntityList but don't remove/add entities
        // but rendering this above everything else is not so bad imo....
        if (trackedPC?.Entity is { } entity) {
            Collider collider = trackedPC.Collider;
            if (trackedPC.FeatherCollider != null && player is not null && player.StateMachine.state == 19) {
                collider = trackedPC.FeatherCollider;
            }
            collider ??= entity.Collider;
            collider?.Render(camera, HighLightColor);
        }
        else if (trackedEntity is not null) {
            if (trackedEntity.Collider is not null) {
                trackedEntity.Collider.Render(camera, HighLightColor);
            }
            else {
                Monocle.Draw.Point(trackedEntity.Position, HighLightColor);
            }
        }
    }
}