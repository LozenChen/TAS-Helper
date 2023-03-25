using Microsoft.Xna.Framework;
using Monocle;
using System.Collections;

namespace Celeste.Mod.TASHelper.Utils;

public static class DebugHelper {

    // only for developing this mod, so make it readonly
    // and set usingDebug = false when release
    public static readonly bool usingDebug = true;
    public static float PlayerIntPositionX { get => PlayerHelper.player.X; set => PlayerHelper.player.X = value; }
    public static float PlayerIntPositionY { get => PlayerHelper.player.Y; set => PlayerHelper.player.Y = value; }

    public static Dictionary<string, int> dict = new();
    internal static void Load() {
        if (usingDebug) {
            On.Monocle.Scene.BeforeUpdate += PatchBeforeUpdate;
            On.Monocle.Scene.Update += PatchUpdate;
            On.Monocle.Scene.AfterUpdate += PatchAfterUpdate;
            On.Monocle.EntityList.DebugRender += PatchEntityListDebugRender;
        }
    }
    internal static void Unload() {
        if (usingDebug) {
            On.Monocle.Scene.BeforeUpdate -= PatchBeforeUpdate;
            On.Monocle.Scene.Update -= PatchUpdate;
            On.Monocle.Scene.AfterUpdate -= PatchAfterUpdate;
            On.Monocle.EntityList.DebugRender -= PatchEntityListDebugRender;
        }
    }

    public static float triggerBuffer = 1.0f;
    public static float triggerTimer = 0.0f;
    public static int lineLength = 140;

    private static void PatchEntityListDebugRender(On.Monocle.EntityList.orig_DebugRender orig, EntityList self, Camera camera) {
        orig(self, camera);
        if (usingDebug) {
            if (PlayerHelper.player is Player player) {
                Monocle.Draw.Point(player.Position, Color.MediumPurple);
            }
        }
    }

    private static void PatchBeforeUpdate(On.Monocle.Scene.orig_BeforeUpdate orig, Scene self) {
        orig(self);
        if (usingDebug && self is Level) {
            if (triggerTimer > 0f) {
                triggerTimer -= Engine.DeltaTime;
            }
        }
    }

    private static void PatchUpdate(On.Monocle.Scene.orig_Update orig, Scene self) {
        orig(self);
        if (usingDebug && !StartToLog && self is Level) {
            if (PlayerHelper.player is Player Player && Player.Speed.Y < -200f && triggerTimer <= 0f) {
                StartToLog = true;
                triggerTimer = triggerBuffer;
                foreach (Entity entity in self.Entities) {
                    string str = "(" + entity.Depth.ToString() + "," + entity.GetType().ToString() + "," + entity.Visible + ")";
                    if (!dict.Keys.Contains(str)) {
                        dict.Add(str, 1);
                    }
                    else {
                        dict[str]++;
                    }
                }
            }
        }
    }

    private static void PatchAfterUpdate(On.Monocle.Scene.orig_AfterUpdate orig, Scene self) {
        if (usingDebug && StartToLog && self is Level) {
            foreach (string str in dict.Keys) {
                string strN = str + "*" + dict[str].ToString() + ",";
                Logger.Log(strN);
            }
            StartToLog = false;
            dict.Clear();
        }
        orig(self);
    }
    public static bool StartToLog = false;

}


public static class Logger {
    public static int stringLength = 0;
    public static int lineLength = 140;
    public static string stringToLog = "";
    public const string sep = ", ";
    public static void Log(this object? obj, string? after = null, string? before = null) {
        if (before != null || after != null) {
            Log(before);
            Log(obj);
            Log(after);
            return;
        }
        if (obj is null) {
            return;
        }
        if (obj is string str) {
            LogString(str);
            return;
        }
        if (obj.isList()) {
            List<object> list = ((IEnumerable)obj).Cast<object>().ToList();
            Log("List:{");
            foreach (var item in list) {
                Log(item, sep);
            }
            Log("}");
            return;
        }
        if (obj is Rectangle rect) {
            Log("Rectangle:{");
            Log(rect.X, sep, "Left");
            Log(rect.Y, sep, "Top");
            Log(rect.Width, sep, "Width");
            Log(rect.Height, null, "Height");
            Log("}");
            return;
        }
        if (obj is Hitbox hitbox) {
            Log("Hitbox:{");
            Log(hitbox.Left, sep, "Left");
            Log(hitbox.Top, sep, "Top");
            Log(hitbox.Width, sep, "Width");
            Log(hitbox.Height, null, "Height");
            Log("}");
            return;
        }
        if (obj is Circle circle) {
            Log("Circle:{");
            Log(circle.Radius, sep, "Radius");
            Log(circle.Position, null, "Offset");
            Log("}");
            return;
        }
        LogString(obj.ToString());
        return;
    }

    internal static bool isList(this object obj) {
        return obj.GetType().IsGenericType && obj is System.Collections.IEnumerable;
    }

    internal static void Load() {
        if (DebugHelper.usingDebug) {
            On.Monocle.Scene.AfterUpdate += PatchAfterUpdate;
        }
    }
    internal static void Unload() {
        if (DebugHelper.usingDebug) {
            On.Monocle.Scene.AfterUpdate -= PatchAfterUpdate;
        }
    }

    private static void PatchAfterUpdate(On.Monocle.Scene.orig_AfterUpdate orig, Scene self) {
        orig(self);
        if (String.IsNullOrWhiteSpace(stringToLog)) {
            stringToLog = "";
        }
        else {
            Celeste.Commands.Log(stringToLog);
            stringToLog = "";
        }
    }
    public static void LogString(string message) {
        stringLength += message.Length;
        if (stringLength > lineLength) {
            stringToLog += "\n" + message;
            stringLength = message.Length;
        }
        else {
            stringToLog += message;
        }
        if (message.Contains("\n")) {
            stringLength = 0;
            // we assume \n is always at the end...
        }
    }

}