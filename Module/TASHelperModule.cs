using Celeste.Mod.TASHelper.Utils;
using FMOD.Studio;

namespace Celeste.Mod.TASHelper.Module;

public class TASHelperModule : EverestModule {

    public static TASHelperModule Instance;
    public TASHelperModule() {
        Instance = this;
    }

    public override Type SettingsType => typeof(TASHelperSettings);
    public override void Load() {
        On.Monocle.MInput.Update += HotkeysPressed;
        Loader.HelperLoad();
        Loader.EntityLoad();
    }

    public override void Unload() {
        On.Monocle.MInput.Update -= HotkeysPressed;
        Loader.HelperUnload();
        Loader.EntityUnload();
    }

    public override void Initialize() {
        Loader.Initialize();
    }

    public override void LoadContent(bool firstLoad) {
        if (firstLoad) {
            Loader.LoadContent();
        }
    }

    public override void LoadSettings() {
        base.LoadSettings();
        TasHelperSettings.OnLoadSettings();
    }

    public override void CreateModMenuSection(TextMenu menu, bool inGame, EventInstance snapshot) {
        CreateModMenuSectionHeader(menu, inGame, snapshot);
        TASHelperMenu.CreateMenu(this, menu, inGame);
    }


    private static void HotkeysPressed(On.Monocle.MInput.orig_Update orig) {
        orig();
        if (TasHelperSettings.SettingsHotkeysPressed()) {
            Instance.SaveSettings();
        }
    }
}










