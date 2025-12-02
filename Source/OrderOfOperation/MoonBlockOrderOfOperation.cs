using Celeste.Mod.TASHelper.Gameplay.AutoWatchEntity;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.TASHelper.OrderOfOperation;
internal static class MoonBlockOrderOfOperation {

    private static int state = 0;

    private static int loop_index = 0;

    private static int loop_bound = 0;

    internal static Entity trackedEntity = null;

    private static bool initialized = false;
    private static void SendText(string str) {
        OoO_Core.SendText(str);
    }
    public static string GetUID(Entity entity) {
        if (entity.SourceId.ToString() is { } id) {
            return $"{entity.GetType().Name}[{id}]";
        }
        return $"{entity.GetType().Name}";
    }

    private static void SetUp(FloatySpaceBlock block) {
        state = 0;
        loop_index = 1;
        loop_bound = block.Moves.Count;
        trackedEntity = null;
    }

    internal static void NextOperation(FloatySpaceBlock block) {
        if (!block.MasterOfGroup) {
            block.Update();
            ForEachBreakPoints_EntityList.MarkSubMethodPassed();
        }

        if (!initialized) {
            initialized = true;
            SetUp(block);
        }

        switch (state) {
            case 0:
                FloatySpaceBlockUpdate_START(block);
                state = 1;
                return;
            case 1 or 2:
                KeyValuePair<Platform, Vector2> move;
                using (var ienum = block.Moves.GetEnumerator()) {
                    for (int i = 1; i <= loop_index; i++) {
                        ienum.MoveNext();
                    }
                    move = ienum.Current;
                }
                MoveToTarget_SingleHandle(state - 1, move);
                loop_index++;
                if (loop_index > loop_bound) {
                    state++;
                    loop_index = 1;
                }
                return;
            case 3:
                FloatySpaceBlockUpdate_END(block);
                state++;
                return;
            case 4:
                Ends();
                return;
        }
    }

    private static void Ends() {
        trackedEntity = null;
        initialized = false;
        ForEachBreakPoints_EntityList.MarkSubMethodPassed();
    }


    private static void FloatySpaceBlockUpdate_START(FloatySpaceBlock block) {
        trackedEntity = block;
        SendText($"{GetUID(block)}'s update starts");

        block.Components.Update();
        if (block.MasterOfGroup) {
            bool flag = false;
            foreach (FloatySpaceBlock item in block.Group) {
                if (item.HasPlayerRider()) {
                    flag = true;
                    break;
                }
            }
            if (!flag) {
                foreach (JumpThru jumpthru in block.Jumpthrus) {
                    if (jumpthru.HasPlayerRider()) {
                        flag = true;
                        break;
                    }
                }
            }
            if (flag) {
                block.sinkTimer = 0.3f;
            }
            else if (block.sinkTimer > 0f) {
                block.sinkTimer -= Engine.DeltaTime;
            }
            if (block.sinkTimer > 0f) {
                block.yLerp = Calc.Approach(block.yLerp, 1f, 1f * Engine.DeltaTime);
            }
            else {
                block.yLerp = Calc.Approach(block.yLerp, 0f, 1f * Engine.DeltaTime);
            }
            block.sineWave += Engine.DeltaTime;
            block.dashEase = Calc.Approach(block.dashEase, 0f, Engine.DeltaTime * 1.5f);
            MoveToTarget_START(block);
        }

        static void MoveToTarget_START(FloatySpaceBlock block) {
            loc_num = (float)((double)FloatySpaceBlock.JITBarrier(4f) * Math.Sin(block.sineWave));
            loc_vector = Calc.YoYo(Ease.QuadIn(block.dashEase)) * block.dashDirection * 8f;
            loc_yLerp = block.yLerp;
        }
    }

    private static float loc_num;

    private static Vector2 loc_vector;

    private static float loc_yLerp;

    private static void MoveToTarget_SingleHandle(int i, KeyValuePair<Platform, Vector2> move) {
        Platform key = move.Key;
        trackedEntity = key;

        bool flag = false;
        JumpThru jumpThru = key as JumpThru;
        Solid solid = key as Solid;
        string round = i == 0 ? "[round_1]" : "[round_2]";
        string failReason = i == 0 ? "has no rider" : "has rider";
        if ((jumpThru != null && jumpThru.HasRider()) || (solid != null && solid.HasRider())) {
            flag = true;
        }
        if ((i == 0 && flag) || (i == 1 && !flag)) {
            Vector2 value = move.Value;
            double num2 = (double)value.Y + (double)FloatySpaceBlock.JITBarrier(12f) * (double)Ease.SineInOut(loc_yLerp) + (double)loc_num;
            Vector2 orig = key.ExactPosition;
            key.MoveToY((float)(num2 + (double)loc_vector.Y));
            key.MoveToX(value.X + loc_vector.X);
            SendText($"{GetUID(key)} {round} moves by {(key.ExactPosition - orig).FloatVector2ToString()}");
        }
        else {
            SendText($"{GetUID(key)} {round} doesn't pass the check because it {failReason}");
        }
    }

    private static void FloatySpaceBlockUpdate_END(FloatySpaceBlock block) {
        trackedEntity = block;
        SendText($"{GetUID(block)}'s update ends");

        block.LiftSpeed = Vector2.Zero;
    }
}
