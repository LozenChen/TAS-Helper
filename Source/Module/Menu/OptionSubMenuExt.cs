using Celeste.Mod.Core;
using Celeste.Mod.SpeedrunTool.Utils;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using CMCore = Celeste.Mod.Core;
namespace Celeste.Mod.TASHelper.Module.Menu;

// basically copied from Everest

public class OptionSubMenuExt : TextMenu.Item {
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

    public bool ThisPageEnterable {
        get {
            foreach (TextMenu.Item item in CurrentMenu) {
                if (item.Hoverable) {
                    return true;
                }
            }
            return false;
        }
    }

    public List<Tuple<string, List<TextMenu.Item>>> Menus { get; private set; }

    public List<TextMenu.Item> CurrentMenu {
        get {
            if (Menus.Count <= 0) {
#pragma warning disable CS8603
                return null;
#pragma warning restore CS8603
            }

            return Menus[MenuIndex].Item2;
        }
    }

    public TextMenu.Item Current {
        get {
            if (CurrentMenu.Count <= 0 || Selection < 0) {
#pragma warning disable CS8603
                return null;
#pragma warning restore CS8603
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
            float min = Engine.Height - 150 - Container.Height * Container.Justify.Y;
            float max = 150f + Container.Height * Container.Justify.Y;
            return Calc.Clamp(Engine.Height / 2 + Container.Height * Container.Justify.Y - GetYOffsetOf(Current), min, max);
        }
    }

    public float TitleHeight { get; private set; }

    public float MenuHeight { get; private set; }

    public OptionSubMenuExt(string label) {
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

    public OptionSubMenuExt Add(string label, List<TextMenu.Item> items) {
        if (Container != null) {
            if (items != null) {
                foreach (TextMenu.Item item in items) {
                    item.Container = Container;
                    Container.Add(item.ValueWiggler = Wiggler.Create(0.25f, 3f));
                    Container.Add(item.SelectWiggler = Wiggler.Create(0.25f, 3f));
                    item.ValueWiggler.UseRawDeltaTime = item.SelectWiggler.UseRawDeltaTime = true;
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

    public OptionSubMenuExt SetInitialSelection(int index) {
        InitialSelection = index;
        return this;
    }

    public void Clear() {
        Menus = new List<Tuple<string, List<TextMenu.Item>>>();
    }

    public void FirstSelection() {
        Selection = -1;
        if (ThisPageEnterable) {
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
                Audio.Play(direction > 0 ? "event:/ui/main/rollover_down" : "event:/ui/main/rollover_up");
                Current.SelectWiggler.Start();
            }
        }
    }

    public void RecalculateSize() {
        TitleHeight = ActiveFont.LineHeight;
        LeftColumnWidth = RightColumnWidth = _MenuHeight = 0f;
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

    public OptionSubMenuExt Change(Action<int> onValueChange) {
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
        if (ThisPageEnterable) {
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
        foreach (string item in Menus.Select((tuple) => tuple.Item1)) {
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

    public void OnPageDown() {
        int selection = Selection;
        float yOffsetOf = GetYOffsetOf(Current);
        while (GetYOffsetOf(Current) < yOffsetOf + 1080f && Selection < LastPossibleSelection) {
            MoveSelection(1);
        }
        if (selection != Selection) { Audio.Play("event:/ui/main/rollover_down"); }
    }

    public void OnPageUp() {
        int selection = Selection;
        float yOffsetOf = GetYOffsetOf(Current);
        while (GetYOffsetOf(Current) > yOffsetOf - 1080f && Selection > FirstPossibleSelection) {
            MoveSelection(-1);
        }
        if (selection != Selection) { Audio.Play("event:/ui/main/rollover_up"); }
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
                else if (CMCore.CoreModule.Settings.MenuPageDown.Pressed && Selection != LastPossibleSelection) {
                    OnPageDown();
                }
                else if (CMCore.CoreModule.Settings.MenuPageUp.Pressed && Selection != FirstPossibleSelection) {
                    OnPageUp();
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
                    MenuIndex = 0;
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
            if (Input.MenuCancel.Pressed && MenuIndex != 0) {
                MenuIndex = 0;
                Audio.Play("event:/ui/main/button_back");
                Container.Focused = true;
                return;
            }
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
        Color color = Disabled ? Color.DarkSlateGray : (highlighted ? Container.HighlightColor : Color.White) * alpha;
        Color strokeColor = Color.Black * (alpha * alpha * alpha);
        bool flag = Container.InnerContent == TextMenu.InnerContentMode.TwoColumn && !AlwaysCenter;
        Vector2 vector2 = vector + Vector2.UnitY * TitleHeight / 2f + (flag ? Vector2.Zero : new Vector2(Container.Width * 0.5f, 0f));
        Vector2 justify = flag ? new Vector2(0f, 0.5f) : new Vector2(0.5f, 0.5f);
        Vector2 justify2 = flag ? new Vector2(ActiveFont.Measure(Label).X + Icon.Width, 5f) : new Vector2(ActiveFont.Measure(Label).X / 2f + Icon.Width, 5f);
        MTexture icon = Icon;
        Color obj;
        if (!Disabled) {
            List<TextMenu.Item> currentMenu = CurrentMenu;
            if (currentMenu == null || currentMenu.Count >= 1) {
                obj = Focused ? Container.HighlightColor : Color.White;
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
            ActiveFont.DrawOutline(Menus[MenuIndex].Item1, vector2 + new Vector2(Container.Width - num * 0.5f + lastDir * ValueWiggler.Value * 8f, 0f), new Vector2(0.5f, 0.5f), Vector2.One * 0.8f, color, 2f, strokeColor);
            Vector2 vector3 = Vector2.UnitX * (highlighted ? (float)Math.Sin(sine * 4f) * 4f : 0f);
            Color color2 = MenuIndex > 0 ? color : Color.DarkSlateGray * alpha;
            Vector2 position2 = vector2 + new Vector2(Container.Width - num + 40f + (lastDir < 0 ? (0f - ValueWiggler.Value) * 8f : 0f), 0f) - (MenuIndex > 0 ? vector3 : Vector2.Zero);
            ActiveFont.DrawOutline("<", position2, new Vector2(0.5f, 0.5f), Vector2.One, color2, 2f, strokeColor);
            color2 = MenuIndex < Menus.Count - 1 ? color : Color.DarkSlateGray * alpha;
            position2 = vector2 + new Vector2(Container.Width - 40f + (lastDir > 0 ? ValueWiggler.Value * 8f : 0f), 0f) + (MenuIndex < Menus.Count - 1 ? vector3 : Vector2.Zero);
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
                if (position3.Y - y < MenuHeight && position3.Y + num2 * 0.5f > 0f && position3.Y - num2 * 0.5f < Engine.Height) {
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

    [Initialize]
    private static void InitializeHook() {
        typeof(TextMenu).GetMethod("Update").ILHook((cursor, _) => {
            if (cursor.TryGotoNext(ins => ins.OpCode == OpCodes.Call, ins => ins.MatchCallvirt(typeof(CoreModuleSettings), "get_MenuPageDown"), ins => true, ins => ins.OpCode == OpCodes.Brfalse)) {
                ILLabel target = (ILLabel)cursor.Next.Next.Next.Next.Operand;
                cursor.MoveAfterLabels();
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate(OnMenuTryPageDown);
                cursor.Emit(OpCodes.Brtrue, target);
            }
        });
    }

    private static bool OnMenuTryPageDown(TextMenu menu) {
        if (CMCore.CoreModule.Settings.MenuPageDown.Pressed && menu.Current is OptionSubMenuExt submenu && !submenu.Focused && submenu.ThisPageEnterable) {
            submenu.ConfirmPressed();
            if (submenu.OnPressed != null) {
                submenu.OnPressed();
            }
            submenu.OnPageDown();
            return true;
        }
        return false;
    }
}