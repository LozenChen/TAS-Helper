
using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework;
using Monocle;
using System.Collections;

namespace Celeste.Mod.TASHelper.Gameplay.AutoWatchEntity;

internal class CutsceneEntityRenderer : AutoWatchTextRenderer {

    public CutsceneEntity cs;
    public CutsceneEntityRenderer(RenderMode mode, bool active = true) : base(mode, active) { }

    public Coroutine coroutine;
    public float waitTimer => coroutine.waitTimer;

    public bool wasWaiting = false;

    public bool waitingForCoroutine = true;

    public static Vector2 offset = Vector2.UnitY * 12f; // make sure this is different to that of player

    private static bool IsCutscene(IEnumerator func) {
        return func is LuaCoroutine || func.GetType().Name.StartsWith("<Cutscene>d__"); // to make sure we don't get other coroutine
    }

    public static bool useFallBack = true;

    public override void Added(Entity entity) {
        base.Added(entity);
        cs = entity as CutsceneEntity;
        text.justify = new Vector2(0.5f, 0f);
        bool found = false;
        foreach (Component c in cs.Components) {
            if (c is not Coroutine cor) {
                continue;
            }
            coroutine = cor;
            waitingForCoroutine = false;
            if (cor.enumerators.FirstOrDefault(IsCutscene) is not null) {
                // great if it matches well
                found = true;
                break;
            }
        }
        if (!found && !waitingForCoroutine) {
            if (useFallBack) {
                found = true;
                // may be not that precise?
                // Celeste.Mod.StrawberryJam2021.Cutscenes.CS_Credits use Lobby/MovieRoutine
            }
            else {
                // if it has coroutine but that does not match (and we don't use fallBack), remove it
                // but if it does not have coroutine (possible for a cutscene entity), then it's okay
                RemoveSelf();
            }
        }
    }

    public override void UpdateImpl() {
        if (waitingForCoroutine) { // CS06_Campfire adds its coroutine when OnBegin is called
            bool found = false;
            foreach (Component c in cs.Components) {
                if (c is not Coroutine cor) {
                    continue;
                }
                coroutine = cor;
                waitingForCoroutine = false;
                if (cor.enumerators.FirstOrDefault(IsCutscene) is not null) {
                    found = true;
                    break;
                }
                // Logger.Log("TAS Helper", string.Join(",", cor.enumerators.Select(call => call.GetType().FullName)));
            }
            if (!found && !waitingForCoroutine) {
                if (useFallBack) {
                    found = true;
                }
                else {
                    waitingForCoroutine = true;
                    Visible = PostActive = hasUpdate = false; // RemoveSelf is dangerous here i guess
                }
            }

            if (!found) {
                return;
            }
        }
        if (playerInstance is not { } player) {
            Visible = false;
            return;
        }
        text.Position = player.BottomCenter + offset;
        text.Clear();
        bool flag = false;
        if (coroutine.Active) {
            if (waitTimer > 0f) {
                text.Append(waitTimer.ToFrame());
                flag = true;
            }
            else if (coroutine.Current.GetType().FullName == "Monocle.Tween+<Wait>d__45" && coroutine.Current.GetFieldValue("<>4__this") is Tween tween) {
                text.Append((tween.TimeLeft.ToFrameData() + 1).ToString());
                flag = true;
            }
            else if (!wasWaiting) {
                text.Append("~");
            }
        }

        if (!flag && wasWaiting) {
            text.Append("0");
        }
        wasWaiting = flag;

        if (text.content != "") {
            text.AppendAtFirst("Cutscene", "Cutscene");
        }

        SetVisible();
    }
}

internal class CutsceneEntityFactory : IRendererFactory {
    public Type GetTargetType() => typeof(CutsceneEntity);

    public bool Inherited() => true;
    public RenderMode Mode() => Config.CutsceneEntity;
    public void AddComponent(Entity entity) {
        entity.Add(new CutsceneEntityRenderer(Mode()));
    }


    [Load]
    private static void Load() {
        On.Celeste.CutsceneEntity.Added += CutsceneEntity_Added;
    }

    [Unload]
    private static void Unload() {
        On.Celeste.CutsceneEntity.Added -= CutsceneEntity_Added;
    }

    private static readonly CutsceneEntityFactory factory = new CutsceneEntityFactory();
    private static void CutsceneEntity_Added(On.Celeste.CutsceneEntity.orig_Added orig, CutsceneEntity self, Scene scene) {
        // CS are usually not present when loading level, e.g. CS06_Campfire
        orig(self, scene);
        if (scene is Level level && self.Components.FirstOrDefault(c => c is AutoWatchRenderer) is null) {
            factory.AddComponent(self);
        }
    }
}




