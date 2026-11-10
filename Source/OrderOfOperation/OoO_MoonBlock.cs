using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.TASHelper.OrderOfOperation;

// Fine-grained stepping for FloatySpaceBlock (月块): its Update is split into
// "update starts" -> one break per platform move of round 1 -> round 2 -> "update ends".
// All of this is done with managed hooks only; blocking preserves the real method's locals.
internal static class OoO_MoonBlock {
    private static readonly HashSet<string> targets = new();
    private static bool inMoveToTarget;
    private static FloatySpaceBlock currentBlock;
    private static readonly Dictionary<Platform, int> moveCounts = new();

    internal static bool HasTargets => targets.Count > 0;

    internal static void AddTarget(string uid) {
        targets.Add(uid);
    }

    internal static void RemoveTarget(string uid) {
        targets.Remove(uid);
    }

    internal static void ShowTargets() {
        foreach (string target in targets) {
            Celeste.Commands.Log(target);
        }
    }

    internal static void ResetRun() {
        inMoveToTarget = false;
        currentBlock = null;
        moveCounts.Clear();
    }

    private static bool IsTarget(FloatySpaceBlock block) {
        string uid = OoO_ForEach.GetUID(block);
        return targets.Contains(uid) || targets.Contains(block.GetType().Name) || targets.Contains("FloatySpaceBlock");
    }

    [Load]
    public static void Load() {
        using (DetourContextHelper.ManagedHook("TAS Helper OoO MoonBlock").AppliedAfter(DetourContextHelper.CelesteTAS).Use()) {
            On.Celeste.FloatySpaceBlock.Update += OnBlockUpdate;
            On.Celeste.FloatySpaceBlock.MoveToTarget += OnMoveToTarget;
            On.Celeste.Platform.MoveToY_float += OnPlatformMoveToY;
        }
    }

    [Unload]
    public static void Unload() {
        On.Celeste.FloatySpaceBlock.Update -= OnBlockUpdate;
        On.Celeste.FloatySpaceBlock.MoveToTarget -= OnMoveToTarget;
        On.Celeste.Platform.MoveToY_float -= OnPlatformMoveToY;
    }

    private static void OnBlockUpdate(On.Celeste.FloatySpaceBlock.orig_Update orig, FloatySpaceBlock self) {
        if (!OoO_Controller.Active || !IsTarget(self)) {
            orig(self);
            return;
        }
        OoO_Highlight.TrackedEntity = self;
        OoO_Controller.BreakExternal($"{OoO_ForEach.GetUID(self)}'s update starts");
        orig(self);
        OoO_Controller.BreakExternal($"{OoO_ForEach.GetUID(self)}'s update ends");
    }

    private static void OnMoveToTarget(On.Celeste.FloatySpaceBlock.orig_MoveToTarget orig, FloatySpaceBlock self) {
        if (!OoO_Controller.Active || !IsTarget(self)) {
            orig(self);
            return;
        }
        inMoveToTarget = true;
        currentBlock = self;
        moveCounts.Clear();
        orig(self);
        inMoveToTarget = false;
        currentBlock = null;
    }

    private static void OnPlatformMoveToY(On.Celeste.Platform.orig_MoveToY_float orig, Platform platform, float y) {
        if (OoO_Controller.Active && inMoveToTarget && currentBlock is not null) {
            int count = moveCounts.TryGetValue(platform, out int c) ? c + 1 : 1;
            moveCounts[platform] = count;
            int round = count <= currentBlock.Moves.Count ? 1 : 2;
            OoO_Highlight.TrackedEntity = platform;
            OoO_Controller.BreakExternal($"{OoO_ForEach.GetUID(platform)} [round_{round}] moves");
        }
        orig(platform, y);
    }
}
