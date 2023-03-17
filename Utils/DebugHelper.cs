using Monocle;

namespace Celeste.Mod.TASHelper.Utils;

// only for developing this mod
internal static class DebugHelper {
    public static bool usingDebug = false;

    public static void Load() {
        On.Monocle.Scene.BeforeUpdate += PatchBeforeUpdate;
        On.Monocle.Scene.Update += PatchUpdate;
        On.Monocle.Entity.Update += PatchEntityUpdate;
        On.Monocle.Scene.AfterUpdate += PatchAfterUpdate;
    }
    public static void Unload() {
        On.Monocle.Scene.BeforeUpdate -= PatchBeforeUpdate;
        On.Monocle.Scene.Update -= PatchUpdate;
        On.Monocle.Entity.Update -= PatchEntityUpdate;
        On.Monocle.Scene.AfterUpdate -= PatchAfterUpdate;
    }

    public static float triggerBuffer = 1.0f;
    public static float triggerTimer = 0.0f;
    private static void PatchBeforeUpdate(On.Monocle.Scene.orig_BeforeUpdate orig, Scene self) {
        orig(self);
        if (usingDebug && self is Level) {
            if (triggered) {
                StartToLog = true;
            }
            if (triggerTimer > 0f) {
                triggerTimer -= Engine.DeltaTime;
            }
        }
    }

    private static void PatchUpdate(On.Monocle.Scene.orig_Update orig, Scene self) {
        orig(self);
        if (usingDebug && self is Level) {
            if (PlayerHelper.player is Player Player && Player.Speed.Y < -200f) {
                if (triggerTimer <= 0f && !StartToLog) {
                    triggered = true;
                    triggerTimer = triggerBuffer;
                }
            }
        }
    }

    private static void PatchEntityUpdate(On.Monocle.Entity.orig_Update orig, Entity self) {
        orig(self);
        if (usingDebug && StartToLog) {
            string str = "(" + self.GetType().ToString() + "," + self.Depth.ToString() + "),";
            if (str != lastLog) {
                stringToLog += str;
                lastLog = str;
                stringCount++;
                if (stringCount >= 3) {
                    stringToLog += "\n";
                    stringCount -= 3;
                }
            }
        }
    }

    private static void PatchAfterUpdate(On.Monocle.Scene.orig_AfterUpdate orig, Scene self) {
        orig(self);
        if (usingDebug && StartToLog && self is Level) {
            Celeste.Commands.Log(stringToLog);
            triggered = false;
            StartToLog = false;
            lastLog = "";
            stringToLog = "";
            stringCount = 0;
        }
    }

    public static bool triggered = false;
    public static bool StartToLog = false;
    public static string lastLog = "";
    public static string stringToLog = "";
    public static int stringCount = 0;

}