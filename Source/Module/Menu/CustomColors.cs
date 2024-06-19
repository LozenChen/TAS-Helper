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

namespace Celeste.Mod.TASHelper.Module.Menu;

public static class CustomColors {
    public static readonly Color defaultLoadRangeColliderColor = Color.Lime;
    public static readonly Color defaultInViewRangeColor = Color.Yellow * 0.8f;
    public static readonly Color defaultNearPlayerRangeColor = Color.Lime * 0.8f;
    public static readonly Color defaultCameraTargetColor = Color.Goldenrod;
    public static readonly Color defaultNotInViewColor = new Color(0.2f, 1f, 0f);
    public static readonly Color defaultNeverActivateColor = new Color(0.25f, 1f, 1f);
    public static readonly Color defaultActivateEveryFrameColor = new Color(0.8f, 0f, 0f);
    public static readonly Color defaultPredictorEndpointColor = Color.MediumPurple * 0.8f;
    public static readonly Color defaultPredictorFinestScaleColor = Color.Red * 0.3f;
    public static readonly Color defaultPredictorFineScaleColor = Color.Gold * 0.5f;
    public static readonly Color defaultPredictorCoarseScaleColor = Color.Green * 0.7f;
    public static readonly Color defaultPredictorKeyframeColor = Color.White * 0.9f;
    public static readonly Color defaultPredictorPolygonalLineColor = Color.Red;
    public static readonly Color defaultPredictorDotColor = Color.LightBlue;
    public static readonly Color defaultCameraTriggerColor = Color.DarkGoldenrod;
    public static readonly Color defaultMOAColor = Color.DarkOrange;

    public static void ResetOtherColor() {
        LoadRangeColliderColor = defaultLoadRangeColliderColor;
        InViewRangeColor = defaultInViewRangeColor;
        NearPlayerRangeColor = defaultNearPlayerRangeColor;
        CameraTargetColor = defaultCameraTargetColor;
        CameraTriggerColor = defaultCameraTriggerColor;
        MovementOvershootAssistantColor = defaultMOAColor;
    }

    public static void ResetSpinnerColor() {
        SpinnerColor_NotInView = defaultNotInViewColor;
        SpinnerColor_NeverActivate = defaultNeverActivateColor;
        SpinnerColor_ActivateEveryFrame = defaultActivateEveryFrameColor;
        SpinnerColor_TasModEntityHitboxColor = DefaultEntityColor;
        SpinnerColor_TasModCycleHitboxColor1 = CycleHitboxColor.DefaultColor1;
        SpinnerColor_TasModCycleHitboxColor2 = CycleHitboxColor.DefaultColor2;
        SpinnerColor_TasModCycleHitboxColor3 = CycleHitboxColor.DefaultColor3;
        SpinnerColor_TasModOtherCyclesHitboxColor = CycleHitboxColor.DefaultOthersColor;
    }

    public static void ResetPredictorColor() {
        Predictor_CoarseScaleColor = defaultPredictorCoarseScaleColor;
        Predictor_EndpointColor = defaultPredictorEndpointColor;
        Predictor_FineScaleColor = defaultPredictorFineScaleColor;
        Predictor_FinestScaleColor = defaultPredictorFinestScaleColor;
        Predictor_KeyframeColor = defaultPredictorKeyframeColor;
        Predictor_PolygonalLineColor = defaultPredictorPolygonalLineColor;
        Predictor_DotColor = defaultPredictorDotColor;
    }


    // whenever change these names, update Dialog

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
    public static Color Predictor_EndpointColor { get => TasHelperSettings.PredictorEndpointColor; set => TasHelperSettings.PredictorEndpointColor = value; }

    public static Color Predictor_FinestScaleColor { get => TasHelperSettings.PredictorFinestScaleColor; set => TasHelperSettings.PredictorFinestScaleColor = value; }

    public static Color Predictor_FineScaleColor { get => TasHelperSettings.PredictorFineScaleColor; set => TasHelperSettings.PredictorFineScaleColor = value; }

    public static Color Predictor_CoarseScaleColor { get => TasHelperSettings.PredictorCoarseScaleColor; set => TasHelperSettings.PredictorCoarseScaleColor = value; }

    public static Color Predictor_KeyframeColor { get => TasHelperSettings.PredictorKeyframeColor; set => TasHelperSettings.PredictorKeyframeColor = value; }

    public static Color Predictor_PolygonalLineColor { get => TasHelperSettings.PredictorPolygonalLineColor; set => TasHelperSettings.PredictorPolygonalLineColor = value; }

    public static Color Predictor_DotColor { get => TasHelperSettings.PredictorDotColor; set => TasHelperSettings.PredictorDotColor = value; }

    public static Color CameraTriggerColor { get => TasHelperSettings.CameraTriggerColor; set => TasHelperSettings.CameraTriggerColor = value; }

    public static Color MovementOvershootAssistantColor { get => TasHelperSettings.MOAColor; set => TasHelperSettings.MOAColor = value; }

    public static TextMenu.Item CreateChangeColorItem(Func<Color> getter, Action<Color> setter, string name, TextMenu textMenu, bool inGame) {
        TextMenu.Item item = new ButtonColorExt(name.ToDialogText(), getter, inGame).Pressed(inGame ? () => { }
        :
            () => {
                Audio.Play("event:/ui/main/savefile_rename_start");
                textMenu.SceneAs<Overworld>().Goto<OuiModOptionStringHexColor>()
                    .Init<OuiModOptions>(ColorToHex(getter()),
                        value => setter(HexToColor(value, getter())), 9);
            });
        return item;
    }

    public static void AddItemWithDescription(TextMenu menu, List<TextMenu.Item> page, bool inGame, Func<Color> getter, Action<Color> setter, string name, string cmd = "", string description = "") {
        TextMenu.Item item = CreateChangeColorItem(getter, setter, name, menu, inGame);
        page.Add(item);
        if (!string.IsNullOrEmpty(description)) {
            page.AddDescriptionOnEnter(menu, item, description);
        }
        if (!string.IsNullOrEmpty(cmd) && inGame) {
            SubHeaderExt cmdText = new(cmd) {
                TextColor = Color.Gray,
                HeightExtra = 0f
            };
            page.Add(cmdText);
        }
    }

    public static void AddItemWithDescriptionAndCMD(TextMenu menu, List<TextMenu.Item> page, bool inGame, Func<Color> getter, Action<Color> setter, string name, Color defaultColor) {
        string cmd = $"Console command: tashelper_custom_color, {name}, {ColorToHex(defaultColor).Remove(0, 1)}";
        AddItemWithDescription(menu, page, inGame, getter, setter, name, cmd);
    }

    internal static List<TextMenu.Item> Create_PageSpinnerColor(TextMenu menu, bool inGame) {
        List<TextMenu.Item> page = new();
        if (inGame) {
            SubHeaderExt remindText = new("Color Customization Remind".ToDialogText()) {
                TextColor = Color.Gray,
                HeightExtra = 0f
            };
            page.Add(remindText);
        }
        SubHeaderExt formatText = new("Color Customization Color Format".ToDialogText()) {
            TextColor = Color.Gray,
            HeightExtra = 0f
        };
        page.Add(formatText);
        AddItemWithDescriptionAndCMD(menu, page, inGame, () => SpinnerColor_NotInView, value => SpinnerColor_NotInView = value, nameof(SpinnerColor_NotInView), defaultNotInViewColor);
        AddItemWithDescriptionAndCMD(menu, page, inGame, () => SpinnerColor_NeverActivate, value => SpinnerColor_NeverActivate = value, nameof(SpinnerColor_NeverActivate), defaultNeverActivateColor);
        AddItemWithDescriptionAndCMD(menu, page, inGame, () => SpinnerColor_ActivateEveryFrame, value => SpinnerColor_ActivateEveryFrame = value, nameof(SpinnerColor_ActivateEveryFrame), defaultActivateEveryFrameColor);
        AddItemWithDescriptionAndCMD(menu, page, inGame, () => SpinnerColor_TasModEntityHitboxColor, value => SpinnerColor_TasModEntityHitboxColor = value, nameof(SpinnerColor_TasModEntityHitboxColor), DefaultEntityColor);
        AddItemWithDescriptionAndCMD(menu, page, inGame, () => SpinnerColor_TasModCycleHitboxColor1, value => SpinnerColor_TasModCycleHitboxColor1 = value, nameof(SpinnerColor_TasModCycleHitboxColor1), CycleHitboxColor.DefaultColor1);
        AddItemWithDescriptionAndCMD(menu, page, inGame, () => SpinnerColor_TasModCycleHitboxColor2, value => SpinnerColor_TasModCycleHitboxColor2 = value, nameof(SpinnerColor_TasModCycleHitboxColor2), CycleHitboxColor.DefaultColor2);
        AddItemWithDescriptionAndCMD(menu, page, inGame, () => SpinnerColor_TasModCycleHitboxColor3, value => SpinnerColor_TasModCycleHitboxColor3 = value, nameof(SpinnerColor_TasModCycleHitboxColor3), CycleHitboxColor.DefaultColor3);
        AddItemWithDescriptionAndCMD(menu, page, inGame, () => SpinnerColor_TasModOtherCyclesHitboxColor, value => SpinnerColor_TasModOtherCyclesHitboxColor = value, nameof(SpinnerColor_TasModOtherCyclesHitboxColor), CycleHitboxColor.DefaultOthersColor);
        SubHeaderExt descriptionText = new("Color Customization SpinnerColor Footnote".ToDialogText()) {
            TextColor = Color.Gray,
            HeightExtra = 0f
        };
        page.Add(descriptionText);
        page.Add(new HLine(Color.Gray));
        return page;

    }

    internal static List<TextMenu.Item> Create_PagePredictor(TextMenu menu, bool inGame) {
        List<TextMenu.Item> page = new();
        if (inGame) {
            SubHeaderExt remindText = new("Color Customization Remind".ToDialogText()) {
                TextColor = Color.Gray,
                HeightExtra = 0f
            };
            page.Add(remindText);
        }
        SubHeaderExt formatText = new("Color Customization Color Format".ToDialogText()) {
            TextColor = Color.Gray,
            HeightExtra = 0f
        };
        page.Add(formatText);
        AddItemWithDescriptionAndCMD(menu, page, inGame, () => Predictor_FinestScaleColor, value => Predictor_FinestScaleColor = value, nameof(Predictor_FinestScaleColor), defaultPredictorFinestScaleColor);
        AddItemWithDescriptionAndCMD(menu, page, inGame, () => Predictor_PolygonalLineColor, value => Predictor_PolygonalLineColor = value, nameof(Predictor_PolygonalLineColor), defaultPredictorPolygonalLineColor);
        AddItemWithDescriptionAndCMD(menu, page, inGame, () => Predictor_DotColor, value => Predictor_DotColor = value, nameof(Predictor_DotColor), defaultPredictorDotColor);
        AddItemWithDescriptionAndCMD(menu, page, inGame, () => Predictor_FineScaleColor, value => Predictor_FineScaleColor = value, nameof(Predictor_FineScaleColor), defaultPredictorFineScaleColor);
        AddItemWithDescriptionAndCMD(menu, page, inGame, () => Predictor_CoarseScaleColor, value => Predictor_CoarseScaleColor = value, nameof(Predictor_CoarseScaleColor), defaultPredictorCoarseScaleColor);
        AddItemWithDescriptionAndCMD(menu, page, inGame, () => Predictor_EndpointColor, value => Predictor_EndpointColor = value, nameof(Predictor_EndpointColor), defaultPredictorEndpointColor);
        AddItemWithDescriptionAndCMD(menu, page, inGame, () => Predictor_KeyframeColor, value => Predictor_KeyframeColor = value, nameof(Predictor_KeyframeColor), defaultPredictorKeyframeColor);
        page.Add(new HLine(Color.Gray));
        return page;

    }


    internal static List<TextMenu.Item> Create_PageOther(TextMenu menu, bool inGame) {
        List<TextMenu.Item> page = new();
        if (inGame) {
            SubHeaderExt remindText = new("Color Customization Remind".ToDialogText()) {
                TextColor = Color.Gray,
                HeightExtra = 0f
            };
            page.Add(remindText);
        }
        SubHeaderExt formatText = new("Color Customization Color Format".ToDialogText()) {
            TextColor = Color.Gray,
            HeightExtra = 0f
        };
        page.Add(formatText);
        AddItemWithDescriptionAndCMD(menu, page, inGame, () => InViewRangeColor, value => InViewRangeColor = value, nameof(InViewRangeColor), defaultInViewRangeColor);
        AddItemWithDescriptionAndCMD(menu, page, inGame, () => NearPlayerRangeColor, value => NearPlayerRangeColor = value, nameof(NearPlayerRangeColor), defaultNearPlayerRangeColor);
        AddItemWithDescriptionAndCMD(menu, page, inGame, () => LoadRangeColliderColor, value => LoadRangeColliderColor = value, nameof(LoadRangeColliderColor), defaultLoadRangeColliderColor);
        AddItemWithDescriptionAndCMD(menu, page, inGame, () => CameraTargetColor, value => CameraTargetColor = value, nameof(CameraTargetColor), defaultCameraTargetColor);
        AddItemWithDescriptionAndCMD(menu, page, inGame, () => CameraTriggerColor, value => CameraTriggerColor = value, nameof(CameraTriggerColor), defaultCameraTriggerColor);
        AddItemWithDescriptionAndCMD(menu, page, inGame, () => MovementOvershootAssistantColor, value => MovementOvershootAssistantColor = value, nameof(MovementOvershootAssistantColor), defaultMOAColor);

        page.Add(new HLine(Color.Gray));
        return page;
    }

    internal static List<TextMenu.Item> Create_PageOnOff(TextMenu menu, bool inGame) {
        List<TextMenu.Item> page = new List<TextMenu.Item>();
        page.Add(new TextMenu.OnOff("Show Cycle Hitbox Colors".ToDialogText(), TasHelperSettings.ShowCycleHitboxColors).Change(value => TasHelperSettings.ShowCycleHitboxColors = value));
        TextMenu.Item NotInViewColorItem = new EnumerableSliderExt<UsingNotInViewColorModes>("Using NotInView Color Modes".ToDialogText(), TASHelperMenu.CreateUsingNotInViewColorOptions(),
                    TasHelperSettings.UsingNotInViewColorMode).Change(value => TasHelperSettings.UsingNotInViewColorMode = value);
        page.Add(NotInViewColorItem);
        page.AddDescriptionOnEnter(menu, NotInViewColorItem, "Using NotInView Color Description".ToDialogText());
        TextMenu.Item UsingFreezeColorItem;
        page.Add(UsingFreezeColorItem = new TextMenu.OnOff("Using Freeze Color".ToDialogText(), TasHelperSettings.UsingFreezeColor).Change(value => TasHelperSettings.UsingFreezeColor = value));
        page.AddDescriptionOnEnter(menu, UsingFreezeColorItem, "Using Freeze Color Description".ToDialogText());
        page.Add(new TextMenu.OnOff("Using Camera Trigger Color".ToDialogText(), TasHelperSettings.EnableCameraTriggerColor).Change(value => TasHelperSettings.EnableCameraTriggerColor = value));
        TextMenu.Item resetButton = new TextMenu.Button("Reset Custom Color".ToDialogText()).Pressed(
            () => {
                Audio.Play("event:/ui/main/rename_entry_accept");
                ResetSpinnerColor();
                ResetOtherColor();
                ResetPredictorColor();
            }
        );
        page.Add(resetButton);
        page.Add(new HLine(Color.Gray));
        return page;
    }


    [Command("tashelper_custom_color", "Check TASHelper mod options menu for help.")]
    public static void CmdCustomColor(string field, string color) {
        switch (field) {
            case nameof(SpinnerColor_NotInView): {
                    SpinnerColor_NotInView = HexToColorWithLog(color, defaultNotInViewColor);
                    TASHelperModule.Instance.SaveSettings();
                    return;
                }

            case nameof(SpinnerColor_NeverActivate): {
                    SpinnerColor_NeverActivate = HexToColorWithLog(color, defaultNeverActivateColor);
                    TASHelperModule.Instance.SaveSettings();
                    return;
                }

            case nameof(SpinnerColor_ActivateEveryFrame): {
                    SpinnerColor_ActivateEveryFrame = HexToColorWithLog(color, defaultActivateEveryFrameColor);
                    TASHelperModule.Instance.SaveSettings();
                    return;
                }

            case nameof(SpinnerColor_TasModEntityHitboxColor): {
                    SpinnerColor_TasModEntityHitboxColor = HexToColorWithLog(color, DefaultEntityColor);
                    CelesteTasModule.Instance.SaveSettings();
                    return;
                }

            case nameof(SpinnerColor_TasModCycleHitboxColor1): {
                    SpinnerColor_TasModCycleHitboxColor1 = HexToColorWithLog(color, CycleHitboxColor.DefaultColor1);
                    CelesteTasModule.Instance.SaveSettings();
                    return;
                }

            case nameof(SpinnerColor_TasModCycleHitboxColor2): {
                    SpinnerColor_TasModCycleHitboxColor2 = HexToColorWithLog(color, CycleHitboxColor.DefaultColor2);
                    CelesteTasModule.Instance.SaveSettings();
                    return;
                }

            case nameof(SpinnerColor_TasModCycleHitboxColor3): {
                    SpinnerColor_TasModCycleHitboxColor3 = HexToColorWithLog(color, CycleHitboxColor.DefaultColor3);
                    CelesteTasModule.Instance.SaveSettings();
                    return;
                }

            case nameof(SpinnerColor_TasModOtherCyclesHitboxColor): {
                    SpinnerColor_TasModOtherCyclesHitboxColor = HexToColorWithLog(color, CycleHitboxColor.DefaultOthersColor);
                    CelesteTasModule.Instance.SaveSettings();
                    return;
                }

            case nameof(LoadRangeColliderColor): {
                    LoadRangeColliderColor = HexToColorWithLog(color, defaultLoadRangeColliderColor);
                    TASHelperModule.Instance.SaveSettings();
                    return;
                }

            case nameof(CameraTargetColor): {
                    CameraTargetColor = HexToColorWithLog(color, defaultCameraTargetColor);
                    TASHelperModule.Instance.SaveSettings();
                    return;
                }

            case nameof(NearPlayerRangeColor): {
                    NearPlayerRangeColor = HexToColorWithLog(color, defaultNearPlayerRangeColor);
                    TASHelperModule.Instance.SaveSettings();
                    return;
                }

            case nameof(InViewRangeColor): {
                    InViewRangeColor = HexToColorWithLog(color, defaultInViewRangeColor);
                    TASHelperModule.Instance.SaveSettings();
                    return;
                }

            case nameof(Predictor_KeyframeColor): {
                    Predictor_KeyframeColor = HexToColorWithLog(color, defaultPredictorKeyframeColor);
                    TASHelperModule.Instance.SaveSettings();
                    return;
                }

            case nameof(Predictor_FinestScaleColor): {
                    Predictor_FinestScaleColor = HexToColorWithLog(color, defaultPredictorFinestScaleColor);
                    TASHelperModule.Instance.SaveSettings();
                    return;
                }
            case nameof(Predictor_FineScaleColor): {
                    Predictor_FineScaleColor = HexToColorWithLog(color, defaultPredictorFineScaleColor);
                    TASHelperModule.Instance.SaveSettings();
                    return;
                }
            case nameof(Predictor_CoarseScaleColor): {
                    Predictor_CoarseScaleColor = HexToColorWithLog(color, defaultPredictorCoarseScaleColor);
                    TASHelperModule.Instance.SaveSettings();
                    return;
                }
            case nameof(Predictor_EndpointColor): {
                    Predictor_EndpointColor = HexToColorWithLog(color, defaultPredictorEndpointColor);
                    TASHelperModule.Instance.SaveSettings();
                    return;
                }
            case nameof(CameraTriggerColor): {
                    CameraTriggerColor = HexToColorWithLog(color, defaultCameraTriggerColor);
                    TASHelperModule.Instance.SaveSettings();
                    return;
                }
            case nameof(Predictor_PolygonalLineColor): {
                    Predictor_PolygonalLineColor = HexToColorWithLog(color, defaultPredictorPolygonalLineColor);
                    TASHelperModule.Instance.SaveSettings();
                    return;
                }
            case nameof(Predictor_DotColor): {
                    Predictor_DotColor = HexToColorWithLog(color, defaultPredictorDotColor);
                    TASHelperModule.Instance.SaveSettings();
                    return;
                }
            case nameof(MovementOvershootAssistantColor): {
                    MovementOvershootAssistantColor = HexToColorWithLog(color, defaultMOAColor);
                    TASHelperModule.Instance.SaveSettings();
                    return;
                }
            default: {
                    Engine.Commands.Log("Invalid field name");
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
            Engine.Commands.Log("Invaild Color");
            return defaultColor;
        }

        if (hex.Length > 8) {
            hex = hex.Substring(0, 8);
        }

        if (hex.Length == 3 || hex.Length == 4) {
            hex = (from c in hex.ToCharArray()
                   select $"{c}{c}").Aggregate((s, s1) => s + s1);
        }

        hex = hex.PadLeft(8, 'F');
        try {
            long num = Convert.ToInt64(hex, 16);
            Color result = default;
            result.A = (byte)(num >> 24);
            result.R = (byte)(num >> 16);
            result.G = (byte)(num >> 8);
            result.B = (byte)num;
            return result;
        }
        catch (FormatException) {
            Engine.Commands.Log("Invaild Color");
            return defaultColor;
        }
    }
}

public class ButtonColorExt : TextMenu.Button, IItemExt {

    public Func<Color> CubeColorGetter = () => Color.White;
    public Color TextColor { get; set; } = Color.White;

    public string name;
    public Color TextColorDisabled { get; set; } = Color.DarkSlateGray;

    public Color TextColorHighlightDisabled { get; set; } = Color.SlateGray;

    public string Icon { get; set; }

    public float? IconWidth { get; set; }

    public bool IconOutline { get; set; }

    public Vector2 Offset { get; set; }

    public float Alpha { get; set; } = 1f;


    public Vector2 Scale { get; set; } = Vector2.One;

    public bool InGame;

    public override float Height() {
        return base.Height() * Scale.Y;
    }

    public override float LeftWidth() {
        return base.LeftWidth() * Scale.X;
    }

#pragma warning disable CS8625
    public ButtonColorExt(string label, Func<Color> cubecolorGetter, bool inGame = false)
#pragma warning restore CS8625
        : base(label) {
        CubeColorGetter = cubecolorGetter;
        Icon = "";
        name = label;
        InGame = inGame;
    }

    public override void Render(Vector2 position, bool highlighted) {
        Label = name + $": {ColorToHex(CubeColorGetter())}";
        position += Offset;
        float num = Container.Alpha * Alpha;
        Color color = (InGame ? (highlighted ? TextColorHighlightDisabled : TextColorDisabled) : (highlighted ? Container.HighlightColor : TextColor)) * num;
        Color strokeColor = Color.Black * (num * num * num);
        bool flag = Container.InnerContent == TextMenu.InnerContentMode.TwoColumn && !AlwaysCenter;
        Vector2 textPosition = position + (flag ? Vector2.Zero : new Vector2(Container.Width * 0.5f, 0f));
        Vector2 justify = flag ? new Vector2(0f, 0.5f) : new Vector2(0.5f, 0.5f);
        float height = ActiveFont.Measure("I").Y / 2f;
        Vector2 cubePosition = textPosition + new Vector2(ActiveFont.Measure(Label).X + 30f, -height / 2f);
        Draw.Rect(cubePosition - new Vector2(4f, 4f), height + 8f, height + 8f, Color.Black);
        Draw.Rect(cubePosition, height, height, CubeColorGetter());
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
#pragma warning disable CS8600
        letterChars = null;
#pragma warning restore CS8600

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

        Engine.Commands.Enabled = Celeste.PlayMode == Celeste.PlayModes.Debug;

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
        Engine.Commands.Enabled = Celeste.PlayMode == Celeste.PlayModes.Debug;
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
        else if ((Input.MenuUp.Pressed || selectingOptions && Value.Length <= 0 && optionsIndex > 0) && (line > 0 || selectingOptions)) {
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
#pragma warning disable CS8625
            OnExit = null;
#pragma warning restore CS8625
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
#pragma warning disable CS8625
        OnExit = null;
#pragma warning restore CS8625
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
        DrawOptionText(cancel, pos + new Vector2(0f, lineHeight + (selectingOptions && optionsIndex == 0 ? wiggle : 0f)), new Vector2(0f, 1f), Vector2.One * optionsScale, selectingOptions && optionsIndex == 0);
        pos.X = boxtopleft.X + boxWidth - backspaceWidth - widestLetter - spaceWidth - widestLetter - beginWidth - boxPadding;

        DrawOptionText(space, pos + new Vector2(0f, lineHeight + (selectingOptions && optionsIndex == 1 ? wiggle : 0f)), new Vector2(0f, 1f), Vector2.One * optionsScale, selectingOptions && optionsIndex == 1, Value.Length == 0 || !Focused);
        pos.X += spaceWidth + widestLetter;

        DrawOptionText(backspace, pos + new Vector2(0f, lineHeight + (selectingOptions && optionsIndex == 2 ? wiggle : 0f)), new Vector2(0f, 1f), Vector2.One * optionsScale, selectingOptions && optionsIndex == 2, Value.Length <= 0 || !Focused);
        pos.X += backspaceWidth + widestLetter;

        DrawOptionText(accept, pos + new Vector2(0f, lineHeight + (selectingOptions && optionsIndex == 3 ? wiggle : 0f)), new Vector2(0f, 1f), Vector2.One * optionsScale, selectingOptions && optionsIndex == 3, Value.Length < 1 || !Focused);

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
            return Calc.BetweenInterval(timer, 0.1f) ? selectColorA : selectColorB;
        return unselectColor;
    }

}