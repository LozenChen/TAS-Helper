using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework;
using System.Reflection;
using Monocle;
using MonoMod.Cil;
using Mono.Cecil.Cil;

namespace Celeste.Mod.TASHelper.Predictor;
public static class Core {
    public static DummyPlayer dm => DummyPlayer.Instance;
    public static void Predict(int frames) {
        if (Engine.Scene.Tracker.GetEntity<Player>() is not { } player || player.Dead) {
            return;
        }
        InputManager.StoreInputState();
        InputManager.ReadInputs(frames);

        if (dm is null || dm.Scene != Engine.Scene) {
            new DummyPlayer().Added(Engine.Scene);
        }

        CloneMachine.Clone(dm, player);

        for (int i = 0; i < frames; i++) {
            TAS.InputHelper.FeedInputs(InputManager.P_Inputs[i]);
            bool b = false;
            if (InputManager.FreezeTimer > 0f) {
                b = true;
                InputManager.FreezeTimer = Math.Max(InputManager.FreezeTimer - Engine.RawDeltaTime, 0f);
            }
            else {
                dm.D_Update();
            }
            // todo: highlight keyframe
            futures.Add(new FutureData(i + 1, dm));
            if (dm.TransitionOrDead) {
                break;
            }

            if (i < 10) {
                Celeste.Commands.Log($"{i}, {dm.Position}, {dm.StateMachine.State}, {dm.DashDir}, {dm.moveX}, {Input.MoveX.Value}, Freeze({b})");
            }
        }
        Celeste.Commands.Log("================");
        InputManager.RestoreInputState();
    }

    public class FutureData {
        public int index;
        public float x;
        public float y;
        public float width;
        public float height;

        public FutureData(int index, DummyPlayer dm) {
            this.index = index;
            x = dm.Collider.Left + dm.X;
            y = dm.Collider.Top + dm.Y;
            width = dm.Collider.Width;
            height = dm.Collider.Height;
        }
    }

    public static List<FutureData> futures = new List<FutureData>();

    public static float FreezeTimerBeforeUpdate = 0f;

    public static int PlayerStateBeforeUpdate = 0;
    public static void Initialize() {
        typeof(Level).GetMethod("LoadLevel").HookAfter<Level>(level => {
            new DummyPlayer().Added(level);
            level.Add(new PredictorRenderer());
        });

        typeof(Engine).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).IlHook((cursor, _) => {
            cursor.EmitDelegate(() => {
                if (Engine.Scene is Level level && level.Tracker.GetEntity<Player>() is Player player) {
                    PlayerStateBeforeUpdate = player.StateMachine.State;
                }
            });
            while (cursor.TryGotoNext(MoveType.AfterLabel, i => i.OpCode == OpCodes.Ret)) {
                cursor.EmitDelegate(() => {
                    if (Engine.Scene is Level level && !level.Transitioning && FrameStep) {
                        if (FreezeTimerBeforeUpdate > 0f) {
                            futures.Clear();
                        }
                        FreezeTimerBeforeUpdate = Engine.FreezeTimer;
                        Predict(TasHelperSettings.FutureLength);
                    }
                });
                cursor.Index++;
            }
            
        });

        // todo: refresh predict on tas file change

        typeof(Scene).GetMethod("BeforeUpdate").HookAfter(() => futures.Clear());
    }

    public class PredictorRenderer : Entity {

        public static Color ColorFinal = Color.Green * 0.8f;

        public static Color ColorSegment = Color.Gold * 0.5f;

        public static Color ColorNormal = Color.Red * 0.2f;
        public override void DebugRender(Camera camera) {
            foreach (FutureData data in futures) {
                Draw.HollowRect(data.x, data.y, data.width, data.height, ColorSelector(data.index, futures.Count));
            }
        }

        public static Color ColorSelector(int index, int count) {
            if (index == count) {
                return ColorFinal;
            }
            if (index % 5 == 0) {
                return ColorSegment;
            }
            return ColorNormal * (1 - 0.5f * (float)index / (float)count);
        }
    }


}