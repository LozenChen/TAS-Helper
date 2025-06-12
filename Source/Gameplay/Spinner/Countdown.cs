using Celeste.Mod.TASHelper.Entities;
using Celeste.Mod.TASHelper.Module.Menu;
using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.TASHelper.Gameplay.Spinner;

internal static class Countdown {

    public static bool LastNotCountdownBoost = !NotCountdownBoost;
    public static bool NotCountdownBoost => !TasHelperSettings.CountdownBoost || FrameStep || (Engine.Scene.Paused && !Manager_Running);

    public static void Draw(Entity self, SpinnerRenderHelper.SpinnerColorIndex index, bool collidable) {
        if (CountdownRenderer.Cached) {
            return;
        }
        if (TasHelperSettings.UsingCountDown && NotCountdownBoost) {
#pragma warning disable CS8629
            float offset = Info.OffsetHelper.GetOffset(self).Value;
#pragma warning restore CS8629
            Vector2 CountdownPos;
            if (Info.HazardTypeHelper.IsLightning(self)) {
                CountdownPos = Info.PositionHelper.GetInviewCheckCenter(self) + new Vector2(-1f, -2f);
            }
            else {
                CountdownPos = Info.PositionHelper.GetInviewCheckPosition(self) + (TasHelperSettings.UsingLoadRange ? new Vector2(-1f, 3f) : new Vector2(-1f, -2f));
            }
            SpinnerRenderHelper.DrawCountdown(CountdownPos, Info.TimeActiveHelper.PredictCountdown(offset, Info.HazardTypeHelper.IsDust(self), TasHelperSettings.SpinnerCountdownLoad), index, collidable);
        }
    }
}


internal class CountdownRenderer : THRenderer {
    public static CountdownRenderer Instance;

    public static Dictionary<int, List<Vector2>> HiresID2Positions = new Dictionary<int, List<Vector2>>();

    public static Dictionary<int, List<Vector2>> NonHiresID2Positions = new Dictionary<int, List<Vector2>>();

    private static MTexture[] numbers;

    public static bool Cached = false;

    public CountdownRenderer() {
        Instance = this;
        HiresID2Positions = new Dictionary<int, List<Vector2>>();
        NonHiresID2Positions = new Dictionary<int, List<Vector2>>();
    }

    [Initialize]
    public static void Initialize() {
        EventOnHook._Scene.BeforeUpdate += (_) => ClearCache();

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

    [LoadLevel]
    private static void OnLoadLevel() {
        if (Instance is null || !HiresLevelRenderer.Contains(Instance)) {
            HiresLevelRenderer.Add(new CountdownRenderer());
        }
    }

    private static Dictionary<Vector2, int> overlapDetector = new();

    private static Dictionary<Vector2, List<int>> overlapResolver = new();

    private static Vector2 unitOffset = Vector2.One * 2f;
    public static void Add(int ID, Vector2 Position) {
        if (TasHelperSettings.UsingHiresFont) {
            Vector2 pos = Position;
            if (overlapDetector.TryGetValue(Position, out int id)) {
                if (id == ID) {
                    return;
                }
                if (!overlapResolver.ContainsKey(Position)) {
                    HiresID2Positions[id].Remove((Position + new Vector2(1.5f, -0.5f)) * 6f);
                    overlapResolver.Add(Position, new List<int>() { id });
                }
                overlapResolver[Position].Add(ID);
                return;
            }
            else {
                overlapDetector.Add(Position, ID);
            }

            pos = (pos + new Vector2(1.5f, -0.5f)) * 6f;
            HiresID2Positions.SafeAdd(ID, pos);
        }
        else {
            NonHiresID2Positions.SafeAdd(ID, Position);
        }
    }

    private static Comparer<int> reverseComparer = Comparer<int>.Create((x, y) => -Comparer<int>.Default.Compare(x, y));
    public override void Render() {
        if (!TasHelperSettings.UsingHiresFont) {
            return;
        }

        Cached = true;

        if (TASHelperMenu.mainItem?.Container is { } container && container.Visible) {
            // it's a bit too laggy
            return;
        }

        foreach (Vector2 Position in overlapResolver.Keys) {
            List<int> distinct = overlapResolver[Position].Distinct().ToList();
            distinct.Sort();
            for (int i = 0; i < distinct.Count; i++) {
                HiresID2Positions.SafeAdd(distinct[i], (Position + unitOffset * i + new Vector2(1.5f, -0.5f)) * 6f);
            }
        }

        Vector2 scale = new Vector2(TasHelperSettings.HiresFontSize / 10f);
        float stroke = TasHelperSettings.HiresFontStroke * 0.4f;
        foreach (int ID_inDict in HiresID2Positions.Keys.ToList().Apply(list => list.Sort(reverseComparer))) { // make 0 render at top
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
            foreach (Vector2 Position in HiresID2Positions[ID_inDict]) {
                Message.RenderMessage(str, Position, new Vector2(0.5f, 0.2f), scale, stroke, colorInside, Color.Black);
            }
        }
    }

    [AddDebugRender]
    private static void NonHiresRender() {
        if (TasHelperSettings.UsingHiresFont) {
            return;
        }

        foreach (int ID_inDict in NonHiresID2Positions.Keys.ToList().Apply(list => list.Sort(reverseComparer))) {
            int index = ID_inDict;
            bool uncollidable = index > 120;
            if (uncollidable) {
                index -= SpinnerRenderHelper.ID_uncollidable_offset;
            }
            Color color = TasHelperSettings.DarkenWhenUncollidable && uncollidable ? Color.Gray : Color.White;
            foreach (Vector2 Position in NonHiresID2Positions[ID_inDict]) {
                if (index == SpinnerRenderHelper.ID_nocycle) {
                    numbers[0].DrawOutline(Position, Vector2.Zero, color);
                    continue;
                }
                if (index == SpinnerRenderHelper.ID_infinity) {
                    numbers[9].DrawOutline(Position, Vector2.Zero, color);
                    continue;
                }
                if (index > 9) {
                    numbers[index / 10].DrawOutline(Position + new Vector2(-4, 0), Vector2.Zero, color);
                    index %= 10;
                }
                numbers[index].DrawOutline(Position, Vector2.Zero, color);
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
        foreach (int ID in HiresID2Positions.Keys) {
            HiresID2Positions[ID].Clear();
        }
        foreach (int ID in NonHiresID2Positions.Keys) {
            NonHiresID2Positions[ID].Clear();
        }
        overlapDetector.Clear();
        overlapResolver.Clear();
        Cached = false;
    }
}

internal static class DictionaryExtension {
    internal static void SafeAdd(this Dictionary<int, List<Vector2>> dict, int id, Vector2 vec) {
        if (!dict.ContainsKey(id)) {
            dict.Add(id, new List<Vector2>());
        }
        dict[id].Add(vec);
    }
}
