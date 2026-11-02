using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace F1RaceSimulator.WinForms;

/// <summary>Яку тему обрав користувач.</summary>
public enum ThemeMode
{
    /// <summary>Як у налаштуваннях Windows (світлий або темний режим застосунків).</summary>
    System,
    Light,
    Dark
}

/// <summary>
/// Перемикання теми оформлення. Зберігає вибір користувача між запусками,
/// застосовує палітру до всіх елементів вікна і повідомляє про зміну подією
/// <see cref="Changed"/> (патерн Observer): вікна, підписані через <see cref="Attach"/>,
/// перефарбовуються автоматично, а панелі, що малюють себе самі (траса, телеметрія),
/// беруть кольори з <see cref="Current"/> під час кожного малювання.
/// </summary>
public static class ThemeManager
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "F1RaceSimulator", "theme.txt");

    private static readonly HashSet<TabControl> OwnerDrawnTabs = new();
    private static bool _listeningToSystem;

    /// <summary>Режим, обраний користувачем.</summary>
    public static ThemeMode Mode { get; private set; } = ThemeMode.System;

    /// <summary>Палітра, що діє зараз.</summary>
    public static Theme Current { get; private set; } = Theme.Light;

    /// <summary>Виникає після зміни теми.</summary>
    public static event EventHandler? Changed;

    /// <summary>Читає збережений вибір (за замовчуванням — як у Windows).</summary>
    public static void Load()
    {
        try
        {
            if (File.Exists(SettingsPath) &&
                Enum.TryParse(File.ReadAllText(SettingsPath).Trim(), ignoreCase: true, out ThemeMode saved))
                Mode = saved;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Не вдалося прочитати — залишаємо тему за замовчуванням.
        }
        Current = Resolve(Mode);
        ListenToSystem();
    }

    /// <summary>Встановлює режим, зберігає його і перефарбовує всі вікна.</summary>
    public static void SetMode(ThemeMode mode)
    {
        Mode = mode;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            File.WriteAllText(SettingsPath, mode.ToString());
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Тема діятиме до кінця сеансу, навіть якщо не збереглася.
        }
        Refresh();
    }

    /// <summary>Перемикає світлу ↔ темну тему.</summary>
    public static void Toggle() => SetMode(Current.IsDark ? ThemeMode.Light : ThemeMode.Dark);

    /// <summary>Чи увімкнено в Windows темний режим для застосунків.</summary>
    public static bool SystemPrefersDark()
    {
        if (Environment.OSVersion.Platform != PlatformID.Win32NT) return false;
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("AppsUseLightTheme") is int value && value == 0;
        }
        catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException or IOException)
        {
            return false;
        }
    }

    private static Theme Resolve(ThemeMode mode) => mode switch
    {
        ThemeMode.Dark => Theme.Dark,
        ThemeMode.Light => Theme.Light,
        _ => SystemPrefersDark() ? Theme.Dark : Theme.Light
    };

    private static void Refresh()
    {
        Current = Resolve(Mode);
        Changed?.Invoke(null, EventArgs.Empty);
    }

    /// <summary>У режимі «як у Windows» тема змінюється разом із системною.</summary>
    private static void ListenToSystem()
    {
        if (_listeningToSystem) return;
        _listeningToSystem = true;
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
        Application.ApplicationExit += (_, _) => SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
    }

    private static void OnUserPreferenceChanged(object? sender, UserPreferenceChangedEventArgs e)
    {
        if (Mode != ThemeMode.System || Resolve(ThemeMode.System) == Current) return;
        Form? form = Application.OpenForms.Count > 0 ? Application.OpenForms[0] : null;
        if (form is { IsHandleCreated: true, InvokeRequired: true })
            form.BeginInvoke(new Action(Refresh));
        else
            Refresh();
    }

    // ------------------------------------------------------------------
    // Застосування палітри до елементів керування
    // ------------------------------------------------------------------

    /// <summary>Фарбує вікно зараз і щоразу, коли тема змінюється, поки вікно відкрите.</summary>
    public static void Attach(Form form)
    {
        Apply(form);
        EventHandler handler = (_, _) => Apply(form);
        Changed += handler;
        form.FormClosed += (_, _) => Changed -= handler;
        form.HandleCreated += (_, _) => SetTitleBar(form, Current.IsDark);
    }

    /// <summary>Застосовує поточну палітру до елемента і всіх вкладених елементів.</summary>
    public static void Apply(Control root)
    {
        Theme t = Current;
        root.SuspendLayout();
        ApplyRecursive(root, t);
        root.ResumeLayout();
        if (root is Form form) SetTitleBar(form, t.IsDark);
        root.Invalidate(true);
    }

    private static void ApplyRecursive(Control control, Theme t)
    {
        ApplyTo(control, t);
        foreach (Control child in control.Controls)
            ApplyRecursive(child, t);
    }

    private static void ApplyTo(Control c, Theme t)
    {
        switch (c)
        {
            case Form f:
                f.BackColor = t.Window;
                f.ForeColor = t.Text;
                break;
            case ToolStrip strip:                       // MenuStrip і StatusStrip теж є ToolStrip
                ApplyToolStrip(strip, t);
                break;
            case DataGridView grid:
                ApplyGrid(grid, t);
                break;
            case TabControl tabs:
                ApplyTabs(tabs);
                break;
            case TabPage page:
                page.UseVisualStyleBackColor = false;
                page.BackColor = t.Panel;
                page.ForeColor = t.Text;
                break;
            case ListBox list:
                list.BackColor = t.Panel;
                list.ForeColor = t.Text;
                break;
            case Button button:
                ApplyButton(button, t);
                break;
            case GroupBox group:
                group.BackColor = t.Window;
                group.ForeColor = t.Text;
                break;
            case Label label:
                label.ForeColor = Equals(label.Tag, "muted") ? t.Muted : t.Text;
                break;
            case TrackPanel or TelemetryPanel:
                c.BackColor = t.Panel;
                c.ForeColor = t.Text;
                break;
            case ComboBox or TextBoxBase:
                c.BackColor = t.Panel;
                c.ForeColor = t.Text;
                break;
            case SplitContainer split:
                split.BackColor = t.Line;               // колір роздільника
                split.Panel1.BackColor = t.Window;
                split.Panel2.BackColor = t.Window;
                break;
            case Panel or TableLayoutPanel:
                if (c is not SplitterPanel) c.BackColor = t.Window;
                break;
        }
    }

    /// <summary>Оформлення таблиці: фон, сітка, заголовки, виділення.</summary>
    public static void ApplyGrid(DataGridView grid, Theme? theme = null)
    {
        Theme t = theme ?? Current;
        grid.EnableHeadersVisualStyles = false;
        grid.BackgroundColor = t.Panel;
        grid.GridColor = t.Line;
        grid.DefaultCellStyle.BackColor = t.Panel;
        grid.DefaultCellStyle.ForeColor = t.Text;
        grid.DefaultCellStyle.SelectionBackColor = t.Selection;
        grid.DefaultCellStyle.SelectionForeColor = t.Text;
        grid.ColumnHeadersDefaultCellStyle.BackColor = t.Chrome;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = t.Muted;
        grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = t.Chrome;
        grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = t.Muted;
        grid.RowHeadersDefaultCellStyle.BackColor = t.Chrome;
        grid.RowHeadersDefaultCellStyle.ForeColor = t.Muted;
        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
    }

    private static void ApplyButton(Button button, Theme t)
    {
        if (t.IsDark)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.UseVisualStyleBackColor = false;
            button.BackColor = t.Chrome;
            button.ForeColor = t.Text;
            button.FlatAppearance.BorderColor = t.Line;
            button.FlatAppearance.MouseOverBackColor = t.Selection;
            button.FlatAppearance.MouseDownBackColor = t.Line;
        }
        else
        {
            button.FlatStyle = FlatStyle.Standard;
            button.BackColor = SystemColors.Control;
            button.ForeColor = SystemColors.ControlText;
            button.UseVisualStyleBackColor = true;
        }
    }

    private static void ApplyToolStrip(ToolStrip strip, Theme t)
    {
        strip.Renderer = new ThemedToolStripRenderer(t);
        strip.BackColor = t.Chrome;
        strip.ForeColor = t.Text;
        foreach (ToolStripItem item in strip.Items)
            ApplyItem(item, t);
    }

    private static void ApplyItem(ToolStripItem item, Theme t)
    {
        // Позначка «accent» у Tag — для підписів, які мають бути кольоровими (таймер сесії).
        item.ForeColor = Equals(item.Tag, "accent") ? t.Accent : t.Text;
        switch (item)
        {
            case ToolStripComboBox combo:
                combo.FlatStyle = t.IsDark ? FlatStyle.Flat : FlatStyle.Standard;
                combo.ComboBox.BackColor = t.Panel;
                combo.ComboBox.ForeColor = t.Text;
                break;
            case ToolStripDropDownItem dropDownItem:
                dropDownItem.DropDown.BackColor = t.Chrome;
                dropDownItem.DropDown.ForeColor = t.Text;
                foreach (ToolStripItem child in dropDownItem.DropDownItems)
                    ApplyItem(child, t);
                break;
        }
    }

    /// <summary>
    /// Стандартні заголовки вкладок не можна перефарбувати, тому вони малюються вручну
    /// (режим <see cref="TabDrawMode.OwnerDrawFixed"/>).
    /// </summary>
    private static void ApplyTabs(TabControl tabs)
    {
        if (OwnerDrawnTabs.Add(tabs))
        {
            tabs.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabs.DrawItem += DrawTab;
            tabs.Disposed += (_, _) => OwnerDrawnTabs.Remove(tabs);
        }
        tabs.Invalidate();
    }

    private static void DrawTab(object? sender, DrawItemEventArgs e)
    {
        if (sender is not TabControl tabs || e.Index < 0 || e.Index >= tabs.TabCount) return;
        Theme t = Current;
        bool selected = e.Index == tabs.SelectedIndex;

        // Смуга праворуч від останньої вкладки — кольором вікна.
        if (e.Index == tabs.TabCount - 1)
        {
            Rectangle last = tabs.GetTabRect(e.Index);
            using var stripBrush = new SolidBrush(tabs.Parent?.BackColor ?? t.Window);
            e.Graphics.FillRectangle(stripBrush, new Rectangle(last.Right, 0, Math.Max(0, tabs.Width - last.Right), last.Bottom + 1));
        }

        using (var back = new SolidBrush(selected ? t.Panel : t.Chrome))
            e.Graphics.FillRectangle(back, e.Bounds);
        if (selected)
        {
            using var accent = new Pen(t.Accent, 2f);
            e.Graphics.DrawLine(accent, e.Bounds.Left + 2, e.Bounds.Top + 1, e.Bounds.Right - 3, e.Bounds.Top + 1);
        }
        TextRenderer.DrawText(e.Graphics, tabs.TabPages[e.Index].Text, tabs.Font, e.Bounds,
            selected ? t.Text : t.Muted,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
    }

    // ------------------------------------------------------------------
    // Темний заголовок вікна (Windows 10 1809+ і Windows 11)
    // ------------------------------------------------------------------

    private const int DwmUseImmersiveDarkMode = 20;
    private const int DwmUseImmersiveDarkModeOld = 19;
    private const uint SwpFrameChanged = 0x0020, SwpNoMove = 0x0002, SwpNoSize = 0x0001, SwpNoZOrder = 0x0004, SwpNoActivate = 0x0010;

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hwnd, IntPtr insertAfter, int x, int y, int cx, int cy, uint flags);

    private static void SetTitleBar(Form form, bool dark)
    {
        if (!form.IsHandleCreated || Environment.OSVersion.Platform != PlatformID.Win32NT ||
            Environment.OSVersion.Version < new Version(10, 0, 17763))
            return;
        try
        {
            int value = dark ? 1 : 0;
            if (DwmSetWindowAttribute(form.Handle, DwmUseImmersiveDarkMode, ref value, sizeof(int)) != 0)
                DwmSetWindowAttribute(form.Handle, DwmUseImmersiveDarkModeOld, ref value, sizeof(int));
            // Змушує Windows одразу перемалювати рамку вікна.
            SetWindowPos(form.Handle, IntPtr.Zero, 0, 0, 0, 0, SwpFrameChanged | SwpNoMove | SwpNoSize | SwpNoZOrder | SwpNoActivate);
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
        {
            // Стара Windows або інша ОС — заголовок залишається системним.
        }
    }
}

/// <summary>
/// Малює меню, панель інструментів і рядок стану кольорами поточної теми
/// (через <see cref="ThemeColorTable"/>), а позначку вибраного пункту меню — вручну,
/// щоб вона була видна і на темному фоні.
/// </summary>
internal sealed class ThemedToolStripRenderer : ToolStripProfessionalRenderer
{
    private readonly Theme _theme;

    public ThemedToolStripRenderer(Theme theme) : base(new ThemeColorTable(theme))
    {
        _theme = theme;
        RoundedEdges = false;
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        if (!e.Item.Enabled) e.TextColor = Color.FromArgb(150, _theme.Muted);
        else if (e.Item is ToolStripMenuItem) e.TextColor = _theme.Text;
        base.OnRenderItemText(e);
    }

    protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
    {
        e.ArrowColor = _theme.Text;
        base.OnRenderArrow(e);
    }

    protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
    {
        Rectangle r = e.ImageRectangle;
        if (r.Width <= 0 || r.Height <= 0) return;
        r.Inflate(-1, -1);
        using (var back = new SolidBrush(_theme.Selection))
            e.Graphics.FillRectangle(back, r);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var pen = new Pen(_theme.Text, 2f);
        e.Graphics.DrawLines(pen, new[]
        {
            new PointF(r.Left + r.Width * 0.24f, r.Top + r.Height * 0.52f),
            new PointF(r.Left + r.Width * 0.43f, r.Top + r.Height * 0.72f),
            new PointF(r.Left + r.Width * 0.78f, r.Top + r.Height * 0.30f)
        });
    }
}

/// <summary>Кольори смуг меню і панелей інструментів для <see cref="ToolStripProfessionalRenderer"/>.</summary>
internal sealed class ThemeColorTable : ProfessionalColorTable
{
    private readonly Theme _t;

    public ThemeColorTable(Theme theme)
    {
        _t = theme;
        UseSystemColors = false;
    }

    public override Color MenuStripGradientBegin => _t.Chrome;
    public override Color MenuStripGradientEnd => _t.Chrome;
    public override Color ToolStripGradientBegin => _t.Chrome;
    public override Color ToolStripGradientMiddle => _t.Chrome;
    public override Color ToolStripGradientEnd => _t.Chrome;
    public override Color ToolStripBorder => _t.Line;
    public override Color StatusStripGradientBegin => _t.Chrome;
    public override Color StatusStripGradientEnd => _t.Chrome;
    public override Color ToolStripDropDownBackground => _t.Chrome;
    public override Color ImageMarginGradientBegin => _t.Chrome;
    public override Color ImageMarginGradientMiddle => _t.Chrome;
    public override Color ImageMarginGradientEnd => _t.Chrome;
    public override Color MenuBorder => _t.Line;
    public override Color MenuItemBorder => _t.Accent;
    public override Color MenuItemSelected => _t.Selection;
    public override Color MenuItemSelectedGradientBegin => _t.Selection;
    public override Color MenuItemSelectedGradientEnd => _t.Selection;
    public override Color MenuItemPressedGradientBegin => _t.Panel;
    public override Color MenuItemPressedGradientMiddle => _t.Panel;
    public override Color MenuItemPressedGradientEnd => _t.Panel;
    public override Color ButtonSelectedHighlight => _t.Selection;
    public override Color ButtonSelectedBorder => _t.Accent;
    public override Color ButtonSelectedGradientBegin => _t.Selection;
    public override Color ButtonSelectedGradientMiddle => _t.Selection;
    public override Color ButtonSelectedGradientEnd => _t.Selection;
    public override Color ButtonPressedGradientBegin => _t.Line;
    public override Color ButtonPressedGradientMiddle => _t.Line;
    public override Color ButtonPressedGradientEnd => _t.Line;
    public override Color ButtonCheckedGradientBegin => _t.Selection;
    public override Color ButtonCheckedGradientMiddle => _t.Selection;
    public override Color ButtonCheckedGradientEnd => _t.Selection;
    public override Color CheckBackground => _t.Selection;
    public override Color CheckSelectedBackground => _t.Selection;
    public override Color CheckPressedBackground => _t.Selection;
    public override Color SeparatorDark => _t.Line;
    public override Color SeparatorLight => _t.Chrome;
    public override Color GripDark => _t.Line;
    public override Color GripLight => _t.Chrome;
}
