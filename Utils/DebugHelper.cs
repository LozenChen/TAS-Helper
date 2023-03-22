using Monocle;

namespace Celeste.Mod.TASHelper.Utils;

// only for developing this mod
internal static class DebugHelper {
    public static bool usingDebug = false;

    public static Dictionary<string, int> dict = new();
    public static void Load() {
        On.Monocle.Scene.BeforeUpdate += PatchBeforeUpdate;
        On.Monocle.Scene.Update += PatchUpdate;
        On.Monocle.Scene.AfterUpdate += PatchAfterUpdate;
    }
    public static void Unload() {
        On.Monocle.Scene.BeforeUpdate -= PatchBeforeUpdate;
        On.Monocle.Scene.Update -= PatchUpdate;
        On.Monocle.Scene.AfterUpdate -= PatchAfterUpdate;
    }

    public static float triggerBuffer = 1.0f;
    public static float triggerTimer = 0.0f;
    public static int lineLength = 140;
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
                foreach(Entity entity in self.Entities) {
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
        orig(self);
        if (usingDebug && StartToLog && self is Level) {
            int stringLength = 0;
            string strToLog = "";
            foreach (string str in dict.Keys) {
                string strN = str + "*" + dict[str].ToString() + ",";
                stringLength += strN.Length;
                if (stringLength > lineLength) {
                    strToLog += "\n";
                    stringLength = strN.Length;
                }
                strToLog += strN;
            }
            Celeste.Commands.Log(strToLog);
            StartToLog = false;
            dict.Clear();
        }
    }
    public static bool StartToLog = false;

}