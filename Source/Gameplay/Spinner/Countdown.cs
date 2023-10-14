using Celeste.Mod.TASHelper.Entities;
using Celeste.Mod.TASHelper.Module.Menu;
using Microsoft.Xna.Framework;
using Monocle;
using TAS;

namespace Celeste.Mod.TASHelper.Gameplay.Spinner;

internal static class Countdown {

    public static bool LastNotCountdownBoost = !NotCountdownBoost;
    public static bool NotCountdownBoost => !TasHelperSettings.CountdownBoost || FrameStep || (Engine.Scene.Paused && !Manager.Running);

    public static void Draw(Entity self, SpinnerRenderHelper.SpinnerColorIndex index, bool collidable) {
        if (CountdownRenderer.Cached) {
            return;
        }
        if (TasHelperSettings.UsingCountDown && NotCountdownBoost) {
#pragma warning disable CS8629
            float offset = SpinnerCalculateHelper.GetOffset(self).Value;
#pragma warning restore CS8629
            Vector2 CountdownPos;
            if (self.isLightning()) {
                CountdownPos = self.Center + new Vector2(-1f, -2f);
            }
            else {
                CountdownPos = self.Position + (TasHelperSettings.UsingLoadRange ? new Vector2(-1f, 3f) : new Vector2(-1f, -2f));
            }
            SpinnerRenderHelper.DrawCountdown(CountdownPos, SpinnerCalculateHelper.PredictCountdown(offset, self.isDust()), index, collidable);
        }
    }
}


internal class CountdownRenderer : THRenderer {
    public static CountdownRenderer Instance;

    public static Dictionary<int, List<Vector2>> ID2Positions = new Dictionary<int, List<Vector2>>();

    private static MTexture[] numbers;

    public static bool Cached = false;

    public CountdownRenderer() {
        Instance = this;
        ID2Positions = new Dictionary<int, List<Vector2>>();
    }

    [Load]
    public static void Load() {
        On.Celeste.Level.LoadLevel += OnLoadLevel;
        On.Monocle.EntityList.DebugRender += NonHiresRender;
        On.Monocle.Scene.BeforeUpdate += OnSceneBeforeUpdate;
    }

    [Unload]
    public static void Unload() {
        On.Celeste.Level.LoadLevel -= OnLoadLevel;
        On.Monocle.EntityList.DebugRender -= NonHiresRender;
        On.Monocle.Scene.BeforeUpdate -= OnSceneBeforeUpdate;
    }

    [Initialize]
    public static void Initialize() {
        // copied from ExtendedVariants.Entities.DashCountIndicator
        MTexture source = GFX.Game["pico8/font"];
        numbers = new MTexture[10];
        int index = 0;
        for (int i = 104; index < 4; i += 4) {
            numbers[index++] = source.GetSubtexture(i, 0, 3, 5);
        }
        for (int i = 0; index < 10; i += 4) {
            numbers[index++] = source.GetSubtexture(i, 6, 3, 5);
        }
    }


    private static void OnLoadLevel(On.Celeste.Level.orig_LoadLevel orig, Level level, Player.IntroTypes playerIntro, bool isFromLoader = false) {
        orig(level, playerIntro, isFromLoader);
        if (Instance is null || !HiresLevelRenderer.Contains(Instance)) {
            HiresLevelRenderer.Add(new CountdownRenderer());
        }
    }

    public static void Add(int ID, Vector2 Position) {
        if (!ID2Positions.ContainsKey(ID)) {
            ID2Positions.Add(ID, new List<Vector2>());
        }
        ID2Positions[ID].Add(Position);
    }
    public override void Render() {
        if (!TasHelperSettings.UsingHiresFont) {
            return;
        }

        Cached = true;

        if (TASHelperMenu.mainItem?.Container is { } container && container.Visible) {
            // it's a bit too laggy
            return;
        }

        Vector2 scale = new Vector2(TasHelperSettings.HiresFontSize / 10f);
        float stroke = TasHelperSettings.HiresFontStroke * 0.4f;
        foreach (int ID_inDict in ID2Positions.Keys) {
            string str;
            int id = ID_inDict;
            bool uncollidable = id > 120;
            if (uncollidable) {
                id -= SpinnerRenderHelper.ID_uncollidable_offset;
            }
            if (id >= 0 && id < 100) {
                str = id.ToString();
            }
            else if (id == SpinnerRenderHelper.ID_infinity) {
                str = "oo";
            }
            else if (id == SpinnerRenderHelper.ID_nocycle) {
                str = "0";
            }
            else {
                throw new Exception($"[Error] TASHelper: Unexpected ID ({ID_inDict}) in CountdownRenderer!");
            }
            Color colorInside = TasHelperSettings.DarkenWhenUncollidable && uncollidable ? Color.Gray : Color.White;
            foreach (Vector2 Position in ID2Positions[ID_inDict]) {
                Message.RenderMessage(str, Position, new Vector2(0.5f, 0.2f), scale, stroke, colorInside, Color.Black);
            }
        }
    }

    private static void NonHiresRender(On.Monocle.EntityList.orig_DebugRender orig, EntityList self, Camera camera) {
        orig(self, camera);

        if (TasHelperSettings.UsingHiresFont) {
            return;
        }

        foreach (int ID_inDict in ID2Positions.Keys) {
            int index = ID_inDict;
            bool uncollidable = index > 120;
            if (uncollidable) {
                index -= SpinnerRenderHelper.ID_uncollidable_offset;
            }
            Color color = TasHelperSettings.DarkenWhenUncollidable && uncollidable ? Color.Gray : Color.White;
            foreach (Vector2 Position in ID2Positions[ID_inDict]) {
                Vector2 pos = Position / 6f - new Vector2(1.5f, -0.5f);
                if (index == SpinnerRenderHelper.ID_nocycle) {
                    numbers[0].DrawOutline(pos, Vector2.Zero, color);
                    continue;
                }
                if (index == SpinnerRenderHelper.ID_infinity) {
                    numbers[9].DrawOutline(pos, Vector2.Zero, color);
                    continue;
                }
                if (index > 9) {
                    numbers[index / 10].DrawOutline(pos + new Vector2(-4, 0), Vector2.Zero, color);
                    index %= 10;
                }
                numbers[index].DrawOutline(pos, Vector2.Zero, color);
            }
        }

        Cached = true;
    }

    public override void AfterRender() {
        if (Countdown.LastNotCountdownBoost != Countdown.NotCountdownBoost) {
            ClearCache();
            Countdown.LastNotCountdownBoost = Countdown.NotCountdownBoost;
        }
    }

    public static void ClearCache() {
        foreach (int ID in ID2Positions.Keys) {
            ID2Positions[ID].Clear();
        }
        Cached = false;
    }

    private static void OnSceneBeforeUpdate(On.Monocle.Scene.orig_BeforeUpdate orig, Scene self) {
        orig(self);
        ClearCache();
    }
}
