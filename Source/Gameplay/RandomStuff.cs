//#define UseRandomStuff

#if UseRandomStuff

using Monocle;
using Celeste;
using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace Celeste.Mod.TASHelper.Gameplay;
internal class RandomStuff {
    // test some stuff, not actually in use

    [Load]
    private static void Load() {
        On.Celeste.JumpThru.MoveHExact += JumpThruMoveH_LiftSpeedPatch;
    }

    [Unload]
    private static void Unload() {
        On.Celeste.JumpThru.MoveHExact -= JumpThruMoveH_LiftSpeedPatch;
    }

    [Initialize]
    private static void Initialize() {
        Celeste.Commands.Log("WARNING: TAS Helper Random Stuff loaded! Please contact the author to disable these codes.");

        if (ModUtils.GetType("SomeStuff", "CustomCrushBlock") is { } type) { // a mod of AppleSheep
            type.GetMethodInfo("MoveHCheck").IlHook(il => {
                ILCursor cursor = new ILCursor(il);
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldarg_1);
                cursor.EmitDelegate(MoveHCheck);
                cursor.Emit(OpCodes.Ret);
            });

            type.GetMethodInfo("MoveVCheck").IlHook(il => {
                ILCursor cursor = new ILCursor(il);
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldarg_1);
                cursor.EmitDelegate(MoveVCheck);
                cursor.Emit(OpCodes.Ret);

            });
            type.GetMethod("MoveHExact").IlHook(il => {
                ILCursor cursor = new ILCursor(il);
                Instruction ins = cursor.Next;
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldarg_1);
                cursor.EmitDelegate(MoveHExact);
                cursor.Emit(OpCodes.Ret);
            });

            type.GetMethod("MoveVExact").IlHook(il => {
                ILCursor cursor = new ILCursor(il);
                Instruction ins = cursor.Next;
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldarg_1);
                cursor.EmitDelegate(MoveVExact);
                cursor.Emit(OpCodes.Ret);
            });
        };
    }

    private static void JumpThruMoveH_LiftSpeedPatch(On.Celeste.JumpThru.orig_MoveHExact orig, JumpThru self, int move) {
        if (self.Collidable) {
            foreach (Actor entity in self.Scene.Tracker.GetEntities<Actor>()) {
                if (entity.IsRiding(self)) {
                    entity.LiftSpeed = self.LiftSpeed;
                }
            }
        }
        orig(self, move);
    }

    private static void MoveHExact(Solid solid, int move) {
        solid.GetRiders();
        float right = solid.Right;
        float left = solid.Left;
        Player player = null;
        player = solid.Scene.Tracker.GetEntity<Player>();
        if (player != null && Input.MoveX.Value == Math.Sign(move) && Math.Sign(player.Speed.X) == Math.Sign(move) && !Solid.riders.Contains(player) && solid.CollideCheck(player, solid.Position + Vector2.UnitX * move - Vector2.UnitY)) {
            player.MoveV(1f);
        }
        solid.X += move;
        if (solid.Collidable) {
            foreach (Actor entity in solid.Scene.Tracker.GetEntities<Actor>()) {
                if (!entity.AllowPushing) {
                    continue;
                }
                bool collidable = entity.Collidable;
                entity.Collidable = true;
                if (!entity.TreatNaive && solid.CollideCheck(entity, solid.Position) && move != 0) {
                    int direction = Math.Sign(move);
                    int moveH = direction;
                    while (solid.CollideCheck(entity, solid.Position - Vector2.UnitX * moveH)) {
                        moveH += direction;
                    }
                    solid.Collidable = false;
                    entity.MoveHExact(moveH, entity.SquishCallback, solid);
                    entity.LiftSpeed = solid.LiftSpeed;
                    solid.Collidable = true;
                }
                else if (Solid.riders.Contains(entity)) {
                    solid.Collidable = false;
                    if (entity.TreatNaive) {
                        entity.NaiveMove(Vector2.UnitX * move);
                    }
                    else {
                        entity.MoveHExact(move);
                    }
                    entity.LiftSpeed = solid.LiftSpeed;
                    solid.Collidable = true;
                }
                entity.Collidable = collidable;
            }
        }
        Solid.riders.Clear();
        solid.MoveStaticMovers(Vector2.UnitX * move);
        // those static movers whose entities are solids, will also call GetRiders and riders.Clear(), so i move it here (and maybe make it more reasonable)
        // if we don't move it, then we need a Clear/GetRider pair enclosing it
    }

    private static void MoveVExact(Solid solid, int move) {
        solid.GetRiders();
        float bottom = solid.Bottom;
        float top = solid.Top;
        solid.Y += move;
        if (solid.Collidable) {
            foreach (Actor entity in solid.Scene.Tracker.GetEntities<Actor>()) {
                if (!entity.AllowPushing) {
                    continue;
                }
                bool collidable = entity.Collidable;
                entity.Collidable = true;
                if (!entity.TreatNaive && solid.CollideCheck(entity, solid.Position) && move != 0) {
                    int direction = Math.Sign(move);
                    int moveV = direction;
                    while (solid.CollideCheck(entity, solid.Position - Vector2.UnitY * moveV)) {
                        moveV += direction;
                    }
                    solid.Collidable = false;
                    entity.MoveVExact(moveV, entity.SquishCallback, solid);
                    entity.LiftSpeed = solid.LiftSpeed;
                    solid.Collidable = true;
                }
                else if (Solid.riders.Contains(entity)) {
                    solid.Collidable = false;
                    if (entity.TreatNaive) {
                        entity.NaiveMove(Vector2.UnitY * move);
                    }
                    else {
                        entity.MoveVExact(move);
                    }
                    entity.LiftSpeed = solid.LiftSpeed;
                    solid.Collidable = true;
                }
                entity.Collidable = collidable;
            }
        }
        Solid.riders.Clear();
        solid.MoveStaticMovers(Vector2.UnitY * move);
    }
    private static bool MoveHCheck(Solid baseSolid, float amount) {

        List<Solid> attachedSolids = baseSolid.staticMovers.Where(sm => sm.Entity is Solid { Collidable: true }).Select(sm => (Solid)sm.Entity).ToList();
        foreach (Solid attachedSolid in attachedSolids) {
            attachedSolid.Collidable = false;
        }
        Collider orig_collider = baseSolid.Collider;
        List<Collider> colliders = attachedSolids.Select(solid => CloneWithShift(baseSolid, solid)).ToList();
        colliders.Add(orig_collider);
        baseSolid.Collider = new ColliderList(colliders.ToArray());

        bool b = MoveHCheckCore(baseSolid, amount);

        baseSolid.Collider = orig_collider;

        foreach (Solid solid in attachedSolids) {
            solid.Collidable = true;
        }

        return b;

        Collider CloneWithShift(Solid baseSolid, Solid attachedSolid) {
            Collider collider = attachedSolid.Collider.Clone();
            collider.Position += attachedSolid.Position - baseSolid.Position;
            return collider;
        }
    }


    private static bool MoveVCheck(Solid baseSolid, float amount) {

        List<Solid> attachedSolids = baseSolid.staticMovers.Where(sm => sm.Entity is Solid { Collidable: true }).Select(sm => (Solid)sm.Entity).ToList();
        foreach (Solid attachedSolid in attachedSolids) {
            attachedSolid.Collidable = false;
        }
        Collider orig_collider = baseSolid.Collider;
        List<Collider> colliders = attachedSolids.Select(solid => CloneWithShift(baseSolid, solid)).ToList();
        colliders.Add(orig_collider);
        baseSolid.Collider = new ColliderList(colliders.ToArray());

        bool b = MoveVCheckCore(baseSolid, amount);

        baseSolid.Collider = orig_collider;


        foreach (Solid solid in attachedSolids) {
            solid.Collidable = true;
        }

        return b;

        Collider CloneWithShift(Solid baseSolid, Solid attachedSolid) {
            Collider collider = attachedSolid.Collider.Clone();
            collider.Position += attachedSolid.Position - baseSolid.Position;
            return collider;
        }
    }

    private static bool MoveHCheckCore(Solid solid, float amount) {
        if (Engine.Scene is not Level level) {
            return true;
        }

        if (solid.MoveHCollideSolidsAndBounds(level, amount, thruDashBlocks: true)) {
            if (amount < 0f && solid.Left <= (float)level.Bounds.Left) {
                return true;
            }
            if (amount > 0f && solid.Right >= (float)level.Bounds.Right) {
                return true;
            }
            for (int i = 1; i <= 4; i++) {
                for (int num = 1; num >= -1; num -= 2) {
                    Vector2 vector = new Vector2(Math.Sign(amount), i * num);
                    if (!solid.CollideCheck<Solid>(solid.Position + vector)) {
                        solid.MoveVExact(i * num);
                        solid.MoveHExact(Math.Sign(amount));
                        return false;
                    }
                }
            }
            return true;
        }
        return false;
    }

    private static bool MoveVCheckCore(Solid solid, float amount) {
        if (Engine.Scene is not Level level) {
            return true;
        }

        if (solid.MoveVCollideSolidsAndBounds(level, amount, thruDashBlocks: true, null, checkBottom: false)) {
            if (amount < 0f && solid.Top <= (float)level.Bounds.Top) {
                return true;
            }
            for (int i = 1; i <= 4; i++) {
                for (int num = 1; num >= -1; num -= 2) {
                    Vector2 vector = new Vector2(i * num, Math.Sign(amount));
                    if (!solid.CollideCheck<Solid>(solid.Position + vector)) {
                        solid.MoveHExact(i * num);
                        solid.MoveVExact(Math.Sign(amount));
                        return false;
                    }
                }
            }
            return true;
        }
        return false;
    }

}

#endif
