using System.ComponentModel;
using System.Drawing.Drawing2D;
using F1RaceSimulator.Core.Components;
using F1RaceSimulator.Core.Simulation;
using F1RaceSimulator.Core.Tyres;

namespace F1RaceSimulator.WinForms;

/// <summary>
/// Телеметрія обраного боліда: швидкість, передача, оберти, газ і гальмо, час кола й секторів,
/// а в гонці ще стан і температура шин, температура та знос двигуна, паливо, заряд ERS і режим їзди.
/// Малюється вручну засобами GDI+, тому шкали можуть мати кольорові зони
/// (робоче вікно шин, перегрів двигуна).
/// </summary>
public sealed class TelemetryPanel : Control
{
    private static readonly Color NormalColor = Color.FromArgb(46, 139, 87);
    private static readonly Color WarningColor = Color.FromArgb(214, 137, 16);
    private static readonly Color CriticalColor = Color.FromArgb(192, 57, 43);
    private static readonly Color ColdColor = Color.FromArgb(52, 120, 200);
    private static readonly Color ScaleBackColor = Color.FromArgb(232, 236, 241);
    private static readonly Color WindowZoneColor = Color.FromArgb(198, 232, 206);
    private static readonly Color HotZoneColor = Color.FromArgb(250, 222, 186);
    private static readonly Color DangerZoneColor = Color.FromArgb(245, 196, 190);
    private static readonly Color TextColor = Color.FromArgb(26, 35, 48);
    private static readonly Color MutedColor = Color.FromArgb(96, 108, 122);
    private static readonly Color SpeedColor = Color.FromArgb(10, 98, 191);
    private static readonly Color RpmColor = Color.FromArgb(120, 82, 200);

    /// <summary>Межі шкали температури шин, °C.</summary>
    private const double TyreScaleMin = 50, TyreScaleMax = 140;

    /// <summary>Межі шкали температури двигуна, °C.</summary>
    private const double EngineScaleMin = 60, EngineScaleMax = 140;

    private const TextFormatFlags LineFlags =
        TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix;

    private Font? _boldFont;

    public TelemetryPanel()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        BackColor = Color.White;
    }

    /// <summary>Болід, телеметрію якого показує панель.</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Car? SelectedCar { get; set; }

    /// <summary>Поточна гонка (для позиції та відриву).</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Race? Race { get; set; }

    /// <summary>Поточна кваліфікація (якщо триває — показуються часи Q1/Q2/Q3 і сектори).</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public QualifyingSession? Qualifying { get; set; }

    /// <summary>Колір команди для смуги біля імені.</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color TeamColor { get; set; } = Color.Gray;

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        Graphics g = e.Graphics;
        g.Clear(BackColor);

        if (SelectedCar is not { } car || (Race == null && Qualifying == null))
        {
            TextRenderer.DrawText(g, "Клацніть по боліду на схемі або по рядку таблиці.", Font, ClientRectangle,
                MutedColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak);
            return;
        }

        g.SmoothingMode = SmoothingMode.AntiAlias;
        _boldFont ??= new Font(Font, FontStyle.Bold);

        int lineHeight = Font.Height + 2;
        int left = 2;
        int width = ClientSize.Width - 4;
        int y = 0;

        QualifyingCar? quali = Qualifying?.Cars.FirstOrDefault(q => q.Car == car);
        bool raceMode = quali == null && Race != null;

        // Заголовок: смуга кольору команди, ім'я, позиція і статус.
        using (var teamBrush = new SolidBrush(TeamColor))
            g.FillRectangle(teamBrush, left, y + 3, 4, lineHeight * 2 - 6);
        DrawLine(g, $"{car.Driver.FullName} — {car.Team.Name}", _boldFont, left + 10, y, width - 10, lineHeight, TextColor);
        y += lineHeight;
        string sub = raceMode
            ? $"P{car.Position} з {Race!.Cars.Count} · старт P{car.GridPosition} · {Race.GapText(car)} · {StateText(car)}"
            : $"P{quali!.Position} у кваліфікації · {quali.StageText}";
        DrawLine(g, sub, Font, left + 10, y, width - 10, lineHeight, MutedColor);
        y += lineHeight + 3;

        // Рядки телеметрії: підпис — шкала — значення.
        int rows = raceMode ? 9 : 3;
        int footer = raceMode ? 2 : 4;
        int labelWidth = Math.Min(104, width / 3);
        int valueWidth = Math.Min(132, width / 3);
        int barLeft = left + labelWidth;
        int barWidth = Math.Max(30, width - labelWidth - valueWidth - 8);
        int valueLeft = barLeft + barWidth + 8;
        int rowsArea = ClientSize.Height - y - footer * lineHeight - 4;
        int rowHeight = Math.Clamp(rowsArea / rows, lineHeight - 2, lineHeight + 8);

        void Row(string label, string value, Color valueColor, Action<Rectangle> drawScale)
        {
            DrawLine(g, label, Font, left, y, labelWidth, rowHeight, TextColor);
            var scale = new Rectangle(barLeft, y + rowHeight / 2 - 5, barWidth, 10);
            drawScale(scale);
            DrawLine(g, value, _boldFont!, valueLeft, y, valueWidth, rowHeight, valueColor);
            y += rowHeight;
        }

        // --- Водіння: швидкість, передача й оберти, газ і гальмо (з профілю справжньої телеметрії F1) ---
        CarTelemetry t = raceMode ? car.Telemetry : quali!.Telemetry;
        Row("Швидкість", $"{t.SpeedKmh:0} км/год", TextColor, r => DrawBar(g, r, t.SpeedKmh / 350, SpeedColor));
        Row("Передача, оберти", $"{t.GearText} · {t.Rpm:N0} об/хв", TextColor, r => DrawBar(g, r, t.Rpm / 12500.0, RpmColor));
        Row("Газ / гальмо", t.Brake ? "ГАЛЬМО" : $"газ {t.Throttle}%", t.Brake ? CriticalColor : NormalColor, r =>
        {
            DrawScaleBack(g, r);
            int half = r.Width / 2 - 2;
            var throttleRect = new Rectangle(r.X, r.Y, half, r.Height);
            var brakeRect = new Rectangle(r.X + half + 4, r.Y, half, r.Height);
            using (var green = new SolidBrush(NormalColor))
                g.FillRectangle(green, throttleRect.X, throttleRect.Y, (int)(throttleRect.Width * t.Throttle / 100.0), throttleRect.Height);
            if (t.Brake)
                using (var red = new SolidBrush(CriticalColor))
                    g.FillRectangle(red, brakeRect);
        });

        if (raceMode)
            DrawComponents(g, car, Row);

        // --- Підсумок ---
        y += 2;
        if (raceMode)
        {
            LapTimer timing = car.Timing;
            DrawLine(g, $"Останнє коло {LapTimer.Format(timing.LastLap)} · найкраще {LapTimer.Format(timing.BestLap)} · {SectorsText(timing.LastSectors)}",
                Font, left, y, width, lineHeight, TextColor);
            y += lineHeight;
            DrawLine(g, $"Режим: {ModeText(car)} · піт-стопів: {car.PitStops} · {string.Join(" → ", car.UsedCompounds)} · {car.Strategy}",
                Font, left, y, width, lineHeight, MutedColor);
        }
        else
        {
            var session = Qualifying!;
            DrawLine(g, $"Q1 {LapTimer.Format(quali!.Best(QualifyingSegment.Q1))} · Q2 {LapTimer.Format(quali.Best(QualifyingSegment.Q2))} · Q3 {LapTimer.Format(quali.Best(QualifyingSegment.Q3))}",
                _boldFont, left, y, width, lineHeight, TextColor);
            y += lineHeight;
            string current = quali.Stage == QualifyingStage.FlyingLap
                ? $"Швидке коло: {QualifyingResult.FormatLapTime(quali.Timing.CurrentLapTime(session.ElapsedSeconds))} · {SectorsText(quali.Timing.CurrentSectors)}"
                : $"Останнє коло: {LapTimer.Format(quali.LastLap)} · {SectorsText(quali.Timing.LastSectors)}";
            DrawLine(g, current, Font, left, y, width, lineHeight, TextColor);
            y += lineHeight;
            DrawLine(g, $"Найкращі сектори: {SectorsText(quali.Timing.BestSectors)}", Font, left, y, width, lineHeight, MutedColor);
            y += lineHeight;
            DrawLine(g, quali.IsEliminated ? $"Вибув у {quali.EliminatedIn}" : $"Виїздів у {session.Segment}: {quali.RunsDone}",
                Font, left, y, width, lineHeight, MutedColor);
        }
    }

    private static string SectorsText(IReadOnlyList<double?> sectors) =>
        "S1 " + Sector(sectors[0]) + " · S2 " + Sector(sectors[1]) + " · S3 " + Sector(sectors[2]);

    private static string Sector(double? s) => s is { } v ? v.ToString("0.000") : "—";

    /// <summary>Шини, двигун, паливо, ERS — шість рядків стану боліда в гонці.</summary>
    private void DrawComponents(Graphics g, Car car, Action<string, string, Color, Action<Rectangle>> Row)
    {
        Tyre tyre = car.Tyre;
        PowerUnit engine = car.Engine;
        FuelTank fuel = car.Fuel;
        ErsBattery battery = car.Battery;

        Color wearColor = tyre.Condition < car.Strategy.Threshold ? CriticalColor
            : tyre.Condition < 50 ? WarningColor
            : NormalColor;
        Row($"Шини {tyre.CompoundName}", $"{tyre.Condition:0}% · {UkrainianText.Laps((int)tyre.LapsUsed)}", wearColor,
            r => DrawBar(g, r, tyre.Condition / 100, wearColor));

        Color tyreTempColor = tyre.Temperature < tyre.OptimalMinTemperature ? ColdColor
            : tyre.IsOverheating ? CriticalColor
            : NormalColor;
        string tyreTempState = tyre.Temperature < tyre.OptimalMinTemperature ? "холодні"
            : tyre.IsOverheating ? "перегрів"
            : "у вікні";
        Row("Темп. шин", $"{tyre.Temperature:0} °C · {tyreTempState}", tyreTempColor, r =>
        {
            DrawScaleBack(g, r);
            DrawZone(g, r, TyreScaleMin, TyreScaleMax, tyre.OptimalMinTemperature, tyre.OptimalMaxTemperature, WindowZoneColor);
            DrawMarker(g, r, TyreScaleMin, TyreScaleMax, tyre.Temperature, tyreTempColor);
        });

        Color engineTempColor = engine.Temperature >= PowerUnit.CriticalTemperature ? CriticalColor
            : engine.Temperature >= PowerUnit.WarningTemperature ? WarningColor
            : NormalColor;
        Row("Двигун", $"{engine.Temperature:0} °C", engineTempColor, r =>
        {
            DrawScaleBack(g, r);
            DrawZone(g, r, EngineScaleMin, EngineScaleMax, PowerUnit.WarningTemperature, PowerUnit.CriticalTemperature, HotZoneColor);
            DrawZone(g, r, EngineScaleMin, EngineScaleMax, PowerUnit.CriticalTemperature, EngineScaleMax, DangerZoneColor);
            DrawMarker(g, r, EngineScaleMin, EngineScaleMax, engine.Temperature, engineTempColor);
        });

        Color engineWearColor = engine.Wear >= 90 ? CriticalColor : engine.Wear >= 70 ? WarningColor : NormalColor;
        Row("Знос двигуна", $"{engine.Wear:0.0}%", engineWearColor, r => DrawBar(g, r, engine.Wear / 100, engineWearColor));

        Color fuelColor = StatusColor(fuel.Status);
        Row("Паливо", $"{fuel.FuelKg:0.0} кг · {fuel.LapsLeft:0} к.", fuelColor, r => DrawBar(g, r, fuel.Level, fuelColor));

        Color ersColor = battery.IsDeploying ? ColdColor : StatusColor(battery.Status);
        Row("Батарея ERS", battery.IsDeploying ? $"{battery.Charge:0}% · віддача" : $"{battery.Charge:0}%", ersColor,
            r => DrawBar(g, r, battery.Level, ersColor));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _boldFont?.Dispose();
        base.Dispose(disposing);
    }

    /// <summary>Стан словами; для пов'язаних із фінішем станів — етап кола охолодження.</summary>
    public static string StateText(Car car) => car.State switch
    {
        CarState.OnTrack => "на трасі",
        CarState.InPitLane => "піт-лейн",
        CarState.PitStop => $"зупинка ({car.StopTimeRemaining:0.0} с)",
        CarState.Finished => car.PostRace switch
        {
            PostRaceStage.CoolDownLap => "фініш · коло охолодження",
            PostRaceStage.PitLane => "фініш · в'їзд у парк-ферме",
            PostRaceStage.Parked => "фініш · у парк-ферме",
            _ => "фінішував"
        },
        CarState.Retired => "сход: " + car.RetirementReason,
        _ => car.State.ToString()
    };

    /// <summary>Режим їзди словами.</summary>
    public static string ModeText(Car car)
    {
        if (car.State != CarState.OnTrack) return "—";
        return car.Mode switch
        {
            DrivingMode.Saving => "економія",
            DrivingMode.Attack => "атака",
            DrivingMode.Defend => "захист",
            _ => "рівний темп"
        };
    }

    private static Color StatusColor(ComponentStatus status) => status switch
    {
        ComponentStatus.Critical => CriticalColor,
        ComponentStatus.Warning => WarningColor,
        _ => NormalColor
    };

    private static void DrawLine(Graphics g, string text, Font font, int x, int y, int width, int height, Color color) =>
        TextRenderer.DrawText(g, text, font, new Rectangle(x, y, Math.Max(0, width), height), color, LineFlags);

    private static void DrawScaleBack(Graphics g, Rectangle r)
    {
        using var back = new SolidBrush(ScaleBackColor);
        g.FillRectangle(back, r);
    }

    /// <summary>Горизонтальна смуга заповнення від 0 до <paramref name="fraction"/>.</summary>
    private static void DrawBar(Graphics g, Rectangle r, double fraction, Color color)
    {
        DrawScaleBack(g, r);
        int filled = (int)Math.Round(r.Width * Math.Clamp(fraction, 0, 1));
        if (filled <= 0) return;
        using var brush = new SolidBrush(color);
        g.FillRectangle(brush, r.X, r.Y, filled, r.Height);
    }

    /// <summary>Кольорова зона шкали між значеннями <paramref name="from"/> і <paramref name="to"/>.</summary>
    private static void DrawZone(Graphics g, Rectangle r, double min, double max, double from, double to, Color color)
    {
        float x1 = ToX(r, min, max, from), x2 = ToX(r, min, max, to);
        if (x2 <= x1) return;
        using var brush = new SolidBrush(color);
        g.FillRectangle(brush, x1, r.Y, x2 - x1, r.Height);
    }

    /// <summary>Вертикальна позначка поточного значення на шкалі.</summary>
    private static void DrawMarker(Graphics g, Rectangle r, double min, double max, double value, Color color)
    {
        float x = ToX(r, min, max, value);
        using var brush = new SolidBrush(color);
        g.FillRectangle(brush, x - 2, r.Y - 3, 4, r.Height + 6);
    }

    private static float ToX(Rectangle r, double min, double max, double value) =>
        r.X + (float)(r.Width * Math.Clamp((value - min) / (max - min), 0, 1));
}
