using Celeste.Mod.TASHelper.Module;
using Celeste.Mod.UI;
using Microsoft.Xna.Framework;
using Monocle;
using System.Collections;
using System.Text.RegularExpressions;
using TAS.EverestInterop.Hitboxes;
using TAS.Module;
using static Celeste.Mod.TASHelper.Module.TASHelperSettings;
using static Celeste.TextMenuExt;
using static TAS.EverestInterop.Hitboxes.HitboxColor;

namespace Celeste.Mod.TASHelper.Utils;

public static class CustomColors {

    internal static readonly Color defaultLoadRangeColliderColor = Color.Lime;
    internal static readonly Color defaultInViewRangeColor = Color.Yellow * 0.8f;
    internal static readonly Color defaultNearPlayerRangeColor = Color.Lime * 0.8f;
    internal static readonly Color defaultCameraTargetColor = Color.Goldenrod;
    internal static readonly Color defaultNotInViewColor = Color.Lime;
    internal static readonly Color defaultNeverActivateColor = new Color(0.25f, 1f, 1f);
    internal static readonly Color defaultActivateEveryFrameColor = new Color(0.8f, 0f, 0f);

    public static void ResetOtherColor() {
        LoadRangeColliderColor = defaultLoadRangeColliderColor;
        InViewRangeColor = defaultInViewRangeColor;
        NearPlayerRangeColor = defaultNearPlayerRangeColor;
        CameraTargetColor = defaultCameraTargetColor;
    }

    public static void ResetSpinnerColor() {
        SpinnerColor_NotInView = defaultNotInViewColor;
        SpinnerColor_NeverActivate = defaultNeverActivateColor;
        SpinnerColor_ActivateEveryFrame = defaultActivateEveryFrameColor;
        SpinnerColor_TasModEntityHitboxColor = HitboxColor.DefaultEntityColor;
        SpinnerColor_TasModCycleHitboxColor1 = CycleHitboxColor.DefaultColor1;
        SpinnerColor_TasModCycleHitboxColor2 = CycleHitboxColor.DefaultColor2;
        SpinnerColor_TasModCycleHitboxColor3 = CycleHitboxColor.DefaultColor3;
        SpinnerColor_TasModOtherCyclesHitboxColor = CycleHitboxColor.DefaultOthersColor;
    }


    // whenever change these names, update Dialog and cmd

    public static Color LoadRangeColliderColor { get => TasHelperSettings.LoadRangeColliderColor; set => TasHelperSettings.LoadRangeColliderColor = value; }
    public static Color InViewRangeColor { get => TasHelperSettings.InViewRangeColor; set => TasHelperSettings.InViewRangeColor = value; }
    public static Color NearPlayerRangeColor { get => TasHelperSettings.NearPlayerRangeColor; set => TasHelperSettings.NearPlayerRangeColor = value; }
    public static Color CameraTargetColor { get => TasHelperSettings.CameraTargetColor; set => TasHelperSettings.CameraTargetColor = value; }

    public static Color SpinnerColor_NotInView { get => TasHelperSettings.NotInViewColor; set => TasHelperSettings.NotInViewColor = value; }
    public static Color SpinnerColor_NeverActivate { get => TasHelperSettings.NeverActivateColor; set => TasHelperSettings.NeverActivateColor = value; }
    public static Color SpinnerColor_ActivateEveryFrame { get => TasHelperSettings.ActivateEveryFrameColor; set => TasHelperSettings.ActivateEveryFrameColor = value; }

    public static Color SpinnerColor_TasModEntityHitboxColor { get => TasSettings.EntityHitboxColor; set => TasSettings.EntityHitboxColor = value; }
    public static Color SpinnerColor_TasModCycleHitboxColor1 { get => TasSettings.CycleHitboxColor1; set => TasSettings.CycleHitboxColor1 = value; }

    public static Color SpinnerColor_TasModCycleHitboxColor2 { get => TasSettings.CycleHitboxColor2; set => TasSettings.CycleHitboxColor2 = value; }

    public static Color SpinnerColor_TasModCycleHitboxColor3 { get => TasSettings.CycleHitboxColor3; set => TasSettings.CycleHitboxColor3 = value; }

    public static Color SpinnerColor_TasModOtherCyclesHitboxColor { get => TasSettings.OtherCyclesHitboxColor; set => TasSettings.OtherCyclesHitboxColor = value; }



    public static TextMenu.Item CreateChangeColorItem(Func<Color> getter, Action<Color> setter, string name, TextMenu textMenu, bool inGame) {
        TextMenu.Item item = new ButtonColorExt(name.ToDialogText(), getter).Pressed(
            () => {
                Audio.Play("event:/ui/main/savefile_rename_start");
                textMenu.SceneAs<Overworld>().Goto<OuiModOptionStringHexColor>()
                    .Init<OuiModOptions>(ColorToHex(getter()),
                        value => setter(HexToColor(value, getter())), 9);
            });
        item.Disabled = inGame;
        return item;
    }

    public static void AddDescriptionOnEnter(this List<TextMenu.Item> page, TextMenu menu, TextMenu.Item item, string description) {
        TextMenuExt.EaseInSubHeaderExt descriptionText = new(description, false, menu) {
            TextColor = Color.Gray,
            HeightExtra = 0f
        };
        page.Add(descriptionText);
        item.OnEnter += () => descriptionText.FadeVisible = true;
        item.OnLeave += () => descriptionText.FadeVisible = false;
    }

    public static void AddItemWithDescription(TextMenu menu, List<TextMenu.Item> page, bool inGame, Func<Color> getter, Action<Color> setter, string name, string cmd = "", string description = "") {
        TextMenu.Item item = CreateChangeColorItem(getter, setter, name, menu, inGame);
        page.Add(item);
        if (!string.IsNullOrEmpty(description)) {
            page.AddDescriptionOnEnter(menu, item, description);
        }
        if (!string.IsNullOrEmpty(cmd) && inGame) {
            TextMenuExt.SubHeaderExt cmdText = new(cmd) {
                TextColor = Color.Gray,
                HeightExtra = 0f
            };
            page.Add(cmdText);
        }
    }

    internal static List<TextMenu.Item> CreateColorCustomization_PageSpinnerColor(TextMenu menu, bool inGame) {
        List<TextMenu.Item> page = new();
        if (inGame) {
            TextMenuExt.SubHeaderExt remindText = new("Color Customization Remind".ToDialogText()) {
                TextColor = Color.Gray,
                HeightExtra = 0f
            };
            page.Add(remindText);
        }
        TextMenuExt.SubHeaderExt formatText = new("Color Customization Color Format".ToDialogText()) {
            TextColor = Color.Gray,
            HeightExtra = 0f
        };
        page.Add(formatText);
        AddItemWithDescription(menu, page, inGame, () => SpinnerColor_NotInView, value => SpinnerColor_NotInView = value, "SpinnerColor_NotInView", "Console command: tashelper_custom_color, SpinnerColor_NotInView, FF00FF00");
        AddItemWithDescription(menu, page, inGame, () => SpinnerColor_NeverActivate, value => SpinnerColor_NeverActivate = value, "SpinnerColor_NeverActivate", "Console command: tashelper_custom_color, SpinnerColor_NeverActivate, FF3FFFFF");
        AddItemWithDescription(menu, page, inGame, () => SpinnerColor_ActivateEveryFrame, value => SpinnerColor_ActivateEveryFrame = value, "SpinnerColor_ActivateEveryFrame", "Console command: tashelper_custom_color, SpinnerColor_ActivateEveryFrame, FFCC0000");
        AddItemWithDescription(menu, page, inGame, () => SpinnerColor_TasModEntityHitboxColor, value => SpinnerColor_TasModEntityHitboxColor = value, "SpinnerColor_TasModEntityHitboxColor", "Console command: tashelper_custom_color, SpinnerColor_TasModEntityHitboxColor, FFFF0000");
        AddItemWithDescription(menu, page, inGame, () => SpinnerColor_TasModCycleHitboxColor1, value => SpinnerColor_TasModCycleHitboxColor1 = value, "SpinnerColor_TasModCycleHitboxColor1", "Console command: tashelper_custom_color, SpinnerColor_TasModCycleHitboxColor1, FFFF0000");
        AddItemWithDescription(menu, page, inGame, () => SpinnerColor_TasModCycleHitboxColor2, value => SpinnerColor_TasModCycleHitboxColor2 = value, "SpinnerColor_TasModCycleHitboxColor2", "Console command: tashelper_custom_color, SpinnerColor_TasModCycleHitboxColor2, FFFFFF00");
        AddItemWithDescription(menu, page, inGame, () => SpinnerColor_TasModCycleHitboxColor3, value => SpinnerColor_TasModCycleHitboxColor3 = value, "SpinnerColor_TasModCycleHitboxColor3", "Console command: tashelper_custom_color, SpinnerColor_TasModCycleHitboxColor3, FF1933FF");
        AddItemWithDescription(menu, page, inGame, () => SpinnerColor_TasModOtherCyclesHitboxColor, value => SpinnerColor_TasModOtherCyclesHitboxColor = value, "SpinnerColor_TasModOtherCyclesHitboxColor", "Console command: tashelper_custom_color, SpinnerColor_TasModOtherCyclesHitboxColor, FF3FFF7F");
        TextMenuExt.SubHeaderExt descriptionText = new("Color Customization SpinnerColor Footnote".ToDialogText()) {
            TextColor = Color.Gray,
            HeightExtra = 0f
        };
        page.Add(descriptionText);
        return page;

    }

    internal static List<TextMenu.Item> CreateColorCustomization_PageOther(TextMenu menu, bool inGame) {
        List<TextMenu.Item> page = new();
        if (inGame) {
            TextMenuExt.SubHeaderExt remindText = new("Color Customization Remind".ToDialogText()) {
                TextColor = Color.Gray,
                HeightExtra = 0f
            };
            page.Add(remindText);
        }
        TextMenuExt.SubHeaderExt formatText = new("Color Customization Color Format".ToDialogText()) {
            TextColor = Color.Gray,
            HeightExtra = 0f
        };
        page.Add(formatText);
        AddItemWithDescription(menu, page, inGame, () => InViewRangeColor, value => InViewRangeColor = value, "InView Range Color", "Console command: tashelper_custom_color, InViewRangeColor, CCCCCC00");
        AddItemWithDescription(menu, page, inGame, () => NearPlayerRangeColor, value => NearPlayerRangeColor = value, "NearPlayer Range Color", "Console command: tashelper_custom_color, NearPlayerRangeColor, CC00CC00");
        AddItemWithDescription(menu, page, inGame, () => CameraTargetColor, value => CameraTargetColor = value, "CameraTarget Color", "Console command: tashelper_custom_color, CameraTargetColor, FFDAA520");
        AddItemWithDescription(menu, page, inGame, () => LoadRangeColliderColor, value => LoadRangeColliderColor = value, "Load Range Collider Color", "Console command: tashelper_custom_color, LoadRangeColliderColor, FF00FF00", "Load Range Collider Description".ToDialogText());
        return page;
    }

    internal static List<TextMenu.Item> CreateColorCustomization_PageOnOff(TextMenu menu, bool inGame) {
        List<TextMenu.Item> page = new List<TextMenu.Item>();
        page.Add(new TextMenu.OnOff("Show Cycle Hitbox Colors".ToDialogText(), TasHelperSettings.ShowCycleHitboxColors).Change(value => TasHelperSettings.ShowCycleHitboxColors = value));
        TextMenu.Item NotInViewColorItem = new TextMenuExt.EnumerableSlider<UsingNotInViewColorModes>("Using NotInView Color Modes".ToDialogText(), TASHelperMenu.CreateUsingNotInViewColorOptions(),
                    TasHelperSettings.UsingNotInViewColorMode).Change(value => TasHelperSettings.UsingNotInViewColorMode = value);
        // NotInViewColorItem.IncludeWidthInMeasurement = false;
        page.Add(NotInViewColorItem);
        page.AddDescriptionOnEnter(menu, NotInViewColorItem, "Using NotInView Color Description".ToDialogText());
        TextMenu.Item UsingFreezeColorItem;
        page.Add(UsingFreezeColorItem = new TextMenu.OnOff("Using Freeze Color".ToDialogText(), TasHelperSettings.UsingFreezeColor).Change(value => TasHelperSettings.UsingFreezeColor = value));
        page.AddDescriptionOnEnter(menu, UsingFreezeColorItem, "Using Freeze Color Description".ToDialogText());
        TextMenu.Item resetButton = new TextMenu.Button("Reset Custom Color".ToDialogText()).Pressed(
            () => {
                Audio.Play("event:/ui/main/rename_entry_accept");
                ResetSpinnerColor();
                ResetOtherColor();
            }
        );
        page.Add(resetButton);
        return page;
    }


    [Command("tashelper_custom_color", "Check TASHelper mod options menu for help.")]
    public static void CmdCustomColor(string field, string color) {
        switch (field) {
            case "SpinnerColor_NotInView": {
                    SpinnerColor_NotInView = HexToColorWithLog(color, defaultNotInViewColor);
                    TASHelperModule.Instance.SaveSettings();
                    return;
                }

            case "SpinnerColor_NeverActivate": {
                    SpinnerColor_NeverActivate = HexToColorWithLog(color, defaultNeverActivateColor);
                    TASHelperModule.Instance.SaveSettings();
                    return;
                }

            case "SpinnerColor_ActivateEveryFrame": {
                    SpinnerColor_ActivateEveryFrame = HexToColorWithLog(color, defaultActivateEveryFrameColor);
                    TASHelperModule.Instance.SaveSettings();
                    return;
                }

            case "SpinnerColor_TasModEntityHitboxColor": {
                    SpinnerColor_TasModEntityHitboxColor = HexToColorWithLog(color, HitboxColor.DefaultEntityColor);
                    CelesteTasModule.Instance.SaveSettings();
                    return;
                }

            case "SpinnerColor_TasModCycleHitboxColor1": {
                    SpinnerColor_TasModCycleHitboxColor1 = HexToColorWithLog(color, CycleHitboxColor.DefaultColor1);
                    CelesteTasModule.Instance.SaveSettings();
                    return;
                }

            case "SpinnerColor_TasModCycleHitboxColor2": {
                    SpinnerColor_TasModCycleHitboxColor2 = HexToColorWithLog(color, CycleHitboxColor.DefaultColor2);
                    CelesteTasModule.Instance.SaveSettings();
                    return;
                }

            case "SpinnerColor_TasModCycleHitboxColor3": {
                    SpinnerColor_TasModCycleHitboxColor3 = HexToColorWithLog(color, CycleHitboxColor.DefaultColor3);
                    CelesteTasModule.Instance.SaveSettings();
                    return;
                }

            case "SpinnerColor_TasModOtherCyclesHitboxColor": {
                    SpinnerColor_TasModOtherCyclesHitboxColor = HexToColorWithLog(color, CycleHitboxColor.DefaultOthersColor);
                    CelesteTasModule.Instance.SaveSettings();
                    return;
                }

            case "LoadRangeColliderColor": {
                    LoadRangeColliderColor = HexToColorWithLog(color, defaultLoadRangeColliderColor);
                    TASHelperModule.Instance.SaveSettings();
                    return;
                }

            case "CameraTargetColor": {
                    CameraTargetColor = HexToColorWithLog(color, defaultCameraTargetColor);
                    TASHelperModule.Instance.SaveSettings();
                    return;
                }

            case "NearPlayerRangeColor": {
                    NearPlayerRangeColor = HexToColorWithLog(color, defaultNearPlayerRangeColor);
                    TASHelperModule.Instance.SaveSettings();
                    return;
                }

            case "InViewRangeColor": {
                    InViewRangeColor = HexToColorWithLog(color, defaultInViewRangeColor);
                    TASHelperModule.Instance.SaveSettings();
                    return;
                }

            default: {
                    Celeste.Commands.Log("Invalid field name");
                    return;
                }
        }
    }

    private static readonly Regex HexChar = new Regex("^[0-9a-f]*$", RegexOptions.IgnoreCase);
    public static Color HexToColorWithLog(string hex, Color defaultColor) {
        if (string.IsNullOrWhiteSpace(hex)) {
            return defaultColor;
        }

        hex = hex.Replace("#", "");
        if (!HexChar.IsMatch(hex)) {
            Celeste.Commands.Log("Invaild Color");
            return defaultColor;
        }

        if (hex.Length > 8) {
            hex = hex.Substring(0, 8);
        }

        if (hex.Length == 3 || hex.Length == 4) {
            hex = (from c in hex.ToCharArray()
                   select $"{c}{c}").Aggregate((string s, string s1) => s + s1);
        }

        hex = hex.PadLeft(8, 'F');
        try {
            long num = Convert.ToInt64(hex, 16);
            Color result = default(Color);
            result.A = (byte)(num >> 24);
            result.R = (byte)(num >> 16);
            result.G = (byte)(num >> 8);
            result.B = (byte)num;
            return result;
        }
        catch (FormatException) {
            Celeste.Commands.Log("Invaild Color");
            return defaultColor;
        }
    }
}


// basically copied from Everest

public class OptionSubMenuCountExt : TextMenu.Item {
    public string Label;

    private MTexture Icon;

    private List<Tuple<string, List<TextMenu.Item>>> delayedAddMenus;

    public int MenuIndex;

    private int InitialSelection;

    public int Selection;

    private int lastDir;

    private float sine;

    public Action<int> OnValueChange;

    public float ItemSpacing;

    public float ItemIndent;

    private Color HighlightColor;

    public string ConfirmSfx;

    public bool AlwaysCenter;

    public float LeftColumnWidth;

    public float RightColumnWidth;

    private float _MenuHeight;

    public bool Focused;

    private bool wasFocused;

    private bool containerAutoScroll;

    public static int HoverableCount(List<TextMenu.Item> list) {
        int count = 0;
        foreach (TextMenu.Item item in list) {
            if (item.Hoverable) {
                count++;
            }
        }
        return count;
    }
    public List<Tuple<string, List<TextMenu.Item>>> Menus { get; private set; }

    public List<TextMenu.Item> CurrentMenu {
        get {
            if (Menus.Count <= 0) {
                return null;
            }

            return Menus[MenuIndex].Item2;
        }
    }

    public TextMenu.Item Current {
        get {
            if (CurrentMenu.Count <= 0 || Selection < 0) {
                return null;
            }

            return CurrentMenu[Selection];
        }
    }

    public int FirstPossibleSelection {
        get {
            for (int i = 0; i < CurrentMenu.Count; i++) {
                if (CurrentMenu[i] != null && CurrentMenu[i].Hoverable) {
                    return i;
                }
            }

            return 0;
        }
    }

    public int LastPossibleSelection {
        get {
            for (int num = CurrentMenu.Count - 1; num >= 0; num--) {
                if (CurrentMenu[num] != null && CurrentMenu[num].Hoverable) {
                    return num;
                }
            }

            return 0;
        }
    }

    public float ScrollTargetY {
        get {
            float min = (float)(Engine.Height - 150) - Container.Height * Container.Justify.Y;
            float max = 150f + Container.Height * Container.Justify.Y;
            return Calc.Clamp((float)(Engine.Height / 2) + Container.Height * Container.Justify.Y - GetYOffsetOf(Current), min, max);
        }
    }

    public float TitleHeight { get; private set; }

    public float MenuHeight { get; private set; }

    public OptionSubMenuCountExt(string label) {
        ConfirmSfx = "event:/ui/main/button_select";
        Label = label;
        Icon = GFX.Gui["downarrow"];
        Selectable = true;
        IncludeWidthInMeasurement = true;
        MenuIndex = 0;
        Menus = new List<Tuple<string, List<TextMenu.Item>>>();
        delayedAddMenus = new List<Tuple<string, List<TextMenu.Item>>>();
        Selection = -1;
        ItemSpacing = 4f;
        ItemIndent = 20f;
        HighlightColor = Color.White;
        RecalculateSize();
    }

    public OptionSubMenuCountExt Add(string label, List<TextMenu.Item> items) {
        if (Container != null) {
            if (items != null) {
                foreach (TextMenu.Item item in items) {
                    item.Container = Container;
                    Container.Add(item.ValueWiggler = Wiggler.Create(0.25f, 3f));
                    Container.Add(item.SelectWiggler = Wiggler.Create(0.25f, 3f));
                    item.ValueWiggler.UseRawDeltaTime = (item.SelectWiggler.UseRawDeltaTime = true);
                    item.Added();
                }

                Menus.Add(new Tuple<string, List<TextMenu.Item>>(label, items));
            }
            else {
                Menus.Add(new Tuple<string, List<TextMenu.Item>>(label, new List<TextMenu.Item>()));
            }

            if (Selection == -1) {
                FirstSelection();
            }

            RecalculateSize();
            return this;
        }

        delayedAddMenus.Add(new Tuple<string, List<TextMenu.Item>>(label, items));
        return this;
    }

    public OptionSubMenuCountExt SetInitialSelection(int index) {
        InitialSelection = index;
        return this;
    }

    public void Clear() {
        Menus = new List<Tuple<string, List<TextMenu.Item>>>();
    }

    public void FirstSelection() {
        Selection = -1;
        if (HoverableCount(CurrentMenu) > 0) {
            MoveSelection(1, wiggle: true);
        }
    }

    public void MoveSelection(int direction, bool wiggle = false) {
        int selection = Selection;
        direction = Math.Sign(direction);
        int num = 0;
        foreach (TextMenu.Item item in CurrentMenu) {
            if (item.Hoverable) {
                num++;
            }
        }

        do {
            Selection += direction;
            if (num > 2) {
                if (Selection < 0) {
                    Selection = CurrentMenu.Count - 1;
                }
                else if (Selection >= CurrentMenu.Count) {
                    Selection = 0;
                }
            }
            else if (Selection < 0 || Selection > CurrentMenu.Count - 1) {
                Selection = Calc.Clamp(Selection, 0, CurrentMenu.Count - 1);
                break;
            }
        }
        while (!Current.Hoverable);
        if (!Current.Hoverable) {
            Selection = selection;
        }

        if (Selection != selection && Current != null) {
            if (selection >= 0 && CurrentMenu[selection] != null && CurrentMenu[selection].OnLeave != null) {
                CurrentMenu[selection].OnLeave();
            }

            Current.OnEnter?.Invoke();
            if (wiggle) {
                Audio.Play((direction > 0) ? "event:/ui/main/rollover_down" : "event:/ui/main/rollover_up");
                Current.SelectWiggler.Start();
            }
        }
    }

    public void RecalculateSize() {
        TitleHeight = ActiveFont.LineHeight;
        LeftColumnWidth = (RightColumnWidth = (_MenuHeight = 0f));
        if (Menus.Count < 1 || CurrentMenu == null) {
            return;
        }

        foreach (TextMenu.Item item in CurrentMenu) {
            if (item.IncludeWidthInMeasurement) {
                LeftColumnWidth = Math.Max(LeftColumnWidth, item.LeftWidth());
            }
        }

        foreach (TextMenu.Item item2 in CurrentMenu) {
            if (item2.IncludeWidthInMeasurement) {
                RightColumnWidth = Math.Max(RightColumnWidth, item2.RightWidth());
            }
        }

        foreach (TextMenu.Item item3 in CurrentMenu) {
            if (item3.Visible) {
                _MenuHeight += item3.Height() + Container.ItemSpacing;
            }
        }

        _MenuHeight -= Container.ItemSpacing;
    }

    public float GetYOffsetOf(TextMenu.Item item) {
        float num = Container.GetYOffsetOf(this) - Height() * 0.5f;
        if (item == null) {
            return num + TitleHeight * 0.5f;
        }

        num += TitleHeight;
        foreach (TextMenu.Item item2 in CurrentMenu) {
            if (item2.Visible) {
                num += item2.Height() + ItemSpacing;
            }

            if (item2 == item) {
                break;
            }
        }

        return num - item.Height() * 0.5f - ItemSpacing;
    }

    public OptionSubMenuCountExt Change(Action<int> onValueChange) {
        OnValueChange = onValueChange;
        return this;
    }

    public override void LeftPressed() {
        if (MenuIndex > 0) {
            Audio.Play("event:/ui/main/button_toggle_off");
            MenuIndex--;
            lastDir = -1;
            ValueWiggler.Start();
            FirstSelection();
            OnValueChange?.Invoke(MenuIndex);
        }
    }

    public override void RightPressed() {
        if (MenuIndex < Menus.Count - 1) {
            Audio.Play("event:/ui/main/button_toggle_on");
            MenuIndex++;
            lastDir = 1;
            ValueWiggler.Start();
            FirstSelection();
            OnValueChange?.Invoke(MenuIndex);
        }
    }

    public override void ConfirmPressed() {
        if (HoverableCount(CurrentMenu) > 0) {
            containerAutoScroll = Container.AutoScroll;
            Container.AutoScroll = false;
            Container.Focused = false;
            Focused = true;
            FirstSelection();
        }
    }

    public override float LeftWidth() {
        return ActiveFont.Measure(Label).X;
    }

    public override float RightWidth() {
        float num = 0f;
        foreach (string item in Menus.Select((Tuple<string, List<TextMenu.Item>> tuple) => tuple.Item1)) {
            num = Math.Max(num, ActiveFont.Measure(item).X);
        }

        return num + 60f;
    }

    public override float Height() {
        return TitleHeight + Math.Max(MenuHeight, 0f);
    }

    public override void Added() {
        base.Added();
        foreach (Tuple<string, List<TextMenu.Item>> delayedAddMenu in delayedAddMenus) {
            Add(delayedAddMenu.Item1, delayedAddMenu.Item2);
        }

        MenuIndex = InitialSelection;
    }

    public override void Update() {
        MenuHeight = Calc.Approach(MenuHeight, _MenuHeight, Engine.RawDeltaTime * Math.Abs(MenuHeight - _MenuHeight) * 8f);
        sine += Engine.RawDeltaTime;
        base.Update();
        if (CurrentMenu == null) {
            return;
        }

        if (Focused) {
            if (!wasFocused) {
                wasFocused = true;
            }
            else {
                if (Input.MenuDown.Pressed && (!Input.MenuDown.Repeating || Selection != LastPossibleSelection)) {
                    MoveSelection(1, wiggle: true);
                }
                else if (Input.MenuUp.Pressed && (!Input.MenuUp.Repeating || Selection != FirstPossibleSelection)) {
                    MoveSelection(-1, wiggle: true);
                }

                if (Current != null) {
                    if (Input.MenuLeft.Pressed) {
                        Current.LeftPressed();
                    }

                    if (Input.MenuRight.Pressed) {
                        Current.RightPressed();
                    }

                    if (Input.MenuConfirm.Pressed) {
                        Current.ConfirmPressed();
                        Current.OnPressed?.Invoke();
                    }

                    if (Input.MenuJournal.Pressed && Current.OnAltPressed != null) {
                        Current.OnAltPressed();
                    }
                }

                if (!Input.MenuConfirm.Pressed && (Input.MenuCancel.Pressed || Input.ESC.Pressed || Input.Pause.Pressed)) {
                    Current?.OnLeave?.Invoke();
                    Focused = false;
                    Audio.Play("event:/ui/main/button_back");
                    Container.AutoScroll = containerAutoScroll;
                    Container.Focused = true;
                }
            }
        }
        else {
            wasFocused = false;
        }

        foreach (Tuple<string, List<TextMenu.Item>> menu in Menus) {
            foreach (TextMenu.Item item in menu.Item2) {
                item.OnUpdate?.Invoke();
                item.Update();
            }
        }

        if (Settings.Instance.DisableFlashes) {
            HighlightColor = TextMenu.HighlightColorA;
        }
        else if (Engine.Scene.OnRawInterval(0.1f)) {
            if (HighlightColor == TextMenu.HighlightColorA) {
                HighlightColor = TextMenu.HighlightColorB;
            }
            else {
                HighlightColor = TextMenu.HighlightColorA;
            }
        }

        if (Focused && containerAutoScroll) {
            if (Container.Height > Container.ScrollableMinSize) {
                Container.Position.Y += (ScrollTargetY - Container.Position.Y) * (1f - (float)Math.Pow(0.0099999997764825821, Engine.RawDeltaTime));
            }
            else {
                Container.Position.Y = 540f;
            }
        }
    }

    public override void Render(Vector2 position, bool highlighted) {
        Vector2 vector = new Vector2(position.X, position.Y - Height() / 2f);
        float alpha = Container.Alpha;
        Color color = (Disabled ? Color.DarkSlateGray : ((highlighted ? Container.HighlightColor : Color.White) * alpha));
        Color strokeColor = Color.Black * (alpha * alpha * alpha);
        bool flag = Container.InnerContent == TextMenu.InnerContentMode.TwoColumn && !AlwaysCenter;
        Vector2 vector2 = vector + Vector2.UnitY * TitleHeight / 2f + (flag ? Vector2.Zero : new Vector2(Container.Width * 0.5f, 0f));
        Vector2 justify = (flag ? new Vector2(0f, 0.5f) : new Vector2(0.5f, 0.5f));
        Vector2 justify2 = (flag ? new Vector2(ActiveFont.Measure(Label).X + (float)Icon.Width, 5f) : new Vector2(ActiveFont.Measure(Label).X / 2f + (float)Icon.Width, 5f));
        MTexture icon = Icon;
        Color obj;
        if (!Disabled) {
            List<TextMenu.Item> currentMenu = CurrentMenu;
            if (currentMenu == null || currentMenu.Count >= 1) {
                obj = (Focused ? Container.HighlightColor : Color.White);
                goto IL_019d;
            }
        }

        obj = Color.DarkSlateGray;
        goto IL_019d;
    IL_019d:
        DrawIcon(vector2, icon, justify2, outline: true, obj * alpha, 0.8f);
        ActiveFont.DrawOutline(Label, vector2, justify, Vector2.One, color, 2f, strokeColor);
        if (Menus.Count > 0) {
            float num = RightWidth();
            ActiveFont.DrawOutline(Menus[MenuIndex].Item1, vector2 + new Vector2(Container.Width - num * 0.5f + (float)lastDir * ValueWiggler.Value * 8f, 0f), new Vector2(0.5f, 0.5f), Vector2.One * 0.8f, color, 2f, strokeColor);
            Vector2 vector3 = Vector2.UnitX * (highlighted ? ((float)Math.Sin(sine * 4f) * 4f) : 0f);
            Color color2 = ((MenuIndex > 0) ? color : (Color.DarkSlateGray * alpha));
            Vector2 position2 = vector2 + new Vector2(Container.Width - num + 40f + ((lastDir < 0) ? ((0f - ValueWiggler.Value) * 8f) : 0f), 0f) - ((MenuIndex > 0) ? vector3 : Vector2.Zero);
            ActiveFont.DrawOutline("<", position2, new Vector2(0.5f, 0.5f), Vector2.One, color2, 2f, strokeColor);
            color2 = ((MenuIndex < Menus.Count - 1) ? color : (Color.DarkSlateGray * alpha));
            position2 = vector2 + new Vector2(Container.Width - 40f + ((lastDir > 0) ? (ValueWiggler.Value * 8f) : 0f), 0f) + ((MenuIndex < Menus.Count - 1) ? vector3 : Vector2.Zero);
            ActiveFont.DrawOutline(">", position2, new Vector2(0.5f, 0.5f), Vector2.One, color2, 2f, strokeColor);
        }

        if (CurrentMenu == null) {
            return;
        }

        Vector2 vector4 = new Vector2(vector.X + ItemIndent, vector.Y + TitleHeight + ItemSpacing);
        float y = vector4.Y;
        RecalculateSize();
        foreach (TextMenu.Item item in CurrentMenu) {
            if (item.Visible) {
                float num2 = item.Height();
                Vector2 position3 = vector4 + new Vector2(0f, num2 * 0.5f + item.SelectWiggler.Value * 8f);
                if (position3.Y - y < MenuHeight && position3.Y + num2 * 0.5f > 0f && position3.Y - num2 * 0.5f < (float)Engine.Height) {
                    item.Render(position3, Focused && Current == item);
                }

                vector4.Y += num2 + ItemSpacing;
            }
        }
    }

    private static void DrawIcon(Vector2 position, MTexture icon, Vector2 justify, bool outline, Color color, float scale) {
        if (outline) {
            icon.DrawOutlineCentered(position + justify, color);
        }
        else {
            icon.DrawCentered(position + justify, color, scale);
        }
    }
}
public class ButtonColorExt : TextMenu.Button, IItemExt {

    public Func<Color> CubeColorGetter = () => Color.White;
    public Color TextColor { get; set; } = Color.White;

    public string name;
    public Color TextColorDisabled { get; set; } = Color.DarkSlateGray;


    public string Icon { get; set; }

    public float? IconWidth { get; set; }

    public bool IconOutline { get; set; }

    public Vector2 Offset { get; set; }

    public float Alpha { get; set; } = 1f;


    public Vector2 Scale { get; set; } = Vector2.One;


    public override float Height() {
        return base.Height() * Scale.Y;
    }

    public override float LeftWidth() {
        return base.LeftWidth() * Scale.X;
    }

    public ButtonColorExt(string label, Func<Color> cubecolorGetter, string icon = null)
        : base(label) {
        CubeColorGetter = cubecolorGetter;
        Icon = icon;
        name = label;
    }

    public override void Render(Vector2 position, bool highlighted) {
        Label = name + $": {ColorToHex(CubeColorGetter())}";
        position += Offset;
        float num = Container.Alpha * Alpha;
        Color color = (Disabled ? TextColorDisabled : (highlighted ? Container.HighlightColor : TextColor)) * num;
        Color strokeColor = Color.Black * (num * num * num);
        bool flag = Container.InnerContent == TextMenu.InnerContentMode.TwoColumn && !AlwaysCenter;
        Vector2 textPosition = position + (flag ? Vector2.Zero : new Vector2(Container.Width * 0.5f, 0f));
        Vector2 justify = (flag ? new Vector2(0f, 0.5f) : new Vector2(0.5f, 0.5f));
        float height = ActiveFont.Measure("I").Y / 2f;
        Vector2 cubePosition = textPosition + new Vector2(ActiveFont.Measure(Label).X + 30f, -height / 2f);
        Draw.Rect(cubePosition - new Vector2(4f, 4f), height + 8f, height + 8f, Color.Black);
        Draw.Rect(cubePosition, height, height, CubeColorGetter());
        DrawIcon(position, Icon, IconWidth, Height(), IconOutline, (Disabled ? Color.DarkSlateGray : (highlighted ? Color.White : Color.LightSlateGray)) * num, ref textPosition);
        ActiveFont.DrawOutline(Label, textPosition, justify, Scale, color, 2f, strokeColor);
    }
}
public class OuiModOptionStringHexColor : Oui, OuiModOptions.ISubmenu {

    private static readonly float fscale = 2f;

    public static bool Cancelled;

    public string StartingValue;

    private string _Value;
    public string Value {
        get {
            return _Value;
        }
        set {
            _Value = value;
            OnValueChange?.Invoke(value);
        }
    }

    public int MaxValueLength;
    public int MinValueLength;

    public event Action<string> OnValueChange;

    public event Action<bool> OnExit;

    private string[] letters;
    private int index = 0;
    private int line = 0;
    private float widestLetter;
    private float widestLine;
    private int widestLineCount;
    private bool selectingOptions = true;
    private int optionsIndex;
    private float lineHeight;
    private float lineSpacing;
    private float boxPadding;
    private float optionsScale;
    private string cancel;
    private string space;
    private string backspace;
    private string accept;
    private float cancelWidth;
    private float spaceWidth;
    private float backspaceWidth;
    private float beginWidth;
    private float optionsWidth;
    private float boxWidth;
    private float boxHeight;
    private float pressedTimer;
    private float timer;
    private float ease;

    private Wiggler wiggler;

    private Color unselectColor = Color.LightGray;
    private Color selectColorA = Calc.HexToColor("84FF54");
    private Color selectColorB = Calc.HexToColor("FCFF59");
    private Color disableColor = Color.DarkSlateBlue;

    private Vector2 boxtopleft {
        get {
            return Position + new Vector2((1920f - boxWidth) / 2f, 360f + (680f - boxHeight) / 2f);
        }
    }

    public OuiModOptionStringHexColor()
        : base() {
        wiggler = Wiggler.Create(0.25f, 4f);
        Position = new Vector2(0f, 1080f);
        Visible = false;
    }

    public OuiModOptionStringHexColor Init<T>(string value, Action<string> onValueChange) where T : Oui {
        return Init<T>(value, onValueChange, 12, 1);
    }

    public OuiModOptionStringHexColor Init<T>(string value, Action<string> onValueChange, int maxValueLength) where T : Oui {
        return Init<T>(value, onValueChange, maxValueLength, 1);
    }

    public OuiModOptionStringHexColor Init<T>(string value, Action<string> onValueChange, int maxValueLength, int minValueLength) where T : Oui {
        return Init(value, onValueChange, (confirm) => Overworld.Goto<T>(), maxValueLength, minValueLength);
    }

    public OuiModOptionStringHexColor Init<T>(string value, Action<string> onValueChange, Action<bool> onExit, int maxValueLength, int minValueLength) where T : Oui {
        return Init(value, onValueChange, (confirm) => { Overworld.Goto<T>(); onExit?.Invoke(confirm); }, maxValueLength, minValueLength);
    }

    public OuiModOptionStringHexColor Init(string value, Action<string> onValueChange, Action<bool> exit, int maxValueLength, int minValueLength) {
        _Value = StartingValue = value ?? "";
        OnValueChange = onValueChange;

        MaxValueLength = maxValueLength;
        MinValueLength = minValueLength;

        OnExit += exit;
        Cancelled = false;

        return this;
    }

    public override IEnumerator Enter(Oui from) {
        TextInput.OnInput += OnTextInput;

        Overworld.ShowInputUI = false;

        Engine.Commands.Enabled = false;

        selectingOptions = false;
        optionsIndex = 0;
        index = 0;
        line = 0;

        string letterChars = "01234567\n89ABCDEF";
        letters = letterChars.Split('\n');

        foreach (char c in letterChars) {
            float width = fscale * ActiveFont.Measure(c).X;
            if (width > widestLetter) {
                widestLetter = width;
            }
        }

        widestLineCount = 0;
        foreach (string letter in letters) {
            if (letter.Length > widestLineCount) {
                widestLineCount = letter.Length;
            }
        }

        widestLine = widestLineCount * widestLetter;
        letterChars = null;

        lineHeight = fscale * ActiveFont.LineHeight;
        lineSpacing = fscale * ActiveFont.LineHeight * 0.1f;
        boxPadding = widestLetter;
        optionsScale = 0.75f;
        cancel = Dialog.Clean("name_back");
        space = Dialog.Clean("name_space");
        backspace = Dialog.Clean("name_backspace");
        accept = Dialog.Clean("name_accept");
        cancelWidth = ActiveFont.Measure(cancel).X * optionsScale;
        spaceWidth = ActiveFont.Measure(space).X * optionsScale;
        backspaceWidth = ActiveFont.Measure(backspace).X * optionsScale;
        beginWidth = ActiveFont.Measure(accept).X * optionsScale;
        optionsWidth = cancelWidth + spaceWidth + backspaceWidth + beginWidth + widestLetter * 3f;
        boxWidth = Math.Max(widestLine, optionsWidth) + boxPadding * 2f;
        boxHeight = (letters.Length + 1f) * lineHeight + letters.Length * lineSpacing + boxPadding * 3f;

        Visible = true;

        Vector2 posFrom = Position;
        Vector2 posTo = Vector2.Zero;
        for (float t = 0f; t < 1f; t += Engine.DeltaTime * 2f) {
            ease = Ease.CubeIn(t);
            Position = posFrom + (posTo - posFrom) * Ease.CubeInOut(t);
            yield return null;
        }
        ease = 1f;
        posFrom = Vector2.Zero;
        posTo = Vector2.Zero;

        yield return 0.2f;

        Focused = true;

        yield return 0.2f;

        wiggler.Start();
    }

    public override IEnumerator Leave(Oui next) {
        TextInput.OnInput -= OnTextInput;

        Overworld.ShowInputUI = true;
        Focused = false;

        Engine.Commands.Enabled = (Celeste.PlayMode == Celeste.PlayModes.Debug);

        Vector2 posFrom = Position;
        Vector2 posTo = new Vector2(0f, 1080f);
        for (float t = 0f; t < 1f; t += Engine.DeltaTime * 2f) {
            ease = 1f - Ease.CubeIn(t);
            Position = posFrom + (posTo - posFrom) * Ease.CubeInOut(t);
            yield return null;
        }

        Visible = false;
    }

    public bool UseKeyboardInput {
        get {
            var settings = Core.CoreModule.Instance._Settings as Core.CoreModuleSettings;
            return settings?.UseKeyboardForTextInput ?? false;
        }
    }

    public void OnTextInput(char c) {
        if (!UseKeyboardInput) {
            return;
        }

        if (c == (char)13) {
            // Enter - confirm.
            Finish();

        }
        else if (c == (char)8) {
            // Backspace - trim.
            Backspace();

        }
        else if (c == (char)22) {
            // Paste.
            string value = Value + TextInput.GetClipboardText();
            if (value.Length > MaxValueLength)
                value = value.Substring(0, MaxValueLength);
            Value = value;

        }
        else if (c == (char)127) {
            // Delete - currenly not handled.

        }
        else if (c == ' ') {
            // Space - append.
            if (Value.Length < MaxValueLength) {
                Audio.Play(SFX.ui_main_rename_entry_space);
                Value += c;
            }
            else {
                Audio.Play(SFX.ui_main_button_invalid);
            }

        }
        else if (!char.IsControl(c)) {
            // Any other character - append.
            if (Value.Length < MaxValueLength && ActiveFont.FontSize.Characters.ContainsKey(c)) {
                Audio.Play(SFX.ui_main_rename_entry_char);
                Value += c;
            }
            else {
                Audio.Play(SFX.ui_main_button_invalid);
            }
        }
    }

    public override void SceneEnd(Scene scene) {
        Overworld.ShowInputUI = true;
        Engine.Commands.Enabled = (Celeste.PlayMode == Celeste.PlayModes.Debug);
    }

    public override void Update() {
        bool wasFocused = Focused;

        // Only "focus" if we're not using the keyboard for input
        Focused = wasFocused && !UseKeyboardInput;

        base.Update();

        // TODO: Rewrite or study and document the following code.
        // It stems from OuiFileNaming.

        if (!(Selected && Focused)) {
            goto End;
        }

        if (Input.MenuRight.Pressed && (optionsIndex < 3 || !selectingOptions) && (Value.Length > 0 || !selectingOptions)) {
            if (selectingOptions) {
                optionsIndex = Math.Min(optionsIndex + 1, 3);
            }
            else {
                do {
                    index = (index + 1) % letters[line].Length;
                } while (letters[line][index] == ' ');
            }
            wiggler.Start();
            Audio.Play(SFX.ui_main_rename_entry_roll);

        }
        else if (Input.MenuLeft.Pressed && (optionsIndex > 0 || !selectingOptions)) {
            if (selectingOptions) {
                optionsIndex = Math.Max(optionsIndex - 1, 0);
            }
            else {
                do {
                    index = (index + letters[line].Length - 1) % letters[line].Length;
                } while (letters[line][index] == ' ');
            }
            wiggler.Start();
            Audio.Play(SFX.ui_main_rename_entry_roll);

        }
        else if (Input.MenuDown.Pressed && !selectingOptions) {
            int lineNext = line + 1;
            bool something = true;
            for (; lineNext < letters.Length; lineNext++) {
                if (index < letters[lineNext].Length && letters[lineNext][index] != ' ') {
                    something = false;
                    break;
                }
            }

            if (something) {
                selectingOptions = true;

            }
            else {
                line = lineNext;

            }

            if (selectingOptions) {
                float pos = index * widestLetter;
                float offs = boxWidth - boxPadding * 2f;
                if (Value.Length == 0 || pos < cancelWidth + (offs - cancelWidth - beginWidth - backspaceWidth - spaceWidth - widestLetter * 3f) / 2f) {
                    optionsIndex = 0;
                }
                else if (pos < offs - beginWidth - backspaceWidth - widestLetter * 2f) {
                    optionsIndex = 1;
                }
                else if (pos < offs - beginWidth - widestLetter) {
                    optionsIndex = 2;
                }
                else {
                    optionsIndex = 3;
                }
            }

            wiggler.Start();
            Audio.Play(SFX.ui_main_rename_entry_roll);

        }
        else if ((Input.MenuUp.Pressed || (selectingOptions && Value.Length <= 0 && optionsIndex > 0)) && (line > 0 || selectingOptions)) {
            if (selectingOptions) {
                line = letters.Length;
                selectingOptions = false;
                float offs = boxWidth - boxPadding * 2f;
                if (optionsIndex == 0) {
                    index = (int)(cancelWidth / 2f / widestLetter);
                }
                else if (optionsIndex == 1) {
                    index = (int)((offs - beginWidth - backspaceWidth - spaceWidth / 2f - widestLetter * 2f) / widestLetter);
                }
                else if (optionsIndex == 2) {
                    index = (int)((offs - beginWidth - backspaceWidth / 2f - widestLetter) / widestLetter);
                }
                else if (optionsIndex == 3) {
                    index = (int)((offs - beginWidth / 2f) / widestLetter);
                }
            }
            do {
                line--;
            } while (line > 0 && (index >= letters[line].Length || letters[line][index] == ' '));
            while (index >= letters[line].Length || letters[line][index] == ' ') {
                index--;
            }
            wiggler.Start();
            Audio.Play(SFX.ui_main_rename_entry_roll);

        }
        else if (Input.MenuConfirm.Pressed) {
            if (selectingOptions) {
                if (optionsIndex == 0) {
                    Cancel();
                }
                else if (optionsIndex == 1 && Value.Length > 0) {
                    Space();
                }
                else if (optionsIndex == 2) {
                    Backspace();
                }
                else if (optionsIndex == 3) {
                    Finish();
                }
            }
            else if (Value.Length < MaxValueLength) {
                Value += letters[line][index].ToString();
                wiggler.Start();
                Audio.Play(SFX.ui_main_rename_entry_char);
            }
            else {
                Audio.Play(SFX.ui_main_button_invalid);
            }

        }
        else if (Input.MenuCancel.Pressed) {
            if (Value.Length > 0) {
                Backspace();
            }
            else {
                Cancel();
            }

        }
        else if (Input.Pause.Pressed) {
            Finish();
        }

    End:

        if (wasFocused && !Focused) {
            if (Input.ESC) {
                Cancel();
                wasFocused = false;
            }
        }

        Focused = wasFocused;

        pressedTimer -= Engine.DeltaTime;
        timer += Engine.DeltaTime;
        wiggler.Update();
    }

    private void Space() {
        if (Value.Length < MaxValueLength) {
            Value += " ";
            wiggler.Start();
            Audio.Play(SFX.ui_main_rename_entry_char);
        }
        else {
            Audio.Play(SFX.ui_main_button_invalid);
        }
    }

    private void Backspace() {
        if (Value.Length > 0) {
            Value = Value.Substring(0, Value.Length - 1);
            Audio.Play(SFX.ui_main_rename_entry_backspace);
        }
        else {
            Audio.Play(SFX.ui_main_button_invalid);
        }
    }

    private void Finish() {
        if (Value.Length >= MinValueLength) {
            Focused = false;
            OnExit?.Invoke(true);
            OnExit = null;
            Audio.Play(SFX.ui_main_rename_entry_accept);
        }
        else {
            Audio.Play(SFX.ui_main_button_invalid);
        }
    }

    private void Cancel() {
        Cancelled = true;
        Value = StartingValue;
        Focused = false;
        OnExit?.Invoke(false);
        OnExit = null;
        Audio.Play(SFX.ui_main_button_back);
    }

    public override void Render() {
        float fscale = OuiModOptionStringHexColor.fscale;
        int prevIndex = index;
        // Only "focus" if we're not using the keyboard for input
        if (UseKeyboardInput)
            index = -1;

        // TODO: Rewrite or study and document the following code.
        // It stems from OuiFileNaming.

        Draw.Rect(-10f, -10f, 1940f, 1100f, Color.Black * 0.8f * ease);

        Vector2 pos = boxtopleft + new Vector2(boxPadding, boxPadding);
        float posX = boxtopleft.X + boxWidth / 2f - fscale * ActiveFont.Measure("0123").X - widestLetter;
        pos.X = posX;
        int letterIndex = 0;
        foreach (string letter in letters) {
            for (int i = 0; i < letter.Length; i++) {
                bool selected = letterIndex == line && i == index && !selectingOptions;
                Vector2 scale = fscale * Vector2.One * (selected ? 1.2f : 1f);
                Vector2 posLetter = pos + new Vector2(widestLetter, lineHeight) / 2f;
                if (selected) {
                    posLetter += new Vector2(0f, wiggler.Value) * 8f * fscale;
                }
                DrawOptionText(letter[i].ToString(), posLetter, new Vector2(0.5f, 0.5f), scale, selected);
                pos.X += widestLetter;
            }
            pos.X = posX;
            pos.Y += lineHeight + lineSpacing;
            letterIndex++;
        }
        float wiggle = wiggler.Value * 8f;

        pos.Y = boxtopleft.Y + boxHeight - lineHeight - boxPadding;
        pos.X = boxtopleft.X + boxPadding;
        Draw.Rect(pos.X, pos.Y - boxPadding * 0.5f, boxWidth - boxPadding * 2f, 4f, Color.White);
        lineHeight /= fscale;
        DrawOptionText(cancel, pos + new Vector2(0f, lineHeight + ((selectingOptions && optionsIndex == 0) ? wiggle : 0f)), new Vector2(0f, 1f), Vector2.One * optionsScale, selectingOptions && optionsIndex == 0);
        pos.X = boxtopleft.X + boxWidth - backspaceWidth - widestLetter - spaceWidth - widestLetter - beginWidth - boxPadding;

        DrawOptionText(space, pos + new Vector2(0f, lineHeight + ((selectingOptions && optionsIndex == 1) ? wiggle : 0f)), new Vector2(0f, 1f), Vector2.One * optionsScale, selectingOptions && optionsIndex == 1, Value.Length == 0 || !Focused);
        pos.X += spaceWidth + widestLetter;

        DrawOptionText(backspace, pos + new Vector2(0f, lineHeight + ((selectingOptions && optionsIndex == 2) ? wiggle : 0f)), new Vector2(0f, 1f), Vector2.One * optionsScale, selectingOptions && optionsIndex == 2, Value.Length <= 0 || !Focused);
        pos.X += backspaceWidth + widestLetter;

        DrawOptionText(accept, pos + new Vector2(0f, lineHeight + ((selectingOptions && optionsIndex == 3) ? wiggle : 0f)), new Vector2(0f, 1f), Vector2.One * optionsScale, selectingOptions && optionsIndex == 3, Value.Length < 1 || !Focused);

        ActiveFont.DrawEdgeOutline(Value, Position + new Vector2(960f, 256f), new Vector2(0.5f, 0.5f), Vector2.One * 2f, Color.Gray, 4f, Color.DarkSlateBlue, 2f, Color.Black);
        lineHeight *= fscale;
        index = prevIndex;
    }

    private void DrawOptionText(string text, Vector2 at, Vector2 justify, Vector2 scale, bool selected, bool disabled = false) {
        // Only draw "interactively" if not using the keyboard for input
        if (UseKeyboardInput) {
            selected = false;
            disabled = true;
        }

        Color color = disabled ? disableColor : GetTextColor(selected);
        Color edgeColor = disabled ? Color.Lerp(disableColor, Color.Black, 0.7f) : Color.Gray;
        if (selected && pressedTimer > 0f) {
            ActiveFont.Draw(text, at + Vector2.UnitY, justify, scale, color);
        }
        else {
            ActiveFont.DrawEdgeOutline(text, at, justify, scale, color, 4f, edgeColor);
        }
    }

    private Color GetTextColor(bool selected) {
        if (selected)
            return (Calc.BetweenInterval(timer, 0.1f) ? selectColorA : selectColorB);
        return unselectColor;
    }

}