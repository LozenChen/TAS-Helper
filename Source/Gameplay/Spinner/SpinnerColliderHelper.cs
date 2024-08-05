using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.TASHelper.Gameplay.Spinner;

public static class SpinnerColliderHelper {

    public class SpinnerColliderValue {
        public MTexture Outline;
        public MTexture Inside;
        public MTexture Outline_Dashed1;
        public MTexture Outline_Dashed2;
        public SpinnerColliderValue(MTexture Outline, MTexture Inside, MTexture Dashed1, MTexture Dashed2) {
            this.Outline = Outline;
            this.Inside = Inside;
            this.Outline_Dashed1 = Dashed1;
            this.Outline_Dashed2 = Dashed2;
        }

        public SpinnerColliderValue(string name) {
            this.Outline = GFX.Game[$"TASHelper/SpinnerCollider/{name}_outline"];
            this.Inside = GFX.Game[$"TASHelper/SpinnerCollider/{name}_inside"];
            this.Outline_Dashed1 = GFX.Game[$"TASHelper/SpinnerCollider/{name}_outlineDashed1"];
            this.Outline_Dashed2 = GFX.Game[$"TASHelper/SpinnerCollider/{name}_outlineDashed2"];
        }
    }

    public static string SpinnerColliderKey(string[] hitboxString, float scale) {
        return string.Join("|", hitboxString) + "//" + scale.ToString();
    }

    private static Dictionary<string, SpinnerColliderValue> SpinnerColliderTextures = new();

    private static Dictionary<ColliderList, SpinnerColliderValue> ComparingDict = new(new ColliderComparer());

    public static SpinnerColliderValue Vanilla;

    public static void LogKeys() {
        foreach (string key in SpinnerColliderTextures.Keys) {
            Celeste.Commands.Log(key);
        }
    }

    [Initialize]
    public static void Initialize() {
        // learn from https://github.com/EverestAPI/Resources/wiki/Adding-Sprites#using-a-spritebank-file

        Add("C:6;0,0|R:16,4;-8,-3", 1f, Vanilla = new SpinnerColliderValue("vanilla"));
        Add("C:6;0,0", 1f, new SpinnerColliderValue("C600"));
        Add("C:6;0,0|R:16,4;-8,-1", 1f, new SpinnerColliderValue("reverted"));
        Add("C:8;0,0", 1f, new SpinnerColliderValue("C800"));
        Add("R:16,16;-8,-8", 1f, new SpinnerColliderValue("S16"));

        void Add(string hitboxS, float scale, SpinnerColliderValue value) {
            string[] hitboxString = hitboxS.Split('|');
            ComparingDict.Add(ParseHitboxType(hitboxString, scale), value);
            SpinnerColliderTextures.Add(SpinnerColliderKey(hitboxString, scale), value);
        }
    }
#pragma warning disable CS8625
    public static bool TryGetValue(string[] hitboxString, float scale, out SpinnerColliderValue value) {
        if (SpinnerColliderTextures.TryGetValue(SpinnerColliderKey(hitboxString, scale), out SpinnerColliderValue v1)) {
            value = v1;
            return true;
        }
        ColliderList list = ParseHitboxType(hitboxString, scale);
        if (list is null) {
            value = null;
            return false;
        }
        if (ComparingDict.TryGetValue(list, out SpinnerColliderValue v2)) {
            SpinnerColliderTextures.Add(SpinnerColliderKey(hitboxString, scale), v2);
            value = v2;
            return true;
        }
        if (TryAutoGenerateTexture(list, out SpinnerColliderValue v3)) {
            SpinnerColliderTextures.Add(SpinnerColliderKey(hitboxString, scale), v3);
            ComparingDict.Add(list, v3);
            value = v3;
            return true;
        }

        value = null;
        return false;
    }

    public static bool TryAutoGenerateTexture(ColliderList list, out SpinnerColliderValue value) {
        // to be implemented
        value = null;
        return false;
    }
#pragma warning restore CS8625
    public static ColliderList ParseHitboxType(string[] S, float scale) {
#pragma warning disable CS8603
        if (S.Length == 0 || (S.Length == 1 && string.IsNullOrWhiteSpace(S[0]))) {
            return null;
        }
#pragma warning restore CS8603
        List<Collider> colliders = new List<Collider>();
        /*At this point, string[] S is a string where each string should be formatted like this:
         * SMaster = A1|A2|A3|A4...|An, S[k] = Ak
         * where Ak => T:U:V
         * where:
         *	: ; = Separators
         *	T = Type: C for circle, R for Rect.
         *	U = AudioParam: for C: r for radius, for R: <w,h>
         *	V = Position offset from Center.
         *	using * before a number as an ignore scale definer.
         *	using a p @ before a number n means (p + n)

         */
        foreach (string s in S) {
            string[] k = s.Split(':', ';'); //Splits Ak into T (k[0]), U (k[1]), and V (k[2])
                                            //We assume that people are going to use this correctly, for now.
            if (k[0][0] == 'C') {
                if (k.Length == 2) { colliders.Add(ParseCircle(scale, k[1])); } else { colliders.Add(ParseCircle(scale, k[1], k[2])); }
            }
            else if (k[0][0] == 'R') {
                if (k.Length == 2) { colliders.Add(ParseRectangle(scale, k[1])); } else { colliders.Add(ParseRectangle(scale, k[1], k[2])); }
            }
        }
        return new ColliderList(colliders.ToArray());

    }

    private static Collider ParseCircle(float scale, string rad, string off = "0,0") {
        int radius;
        int[] offset = new int[2];
        radius = ParseInt(rad, scale);
        string[] offs = off.Split(',');
        for (int i = 0; i < 2; i++) {
            offset[i] = ParseInt(offs[i], scale);
        }
        return new Circle(radius, offset[0], offset[1]);
    }

    private static Collider ParseRectangle(float scale, string Wh) {
        int[] wh = new int[2];
        int[] offset = new int[2];
        string[] a = Wh.Split(',');
        wh[0] = ParseInt(a[0], scale);
        wh[1] = ParseInt(a[1], scale);
        offset[0] = 0 - Math.Abs((int)Math.Round(wh[0] / 2f));
        offset[1] = Math.Min(-3, 0 - Math.Abs((int)Math.Round(wh[1] / 2f)));
        return new Hitbox(wh[0], wh[1], offset[0], offset[1]);
    }

    private static Collider ParseRectangle(float scale, string Wh, string off) {
        int[] wh = new int[2];
        int[] offset = new int[2];
        string[] a = Wh.Split(',');
        string[] b = off.Split(',');
        for (int i = 0; i < 2; i++) {
            wh[i] = ParseInt(a[i], scale);
            offset[i] = ParseInt(b[i], scale);
        }
        return new Hitbox(wh[0], wh[1], offset[0], offset[1]);
    }

    private static int ParseInt(string k, float scale) {
        if (string.IsNullOrEmpty(k)) {
            throw new Exception("Integer was empty.");
        }
        if (k.Contains("@")) {
            string[] q = k.Split('@');
            int p = 0;
            for (int s = 0; s < q.Length; s++) { p += ParseInt(q[s], scale); }
            return p;
        }
        if (k[0] == '*') {
            return int.Parse(k.Substring(1));
        }
        else {
            return (int)Math.Round(int.Parse(k) * (double)scale);
        }
    }

    public class ColliderComparer : IEqualityComparer<ColliderList> {
        public bool Equals(ColliderList x, ColliderList y) {
            Collider[] list1 = x.colliders;
            Collider[] list2 = y.colliders;
            if (list1.Length != list2.Length) {
                return false;
            }
            for (int i = 0; i < list1.Length; i++) {
                // viv helper does not use nested collider list
                if (list1[i] is Hitbox h1 && list2[i] is Hitbox h2) {
                    if (!Equals(h1, h2)) {
                        return false;
                    }
                }
                else if (list1[i] is Circle c1 && list2[i] is Circle c2) {
                    if (!Equals(c1, c2)) {
                        return false;
                    }
                }
                else {
                    return false;
                }
            }
            return true;
        }

        public bool Equals(Hitbox h1, Hitbox h2) {
            return h1.width == h2.width && h1.height == h2.height && h1.Position == h2.Position;
        }
        public bool Equals(Circle c1, Circle c2) {
            return c1.Radius == c2.Radius && c1.Position == c2.Position;
        }

        public int GetHashCode(ColliderList obj) {
            if (obj is null) {
                return 0;
            }

            unchecked {
                int hash = 17;
                foreach (Collider item in obj.colliders) {
                    hash = hash * -1521134295 + GetSimpleHashCode(item);
                }

                return hash;
            }
        }

        public int GetSimpleHashCode(Collider collider) {
            if (collider is Hitbox h) {
                return new Tuple<float, float, Vector2>(h.Width, h.Height, h.Position).GetHashCode();
            }
            else if (collider is Circle c) {
                return new Tuple<float, Vector2>(c.Radius, c.Position).GetHashCode();
            }
            else {
                return collider.GetType().GetHashCode();
            }
        }

    }
}
