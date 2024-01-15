//#define PatchMoveHV

#if PatchMoveHV

using Monocle;
using Celeste;
using Microsoft.Xna.Framework;
using System;

namespace Celeste.Mod.TASHelper.Experimental;

internal static class PatchMoveHV {

    public static bool checkAbnormalCases = true;


    [Load]
    private static void Load() {
        On.Celeste.Actor.MoveHExact += HookMoveHExact;
    }

    [Unload]
    private static void Unload() {
        On.Celeste.Actor.MoveHExact -= HookMoveHExact;
    }

    private static bool HookMoveHExact(On.Celeste.Actor.orig_MoveHExact orig, Actor self, int moveH, Collision onCollide, Solid pusher) {
        // 2 - 3 times faster than orig when high speed (e.g. SuperDashing 9)
        // 1/4 faster than orig when low speed (e.g. 1A dashless)
        if (self.Collider is not { } collider) {
            self.X += moveH;
            return false;
        }
        if (Math.Abs(moveH) < 2
            || checkAbnormalCases && (
                   Math.Abs(moveH) > Int32.MaxValue / 2
                || collider.Left != (int)collider.Left
                || collider.Right != (int)collider.Right
                )
            ) {
            return orig(self, moveH, onCollide, pusher);
        }
        return PatchedMoveHExact(self, moveH, onCollide, pusher);
    }

    private static bool PatchedMoveHExact(Actor self, int moveH, Collision onCollide, Solid pusher) {
        Collider collider = self.Collider;
        int sign = Math.Sign(moveH);
        int left = (int)collider.Left;
        int right = (int)collider.Right;
        int width = right - left;

        // note that if we are exactly 1 px inside left wall, we can still go right
        self.X += sign;
        Entity result = null;

        if (sign > 0) {
            collider.Width = width + moveH - sign;
            foreach (Entity solid in self.Scene.Tracker.Entities[typeof(Solid)].Where(x => x.Collidable && x.Collider is not null)) {
                if (self.CollideCheck(solid)) {
                    // moveH leads to self.AbsoluteRight > solid.AbsoluteLeft
                    result = solid;
                    if (solid.Collider is Hitbox) {
                        collider.Width = (int)Math.Floor(solid.Left - self.X - left); // this must make Width / AbsRight smaller
                        if (collider.Width < width) {
                            // an "instant" collision
                            collider.Width = width - 1;
                            break;
                        }
                    }
                    else {
                        collider.Width--;
                        while (collider.Width >= width) {
                            if (!self.CollideCheck(solid)) {
                                break;
                            }
                            collider.Width--;
                        }
                        if (collider.Width < width) {
                            break;
                        }
                    }
                }
            }
            if (result is null) {
                self.X += moveH - 1;
                collider.Width = width;
                return false;
            }
            else {
                int dist = 1 + (int)(collider.Width - width);
                self.X += dist - 1;
                collider.Width = width;
                self.movementCounter.X = 0f;
                onCollide?.Invoke(new CollisionData {
                    Direction = Vector2.UnitX,
                    Moved = Vector2.UnitX * dist,
                    TargetPosition = self.Position + Vector2.UnitX * moveH,
                    Hit = (Platform)result,
                    Pusher = pusher
                });
                return true;
            }
        }
        else {
            collider.Left = left + moveH - sign;
            collider.Width = right - collider.Left;
            foreach (Entity solid in self.Scene.Tracker.Entities[typeof(Solid)].Where(x => x.Collidable)) {
                if (self.CollideCheck(solid)) {
                    result = solid;
                    if (solid.Collider is Hitbox) {
                        collider.Left = (int)Math.Ceiling(solid.Right - self.X);
                        if (collider.Left > left) {
                            collider.Left = left + 1;
                            break;
                        }
                        collider.Width = right - collider.Left;
                    }
                    else {
                        collider.Left++;
                        while (collider.Left <= left) {
                            if (!self.CollideCheck(solid)) {
                                break;
                            }
                            collider.Left++;
                        }
                        if (collider.Left > left) {
                            collider.Left = left + 1;
                            break;
                        }
                        collider.Width = right - collider.Left;
                    }
                }
            }
            if (result is null) {
                self.X += moveH + 1;
                collider.Left = left;
                collider.Width = width;
                return false;
            }
            else {
                int dist = -1 + (int)(collider.Left - left);
                self.X += dist + 1;
                collider.Left = left;
                collider.Width = width;
                self.movementCounter.X = 0f;
                onCollide?.Invoke(new CollisionData {
                    Direction = -Vector2.UnitX,
                    Moved = Vector2.UnitX * dist,
                    TargetPosition = self.Position + Vector2.UnitX * moveH,
                    Hit = (Platform)result,
                    Pusher = pusher
                });
                return true;
            }
        }
    }
}

#endif
