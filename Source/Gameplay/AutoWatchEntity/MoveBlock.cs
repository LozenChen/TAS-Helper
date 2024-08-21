
using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework;
using Monocle;
using System.Collections;

namespace Celeste.Mod.TASHelper.Gameplay.AutoWatchEntity;


internal class MoveBlockRenderer : AutoWatchTextRenderer {

    public MoveBlock moveBlock;

    public Coroutine coroutine;

    public IEnumerator controller;

    public int state => controller.GetFieldValue<int>("<>1__state");
    public float crashTimer => controller.GetFieldValue<float>("<crashTimer>5__2");


    public Vector2 lastPos;

    public Vector2 pos;
    public MoveBlockRenderer(RenderMode mode, bool active = true) : base(mode, active) { }

    public override void Added(Entity entity) {
        base.Added(entity);
        moveBlock = entity as MoveBlock;
        lastPos = pos = entity.Position;
        if (entity.FindCoroutineComponent("Celeste.MoveBlock+<Controller>d__45", out Tuple<Coroutine, IEnumerator> tuple)) {
            coroutine = tuple.Item1;
            controller = tuple.Item2;
        }
        else {
            RemoveSelf();
        }
    }

    public override void UpdateImpl() {
        text.Position = moveBlock.Center;
        lastPos = pos;
        pos = moveBlock.Position + moveBlock.movementCounter;
        text.Clear();
        if (moveBlock.state == MoveBlock.MovementState.Moving && crashTimer < 0.15f) {
            text.Append(crashTimer.ToFrame()); // not exactly frame, coz the timer decreases if move block will collide into a wall in this frame. but if you hold the other direction, then the move block has lower speed, so it's possible that the delta position is not enough to make it collide into a wall
        }
        text.Append((pos - lastPos).Positon2ToSignedSpeed());
        SetVisible();
    }

    public override void ClearHistoryData() {
        lastPos = pos = moveBlock.Position;
    }
}

internal class MoveBlockFactory : IRendererFactory {
    public Type GetTargetType() => typeof(MoveBlock);

    public bool Inherited() => false;
    public RenderMode Mode() => Config.MoveBlock;
    public void AddComponent(Entity entity) {
        entity.Add(new MoveBlockRenderer(Mode()));
    }
}





