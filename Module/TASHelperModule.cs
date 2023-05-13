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
        On.Monocle.MInput.Update += HotkeysSaveSettings;
        On.Celeste.Level.Render += HotkeysPressed;
        Loader.HelperLoad();
        Loader.EntityLoad();
    }

    public override void Unload() {
        On.Monocle.MInput.Update -= HotkeysSaveSettings;
        On.Celeste.Level.Render -= HotkeysPressed;
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


    private static void HotkeysSaveSettings(On.Monocle.MInput.orig_Update orig) {
        orig();
        if (TASHelperSettings.hotkeysPressed) {
            Instance.SaveSettings();
            TASHelperSettings.hotkeysPressed = false;
        }
        // some part of MInput_Update is taken over by CelesteTAS, in that case orig() will not be called
        // so we can't press hotkeys here
    }

    private static void HotkeysPressed(On.Celeste.Level.orig_Render orig, Level self) {
        orig(self);
        TASHelperSettings.hotkeysPressed = TasHelperSettings.SettingsHotkeysPressed();
        // if you call Instance.SaveSettings() here, then the game will crash if you open Menu-Mod Options in a Level and close the menu.
    }
}










