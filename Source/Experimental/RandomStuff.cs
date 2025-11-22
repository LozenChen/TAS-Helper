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
using YamlDotNet.Core;

namespace Celeste.Mod.TASHelper.Experimental;
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

        /*
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
        */

        public class LightingRendererKiller : Entity {
            public LightingRendererKiller(Level level) : base(new Vector2(level.Bounds.X, level.Bounds.Y)) {
                this.Collider = new Hitbox(level.Bounds.Width / 2f, level.Bounds.Height / 2f);
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

    [Load]
    private static void Load() {

    }

    [Initialize(int.MaxValue)]
    internal static void Initialize() {
        /*
        ModUtils.GetType("BrokemiaHelper", "BrokemiaHelper.PixelRendered.PixelComponent")?.GetMethodInfo("DebugRender")?.IlHook((cursor, _) => {
            Instruction start = cursor.Next;
            cursor.EmitDelegate(IsSimplifiedGraphics);
            cursor.Emit(OpCodes.Brfalse, start);
            cursor.Emit(OpCodes.Ret);
        });
        
        ModUtils.GetType("BrokemiaHelper", "BrokemiaHelper.PixelRendered.PixelComponent")?.GetMethodInfo("Render")?.IlHook((cursor, _) => {
            Instruction start = cursor.Next;
            cursor.EmitDelegate(IsSimplifiedGraphics);
            cursor.Emit(OpCodes.Brfalse, start);
            cursor.Emit(OpCodes.Ret);
        });
        */

        /*
        ModUtils.GetType("MotionSmoothing", "Celeste.Mod.MotionSmoothing.Smoothing.Targets.UnlockedCameraSmoother")?.GetMethodInfo("GetCameraOffset")?.IlHook((cursor, _) => {
            cursor.EmitDelegate(HandleMotionSmoothing);
            cursor.Emit(OpCodes.Ret);
        });
        */

        Logger.Log(LogLevel.Warn, "TAS Helper", "TAS Helper Random Stuff loaded! Please contact the author to disable these codes.");
        Celeste.Commands.Log("WARNING: TAS Helper Random Stuff loaded! Please contact the author to disable these codes.");

        //typeof(Tracker).GetMethodInfo("AddTypeToTracker", [typeof(Type), typeof(Type), typeof(Type[])]).ILHook(il => {
        //    ILCursor cursor = new ILCursor(il);
        //    cursor.Emit(OpCodes.Ldarg_0);
        //    cursor.Emit(OpCodes.Ldarg_1);
        //    cursor.Emit(OpCodes.Ldarg_2);
        //    cursor.EmitDelegate(ReplaceAdd);
        //    cursor.Emit(OpCodes.Ret);
        //});

        //static void ReplaceAdd(Type type, Type trackedAs = null, params Type[] subtypes) {
        //    Type type2 = ((trackedAs != null && trackedAs.IsAssignableFrom(type)) ? trackedAs : type);
        //    bool flag = (typeof(Entity).IsAssignableFrom(type) ? new bool?(true) : (typeof(Component).IsAssignableFrom(type) ? new bool?(false) : null)) ?? throw new Exception("Type '" + type.Name + "' cannot be Tracked" + ((type2 != type) ? "As" : "") + " because it does not derive from Entity or Component");
        //    bool flag2 = false;
        //    HashSet<Type> knownTypes = (flag ? Tracker.StoredEntityTypes : Tracker.StoredComponentTypes);
        //    Dictionary<Type, List<Type>> tracked = (flag ? Tracker.TrackedEntityTypes : Tracker.TrackedComponentTypes);
        //    if (ReplaceAddSpecific(type, type2, tracked, knownTypes)) {
        //        flag2 = true;
        //    }
        //    foreach (Type type3 in subtypes) {
        //        if (type2.IsAssignableFrom(type3) && ReplaceAddSpecific(type3, type2, tracked, null)) {
        //            flag2 = true;
        //        }
        //    }
        //    if (flag2) {
        //        Tracker.TrackedTypeVersion++;
        //    }
        //}

        //static bool ReplaceAddSpecific(Type type, Type trackedAsType, Dictionary<Type, List<Type>> tracked, HashSet<Type> knownTypes) {
        //    if (knownTypes is not null) {
        //        knownTypes.Add(type);
        //        knownTypes.Add(trackedAsType);
        //    }
        //    if (type.IsAbstract) {
        //        return false;
        //    }
        //    if (!tracked.TryGetValue(type, out var value)) {
        //        value = new List<Type>();
        //        tracked.Add(type, value);
        //    }
        //    int count = value.Count;
        //    value.Add(trackedAsType);
        //    value.AddRange(tracked.TryGetValue(trackedAsType, out var value2) ? value2 : new List<Type>());
        //    List<Type> list2 = (tracked[type] = Enumerable.ToList(Enumerable.Distinct(value)));
        //    List<Type> list3 = list2;
        //    return count != list3.Count;
        //}
    }


    private static bool IsSimplifiedGraphics() => TasSettings.SimplifiedGraphics;


    [Command("test", "tashelper test command")]
    public static void TestCommand() {
        Commands.CmdLoad(141, "StrawberryJam2021/5-Grandmaster/ZZ-HeartSide");
    }
}

#endif