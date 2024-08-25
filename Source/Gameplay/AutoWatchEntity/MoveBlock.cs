
using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework;
using Monocle;
using System.Collections;

namespace Celeste.Mod.TASHelper.Gameplay.AutoWatchEntity;


internal class MoveBlockRenderer : AutoWatchTextRenderer {

    public MoveBlock moveBlock;

    public Coroutine coroutine;

    public IEnumerator controller;

    public int state => controller?.GetFieldValue<int>("<>1__state") ?? -42; // -1 is used by the ienumrator itself, so we use other number
    public float crashTimer => controller?.GetFieldValue<float>("<crashTimer>5__2") ?? -9999;


    public Vector2 lastPos;

    public Vector2 pos;
    public MoveBlockRenderer(RenderMode mode) : base(mode, active: true) { }

    public override void Added(Entity entity) {
        base.Added(entity);
        moveBlock = entity as MoveBlock;
        lastPos = pos = entity.Position;
        if (entity.FindCoroutineComponent("Celeste.MoveBlock+<Controller>d__45", out Tuple<Coroutine, IEnumerator> tuple)) {
            coroutine = tuple.Item1;
            controller = tuple.Item2;
        }
        else {
            coroutine = null;
            controller = null;
        }
    }

    public override void UpdateImpl() {
        text.Position = moveBlock.Center;
        lastPos = pos;
        pos = moveBlock.Position + moveBlock.movementCounter;
        text.Clear();
        if (moveBlock.state == MoveBlock.MovementState.Moving && crashTimer < 0.15f && crashTimer != -9999) {
            text.Append(crashTimer.ToFrame()); // not exactly frame, coz the timer decreases if move block will collide into a wall in this frame. but if you hold the other direction, then the move block has lower speed, so it's possible that the delta position is not enough to make it collide into a wall
        }
        text.Append((pos - lastPos).Positon2ToSignedSpeed(allowZero: false, breakline: true));
        SetVisible();
    }

    public override void ClearHistoryData() {
        lastPos = pos = moveBlock.Position;
    }
}

internal class MoveBlockFactory : IRendererFactory {
    public Type GetTargetType() => typeof(MoveBlock);

    public bool Inherited() => true;
    public RenderMode Mode() => Config.MoveBlock;
    public void AddComponent(Entity entity) {
        entity.Add(new MoveBlockRenderer(Mode()));
    }
}





