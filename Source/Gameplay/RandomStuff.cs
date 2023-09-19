//#define UseRandomStuff

#if UseRandomStuff

using Monocle;
using Celeste;
using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework;

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
}

#endif