//#define UseRandomStuff
//#define JumpThruPatch
//#define LightingRendererBugReproducer

#if UseRandomStuff

using Monocle;
using Celeste;
using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Celeste.Mod.TASHelper.Entities;
using TAS.Input.Commands;
using TAS;

namespace Celeste.Mod.TASHelper.Gameplay;
internal class RandomStuff {
    // test some stuff, not actually in use

#if JumpThruPatch

    internal static class JumpThruPatch {
        [Load]
        private static void Load() {
            On.Celeste.JumpThru.MoveHExact += JumpThruMoveH_LiftSpeedPatch;
        }

        [Unload]
        private static void Unload() {
            On.Celeste.JumpThru.MoveHExact -= JumpThruMoveH_LiftSpeedPatch;
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

#if LightingRendererBugReproducer

    internal static class LightingRendererBugReproducer {
        [Load]
        private static void Load() {
            On.Celeste.Level.LoadLevel += OnLoadLevel;
        }

        [Unload]
        private static void Unload() {
            On.Celeste.Level.LoadLevel -= OnLoadLevel;
        }

        [Initialize]
        private static void Initialize() {
            typeof(LightingRenderer).GetMethodInfo("StartDrawingPrimitives").HookBefore<LightingRenderer>(r => {
                if (r.indexCount > 100) {
                    int count = 0;
                    for (int i = 0; i < 64; i++) {
                        VertexLight vertexLight = r.lights[i];
                        if (vertexLight == null || !vertexLight.Dirty) {
                            continue;
                        }
                        count++;
                    }
                    Logger.Log(LogLevel.Debug, "TAS Helper", $"--------------- Clear ------------ {r.indexCount} {r.vertexCount} {Engine.Scene.Tracker.Components[typeof(LightOcclude)].Count} {Engine.Scene.Tracker.Components[typeof(EffectCutout)].Count} {Engine.Scene.Tracker.Components[typeof(VertexLight)].Count} {count} \n");
                }
            });
            typeof(LightingRenderer).GetMethodInfo("SetOccluder").HookAfter<LightingRenderer>(r => {
                if (r.indexCount > 100) {
                    Logger.Log(LogLevel.Debug, "TAS Helper", $"\n \n [SetCutout] indexCount = {r.indexCount}; vertexCount = {r.vertexCount}; \n LightOcclude = {Engine.Scene.Tracker.Components[typeof(LightOcclude)].Count}; EffectCutout = {Engine.Scene.Tracker.Components[typeof(EffectCutout)].Count}; VertexLight = {Engine.Scene.Tracker.Components[typeof(VertexLight)].Count} {TAS.Manager.Controller.CurrentFrameInTas}");
                }
            });
            typeof(LightingRenderer).GetMethodInfo("SetCutout").HookAfter<LightingRenderer>(r => {
                if (r.indexCount > 100) {
                    Logger.Log(LogLevel.Debug, "TAS Helper", $"\n \n [SetCutout] indexCount = {r.indexCount}; vertexCount = {r.vertexCount}; \n  LightOcclude = {Engine.Scene.Tracker.Components[typeof(LightOcclude)].Count}; EffectCutout = {Engine.Scene.Tracker.Components[typeof(EffectCutout)].Count}; VertexLight = {Engine.Scene.Tracker.Components[typeof(VertexLight)].Count} {TAS.Manager.Controller.CurrentFrameInTas}");
                }
            });
        }

        public class LightingRendererKiller : Entity {
            public LightingRendererKiller(Level level) : base(new Vector2(level.Bounds.X, level.Bounds.Y)) {
                this.Collider = new Hitbox(level.Bounds.Width, level.Bounds.Height);
                for (int i = 1; i <= 4; i++) {
                    this.Add(new EffectCutout());
                }
            }
        }
        private static void OnLoadLevel(On.Celeste.Level.orig_LoadLevel orig, Level self, Player.IntroTypes playerIntro, bool isFromLoader) {
            orig(self, playerIntro, isFromLoader);
            self.Add(new LightingRendererKiller(self));
        }
    }
#endif

    [Initialize]
    private static void Initialize() {
        Celeste.Commands.Log("WARNING: TAS Helper Random Stuff loaded! Please contact the author to disable these codes.");
    }
}

#endif
