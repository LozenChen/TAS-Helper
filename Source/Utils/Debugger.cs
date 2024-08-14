//#define usingDebug
//#define usingLogger
/*
using System.Text;
using System.Collections;
using Monocle;
using Microsoft.Xna.Framework;
*/
namespace Celeste.Mod.TASHelper.Utils;

#pragma warning disable CS8602

#if usingDebug

public static class DebugHelper {

    // only for developing this mod, so make it readonly
    // and set usingDebug = false when release

    public static readonly bool usingDebug = false;

    public static bool usingEntityLog = false;

    public static bool UsingEntityLog => usingDebug && usingEntityLog;

    public static bool LogFPS = false;

    public static Dictionary<string, int> dict = new();


    [Load]
    internal static void Load() {
        On.Monocle.Scene.BeforeUpdate += PatchBeforeUpdate;
        On.Monocle.Scene.Update += PatchUpdate;
        On.Monocle.Scene.AfterUpdate += PatchAfterUpdate;
        On.Monocle.EntityList.DebugRender += PatchEntityListDebugRender;
    }

    [Unload]
    internal static void Unload() {
        On.Monocle.Scene.BeforeUpdate -= PatchBeforeUpdate;
        On.Monocle.Scene.Update -= PatchUpdate;
        On.Monocle.Scene.AfterUpdate -= PatchAfterUpdate;
        On.Monocle.EntityList.DebugRender -= PatchEntityListDebugRender;
    }


    public static float triggerBuffer = 1.0f;
    public static float triggerTimer = 0.0f;
    public static int lineLength = 140;

    private static void PatchEntityListDebugRender(On.Monocle.EntityList.orig_DebugRender orig, EntityList self, Camera camera) {
        orig(self, camera);
        if (usingDebug) {
            if (player is not null) {
                Monocle.Draw.Point(player.Position, Color.MediumPurple);
            }
        }
    }

    private static void PatchBeforeUpdate(On.Monocle.Scene.orig_BeforeUpdate orig, Scene self) {
        orig(self);
        if (UsingEntityLog && self is Level) {
            if (triggerTimer > 0f) {
                triggerTimer -= Engine.DeltaTime;
            }
        }
    }

    private static void PatchUpdate(On.Monocle.Scene.orig_Update orig, Scene self) {
        orig(self);
        if (UsingEntityLog && !StartToLog && self is Level) {
            if (player is not null && player.Speed.Y < -200f && triggerTimer <= 0f) {
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
        if (usingDebug && LogFPS) {
            if (Engine.FPS < 60) {
                Celeste.Commands.Log(self.TimeActive.ToString() + "," + Engine.FPS.ToString());
            }
        }
    }

    private static void PatchAfterUpdate(On.Monocle.Scene.orig_AfterUpdate orig, Scene self) {
        if (UsingEntityLog && StartToLog && self is Level) {
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
#endif

#if usingLogger
public static class Logger {
    public static int stringLength = 0;
    public static int lineLength = 140;
    public static StringBuilder StringBuilder = new StringBuilder();
    public const string sep = ", ";

    public static void Log(this object? obj, string? after = null, string? before = null, bool onlyDebug = true) {
        if (onlyDebug) {
            return;
        }
        orig_Log(obj, after, before);
    }

    public static void orig_Log(this object? obj, string? after = null, string? before = null) {
        if (before != null || after != null) {
            orig_Log(before);
            orig_Log(obj);
            orig_Log(after);
            return;
        }
        if (obj is null) {
            return;
        }
        if (obj is string str) {
            LogString(str);
            return;
        }
        if (obj is string[] strings) {
            orig_Log("Array:{");
            foreach (string str2 in strings) {
                orig_Log(str2, sep);
            }
            orig_Log("}");
            return;
        }
        if (obj.isList()) {
            List<object> list = ((IEnumerable)obj).Cast<object>().ToList();
            orig_Log("List:{");
            foreach (var item in list) {
                orig_Log(item, sep);
            }
            orig_Log("}");
            return;
        }
        if (obj is Rectangle rect) {
            orig_Log("Rectangle:{");
            orig_Log(rect.X, sep, "Left");
            orig_Log(rect.Y, sep, "Top");
            orig_Log(rect.Width, sep, "Width");
            orig_Log(rect.Height, null, "Height");
            orig_Log("}");
            return;
        }
        if (obj is Hitbox hitbox) {
            orig_Log("Hitbox:{");
            orig_Log(hitbox.Left, sep, "Left");
            orig_Log(hitbox.Top, sep, "Top");
            orig_Log(hitbox.Width, sep, "Width");
            orig_Log(hitbox.Height, null, "Height");
            orig_Log("}");
            return;
        }
        if (obj is Circle circle) {
            orig_Log("Circle:{");
            orig_Log(circle.Radius, sep, "Radius");
            orig_Log(circle.Position, null, "Offset");
            orig_Log("}");
            return;
        }
        LogString(obj.ToString());
        return;
    }

    private static bool isList(this object obj) {
        return obj.GetType().IsGenericType && obj is System.Collections.IEnumerable;
    }

    [Load]
    internal static void Load() {
        On.Monocle.Scene.AfterUpdate += PatchAfterUpdate;
    }

    [Unload]
    internal static void Unload() {
        On.Monocle.Scene.AfterUpdate -= PatchAfterUpdate;
    }

    private static void PatchAfterUpdate(On.Monocle.Scene.orig_AfterUpdate orig, Scene self) {
        orig(self);
        if (StringBuilder.Length > 0) {
            Mod.Logger.Log(LogLevel.Debug, "TAS Helper", StringBuilder.ToString());
            StringBuilder.Clear();
        }
    }


    public static void LogString(string message) {
        stringLength += message.Length;
        if (stringLength > lineLength) {
            StringBuilder.Append("\n" + message);
            stringLength = message.Length;
        }
        else {
            StringBuilder.Append(message);
        }
        if (message.Contains("\n")) {
            stringLength = 0;
            // we assume \n is always at the end...
        }
    }

}
#endif