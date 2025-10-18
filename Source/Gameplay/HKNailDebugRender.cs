using Microsoft.Xna.Framework;
using Monocle;
using System.Reflection;

namespace Celeste.Mod.TASHelper.Gameplay;
internal static class HKNailDebugRender {

    // Kepler mod also has hk nail, but it has a good DebugRender
    [Initialize]
    private static void Initialize() {
        prepared = true;
        if (ModUtils.GetType("FlaglinesAndSuch", "FlaglinesAndSuch.HKnail") is { } hkNail) {
            nailTimerGetter = hkNail.GetFieldInfo("nailTimer");
            rechargeTimerGetter = hkNail.GetFieldInfo("nailRechargeTimer");
            nailDir = hkNail.GetFieldInfo("nailDir");
            if (nailTimerGetter is null || rechargeTimerGetter is null || nailDir is null) {
                prepared = false;
            }
        }
        else {
            prepared = false;
        }
        if (prepared && ModUtils.GetType("FlaglinesAndSuch", "FlaglinesAndSuch.Class1") is { } moduleType && ModUtils.GetType("FlaglinesAndSuch", "FlaglinesAndSuch.FlaglinesAndSuchModuleSettings") is { } settingType && settingType.GetPropertyInfo("PlayerAlwaysHasNail") is { } alwaysHasNail && moduleType.GetPropertyValue<EverestModuleSettings>("Settings") is { } settings) {
            flaglinesAndSuchSettings = settings;
            alwaysHasNailGetter = alwaysHasNail;
        }
        else {
            prepared = false;
        }
    }

    private static FieldInfo nailTimerGetter;

    private static FieldInfo rechargeTimerGetter;

    private static FieldInfo nailDir;

    private static EverestModuleSettings flaglinesAndSuchSettings;

    private static PropertyInfo alwaysHasNailGetter;

    private static bool PlayerAlwaysHasNail => (bool)alwaysHasNailGetter.GetValue(flaglinesAndSuchSettings);

    private static bool prepared = false;

    [AddDebugRender]
    private static void PatchEntityListDebugRender(EntityList self, Camera camera) {
        if (!prepared || Engine.Scene is not Level level) {
            return;
        }
        if (!PlayerAlwaysHasNail && !level.Session.GetFlag("flaglinesandsuch_nail_enabled")) {
            return;
        }
        if (playerInstance is not Player player) {
            return;
        }
        if ((float)nailTimerGetter.GetValue(null) > 0f || (float)rechargeTimerGetter.GetValue(null) == 0.1f) {
            Vector2 orig_Position = player.Position;
            Collider collider = player.Collider;
            int dir = (int)nailDir.GetValue(null);
            player.Position = Spinner.Info.PositionHelper.PlayerPositionBeforeSelfUpdate;
            player.Collider = dir switch {
                0 => nailhitboxUp,
                1 => nailhitboxDown,
                2 => nailhitboxLeft,
                3 => nailhitboxRight,
                _ => nailhitboxRight
            };
            player.Collider.Render(camera);
            player.Collider = collider;
            player.Position = orig_Position;
        }
    }

    private static readonly Hitbox nailhitboxDown = new Hitbox(16f, 14f, -8f);

    private static readonly Hitbox nailhitboxUp = new Hitbox(16f, 14f, -8f, -25f);

    private static readonly Hitbox nailhitboxRight = new Hitbox(14f, 16f, 4f, -12f);

    private static readonly Hitbox nailhitboxLeft = new Hitbox(14f, 16f, -18f, -12f);
}