using Celeste.Mod.TASHelper.Entities;
using Celeste.Mod.TASHelper.Module.Menu;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.TASHelper.Gameplay.Spinner.Render;


internal class CountdownRenderer : THRenderer {
    public static CountdownRenderer Instance;

    public static Dictionary<int, List<Vector2>> HiresID2Positions = [];

    public static Dictionary<int, List<Vector2>> NonHiresID2Positions = [];

    private static MTexture[] numbers;

    public static bool Cached { get => Countdown.Cached; set => Countdown.Cached = value; }

    public CountdownRenderer() {
        Instance = this;
        HiresID2Positions = new Dictionary<int, List<Vector2>>();
        NonHiresID2Positions = new Dictionary<int, List<Vector2>>();
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

    [LoadLevel]
    private static void OnLoadLevel() {
        if (Instance is null || !HiresLevelRenderer.Contains(Instance)) {
            HiresLevelRenderer.Add(new CountdownRenderer());
        }
    }

    private static readonly Dictionary<Vector2, int> overlapDetector = [];

    private static readonly Dictionary<Vector2, List<int>> overlapResolver = [];

    // Reused sorted key lists: the dictionary keys change every frame, but sorting into the same list avoids
    // the per-frame Keys.ToList().Sort() allocations (and the per-ID Where(InBound).ToList() below).
    private static readonly List<int> sortedHiresIDs = new();
    private static readonly List<int> sortedNonHiresIDs = new();
    private static bool sortedHiresDirty = true;
    private static bool sortedNonHiresDirty = true;

    private static Vector2 unitOffset = Vector2.One * 2f;
    public static void Add(int ID, Vector2 Position) {
        sortedHiresDirty = true;
        sortedNonHiresDirty = true;
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

    private static readonly Comparer<int> reverseComparer = Comparer<int>.Create((x, y) => -Comparer<int>.Default.Compare(x, y));
    public override void Render() {
        if (!TasHelperSettings.UsingHiresFont) {
            return;
        }

        bool overlapResolved = Cached;
        Cached = true;

        if (TASHelperMenu.mainItem?.Container is { } container && container.Visible) {
            // it's a bit too laggy
            return;
        }

        if (!overlapResolved) {
            foreach (Vector2 Position in overlapResolver.Keys) {
                List<int> distinct = overlapResolver[Position].Distinct().ToList();
                distinct.Sort();
                for (int i = 0; i < distinct.Count; i++) {
                    HiresID2Positions.SafeAdd(distinct[i], (Position + unitOffset * i + new Vector2(1.5f, -0.5f)) * 6f);
                }
            }
        }

        Vector2 scale = new Vector2(TasHelperSettings.HiresFontSize / 10f);
        float stroke = TasHelperSettings.HiresFontStroke * 0.4f;

        if (sortedHiresDirty) {
            sortedHiresIDs.Clear();
            foreach (int id in HiresID2Positions.Keys) {
                sortedHiresIDs.Add(id);
            }
            sortedHiresIDs.Sort(reverseComparer);
            sortedHiresDirty = false;
        }

        foreach (int ID_inDict in sortedHiresIDs) { // make 0 render at top
            List<Vector2> positions = HiresID2Positions[ID_inDict];
            int id = ID_inDict;
            bool uncollidable = id > 120;
            if (uncollidable) {
                id -= SpinnerRenderHelper.ID_uncollidable_offset;
            }
            string str = id switch {
                >= 0 and < 100 => id.ToString(),
                SpinnerRenderHelper.ID_infinity => Infinity_String,
                SpinnerRenderHelper.ID_nocycle => "0",
                _ => throw new Exception($"[Error] TASHelper: Unexpected ID ({ID_inDict}) in CountdownRenderer!")
            };
            Color colorInside = TasHelperSettings.DarkenWhenUncollidable && uncollidable ? Color.Gray : Color.White;
            foreach (Vector2 Position in positions) {
                if (!HiresLevelRenderer.InBound(Position)) {
                    continue;
                }
                Message.RenderMessage(str, Position, new Vector2(0.5f, 0.2f), scale, stroke, colorInside, Color.Black);
            }
        }
    }

    private const string Infinity_String = "\u221e";
    // https://github.com/EverestAPI/Resources/wiki/Adding-Custom-Dialogue#adding-characters-to-the-english-font-with-the-font-generator-website
    // the "oo" infinity symbol is added to Renogare64 font ("English dialog") by us

    [AddDebugRender]
    private static void NonHiresRender() {
        if (TasHelperSettings.UsingHiresFont) {
            return;
        }

        Cached = true;

        if (sortedNonHiresDirty) {
            sortedNonHiresIDs.Clear();
            foreach (int id in NonHiresID2Positions.Keys) {
                sortedNonHiresIDs.Add(id);
            }
            sortedNonHiresIDs.Sort(reverseComparer);
            sortedNonHiresDirty = false;
        }

        foreach (int ID_inDict in sortedNonHiresIDs) {
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
        sortedHiresDirty = true;
        sortedNonHiresDirty = true;
        Cached = false;
    }
}

internal static class DictionaryExtension {
    internal static void SafeAdd(this Dictionary<int, List<Vector2>> dict, int id, Vector2 vec) {
        if (dict.TryGetValue(id, out List<Vector2> list) && list.IsNotNullOrEmpty()) {
            list.Add(vec);
        }
        else {
            dict[id] = new List<Vector2> { vec };
        }
    }
}
