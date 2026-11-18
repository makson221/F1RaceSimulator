using System.Drawing.Drawing2D;
using System.Drawing.Text;
using F1RaceSimulator.Core.Geometry;
using F1RaceSimulator.Core.Models;
using F1RaceSimulator.Core.Simulation;

namespace F1RaceSimulator.WinForms;

/// <summary>
/// Малює схему траси в стилі офіційних карт F1: трасу з кольоровими секторами,
/// номери поворотів, піт-лейн, бокси, лінію старту і боліди будь-якої сесії
/// (<see cref="ITrackSession"/>: кваліфікації або гонки). Відповідає лише за малювання
/// і нічого не змінює в симуляції (принцип єдиної відповідальності).
/// </summary>
public sealed class TrackRenderer
{
    // Кольори траси залежать від теми (світла / темна) і беруться під час малювання.
    private static Color TrackColor => ThemeManager.Current.Track;
    private static Color PitLaneColor => ThemeManager.Current.PitLane;
    private static Color AccentColor => ThemeManager.Current.Accent;
    private static Color LabelColor => ThemeManager.Current.Text;

    /// <summary>Кольори секторів, як на офіційних картах: червоний, синій, жовтий.</summary>
    private static readonly Color[] SectorColors =
    {
        Color.FromArgb(225, 6, 0), Color.FromArgb(0, 144, 224), Color.FromArgb(240, 190, 0)
    };

    private readonly Dictionary<string, Color> _colorCache = new();
    private readonly List<(Car Car, PointF Point)> _carPositions = new();

    /// <summary>Екранні координати болідів після останнього малювання (для вибору мишею).</summary>
    public IReadOnlyList<(Car Car, PointF Point)> CarPositions => _carPositions;

    /// <summary>Малює трасу і боліди сесії (сесія може бути null — тоді лише траса).</summary>
    public void Draw(Graphics g, Rectangle bounds, Track track, ITrackSession? session, Car? selected)
    {
        ArgumentNullException.ThrowIfNull(g);
        ArgumentNullException.ThrowIfNull(track);

        _carPositions.Clear();
        TrackLayout layout = track.Layout;

        const float margin = 10f;
        float scale = (float)Math.Min(
            (bounds.Width - 2 * margin) / layout.Width,
            (bounds.Height - 2 * margin) / layout.Height);
        if (scale <= 0) return;

        float offsetX = bounds.Left + (bounds.Width - (float)layout.Width * scale) / 2f;
        float offsetY = bounds.Top + (bounds.Height - (float)layout.Height * scale) / 2f;
        PointF Map(Vec2 p) => new(offsetX + (float)p.X * scale, offsetY + (float)p.Y * scale);

        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        // 1. Траса — товста темна лінія, поверх неї тонкі кольорові лінії секторів.
        Polyline path = layout.TrackPath;
        PointF[] trackPoints = path.Points.Select(Map).ToArray();
        using (var trackPen = new Pen(TrackColor, Math.Max(5f, 7f * scale)) { LineJoin = LineJoin.Round })
            g.DrawPolygon(trackPen, trackPoints);
        DrawSectors(g, layout, Map, Math.Max(1.6f, 2f * scale));

        // 2. Піт-лейн — тонка червона лінія.
        PointF[] pitPoints = layout.PitLanePath.Points.Select(Map).ToArray();
        using (var pitPen = new Pen(PitLaneColor, Math.Max(1.5f, 1.8f * scale)) { LineJoin = LineJoin.Round })
            g.DrawLines(pitPen, pitPoints);

        // 3. Бокси команд у піт-лейні.
        if (session != null)
        {
            float box = Math.Max(3f, 2.6f * scale);
            foreach (Team team in session.GetCarPositions().Select(c => c.Car.Team).Concat(TeamsOf(session)).Distinct())
            {
                PointF p = Map(layout.PitLanePath.PointAt(Race.BoxPositionFor(team)));
                using var brush = new SolidBrush(ColorOf(team));
                g.FillRectangle(brush, p.X - box / 2, p.Y - box / 2, box, box);
            }
        }

        float fontSize = Math.Clamp(3.6f * scale, 7.5f, 11f);
        using var font = new Font("Segoe UI", fontSize, FontStyle.Bold, GraphicsUnit.Point);
        using var labelBrush = new SolidBrush(LabelColor);

        // 4. Номери поворотів — кружечки збоку від траси, як на офіційній карті.
        DrawTurns(g, layout, Map, scale);

        // 5. Лінія старту/фінішу та підпис.
        if (layout.StartFinish.Length == 4)
        {
            var a = Map(new Vec2(layout.StartFinish[0], layout.StartFinish[1]));
            var b = Map(new Vec2(layout.StartFinish[2], layout.StartFinish[3]));
            using (var linePen = new Pen(ThemeManager.Current.StartLine, Math.Max(2.5f, 2f * scale)))
                g.DrawLine(linePen, a, b);

            if (layout.LabelPosition.Length == 2)
            {
                const string text = "Старт / фініш";
                SizeF size = g.MeasureString(text, font);
                PointF label = Map(new Vec2(layout.LabelPosition[0], layout.LabelPosition[1]));
                var rect = new RectangleF(label.X - size.Width / 2, label.Y - size.Height * 0.8f, size.Width, size.Height);
                if (layout.LabelLeader)
                {
                    var mid = new PointF((a.X + b.X) / 2, (a.Y + b.Y) / 2);
                    float y = rect.Bottom < mid.Y ? rect.Bottom : rect.Top;
                    using var leaderPen = new Pen(ThemeManager.Current.Muted, 1f) { DashStyle = DashStyle.Dash };
                    g.DrawLine(leaderPen, label.X, y, mid.X, mid.Y);
                }
                g.DrawString(text, font, labelBrush, rect.Location);
            }
        }

        // 6. Стрілка напрямку руху поруч зі стартом.
        DrawDirectionArrow(g, path, Map);

        // 7. Боліди (сесія повертає їх від останнього до першого — лідер зверху).
        if (session == null) return;
        float radius = Math.Max(3.5f, 4f * scale);
        foreach (CarPosition cp in session.GetCarPositions())
        {
            Vec2 position = cp.InPitLane
                ? layout.PitLanePath.PointAt(cp.Fraction)
                : path.PointAt(cp.Fraction);
            PointF p = Map(position);
            _carPositions.Add((cp.Car, p));

            bool isSelected = cp.Car == selected;
            using (var fill = new SolidBrush(ColorOf(cp.Car.Team)))
                g.FillEllipse(fill, p.X - radius, p.Y - radius, radius * 2, radius * 2);
            using (var outline = new Pen(ThemeManager.Current.Halo, 1f))
                g.DrawEllipse(outline, p.X - radius, p.Y - radius, radius * 2, radius * 2);

            if (isSelected)
            {
                float ring = radius + 3;
                using var ringPen = new Pen(AccentColor, 2f);
                g.DrawEllipse(ringPen, p.X - ring, p.Y - ring, ring * 2, ring * 2);
            }

            string? text = cp.Label ?? (isSelected ? cp.Car.Driver.Code : null);
            if (text != null)
            {
                SizeF size = g.MeasureString(text, font);
                var textPoint = new PointF(p.X - size.Width / 2, p.Y - radius - size.Height);
                using var halo = new SolidBrush(Color.FromArgb(200, ThemeManager.Current.Halo));
                g.FillRectangle(halo, textPoint.X + 2, textPoint.Y + 2, size.Width - 4, size.Height - 4);
                g.DrawString(text, font, labelBrush, textPoint);
            }
        }

        // 8. Сейфті-кар на трасі і табличка нейтралізації.
        if (session is Race race)
            DrawSafetyCar(g, bounds, race, path, Map, radius);
    }

    /// <summary>Машина безпеки трохи попереду лідера і жовта табличка «СЕЙФТІ-КАР» / «VSC».</summary>
    private static void DrawSafetyCar(Graphics g, Rectangle bounds, Race race, Polyline path, Func<Vec2, PointF> map, float radius)
    {
        if (race.SafetyCar == SafetyCarMode.None) return;
        Color yellow = Color.FromArgb(255, 200, 0);
        using var black = new SolidBrush(Color.FromArgb(20, 20, 20));
        using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

        if (race.SafetyCarFraction is { } fraction)
        {
            PointF p = map(path.PointAt(fraction));
            float w = radius * 4.2f, h = radius * 2.6f;
            var rect = new RectangleF(p.X - w / 2, p.Y - h / 2, w, h);
            using (var fill = new SolidBrush(yellow))
            using (var shape = RoundedRect(rect, h / 2.5f))
            {
                g.FillPath(fill, shape);
                using var border = new Pen(Color.FromArgb(20, 20, 20), 1f);
                g.DrawPath(border, shape);
            }
            using var tiny = new Font("Segoe UI", Math.Max(6.5f, h * 0.48f), FontStyle.Bold, GraphicsUnit.Pixel);
            g.DrawString("SC", tiny, black, rect, format);
        }

        string text = race.SafetyCar == SafetyCarMode.SafetyCar ? "СЕЙФТІ-КАР" : "ВІРТУАЛЬНИЙ СЕЙФТІ-КАР";
        using var font = new Font("Segoe UI", 10f, FontStyle.Bold, GraphicsUnit.Point);
        SizeF size = g.MeasureString(text, font);
        var banner = new RectangleF(bounds.Left + 10, bounds.Top + 10, size.Width + 22, size.Height + 8);
        using (var fill = new SolidBrush(yellow))
        using (var shape = RoundedRect(banner, 5))
            g.FillPath(fill, shape);
        g.DrawString(text, font, black, banner, format);
    }

    /// <summary>
    /// Табло стартових вогнів: п'ять колонок по два червоні вогні. Вогні загоряються по одному,
    /// а коли гаснуть усі — стартує гонка, і табло плавно зникає.
    /// </summary>
    public static void DrawStartLights(Graphics g, Rectangle bounds, StartLightsView view)
    {
        ArgumentNullException.ThrowIfNull(g);
        ArgumentNullException.ThrowIfNull(view);
        float alpha = Math.Clamp(view.Opacity, 0f, 1f);
        if (alpha <= 0) return;
        Color A(Color c, float k = 1f) => Color.FromArgb((int)(c.A * alpha * k), c);

        g.SmoothingMode = SmoothingMode.AntiAlias;
        float lamp = Math.Clamp(bounds.Width / 30f, 14f, 26f);
        float gap = lamp * 0.45f, pad = lamp * 0.45f;
        float columnWidth = lamp + pad;
        float width = Race.StartLightCount * columnWidth + (Race.StartLightCount - 1) * gap + pad * 2;
        float height = lamp * 2 + pad * 4;
        var housing = new RectangleF(bounds.Left + (bounds.Width - width) / 2, bounds.Top + 12, width, height);

        // Корпус табло.
        using (var fill = new SolidBrush(A(Color.FromArgb(235, 18, 18, 22))))
        using (var shape = RoundedRect(housing, lamp * 0.35f))
            g.FillPath(fill, shape);

        for (int i = 0; i < Race.StartLightCount; i++)
        {
            float x = housing.Left + pad + i * (columnWidth + gap);
            var column = new RectangleF(x, housing.Top + pad, columnWidth, height - pad * 2);
            using (var fill = new SolidBrush(A(Color.FromArgb(255, 38, 38, 44))))
            using (var shape = RoundedRect(column, lamp * 0.25f))
                g.FillPath(fill, shape);

            bool lit = i < view.LitCount;
            for (int row = 0; row < 2; row++)
            {
                float cx = column.Left + column.Width / 2;
                float cy = column.Top + pad * 0.75f + lamp / 2 + row * (lamp + pad * 0.5f);
                if (lit)
                {
                    using var glow = new SolidBrush(A(Color.FromArgb(90, 255, 40, 30)));
                    float r = lamp * 0.78f;
                    g.FillEllipse(glow, cx - r, cy - r, r * 2, r * 2);
                }
                using var bulb = new SolidBrush(A(lit ? Color.FromArgb(255, 255, 36, 24) : Color.FromArgb(255, 70, 22, 22)));
                g.FillEllipse(bulb, cx - lamp / 2, cy - lamp / 2, lamp, lamp);
                if (lit)
                {
                    using var shine = new SolidBrush(A(Color.FromArgb(150, 255, 210, 200)));
                    g.FillEllipse(shine, cx - lamp * 0.22f, cy - lamp * 0.3f, lamp * 0.26f, lamp * 0.2f);
                }
            }
        }

        // Підпис під табло.
        using var font = new Font("Segoe UI", 9.5f, FontStyle.Bold, GraphicsUnit.Point);
        SizeF size = g.MeasureString(view.Caption, font);
        var pill = new RectangleF(housing.Left + (housing.Width - size.Width - 18) / 2, housing.Bottom + 6, size.Width + 18, size.Height + 4);
        using (var fill = new SolidBrush(A(Color.FromArgb(225, 18, 18, 22))))
        using (var shape = RoundedRect(pill, pill.Height / 2))
            g.FillPath(fill, shape);
        using var textBrush = new SolidBrush(A(Color.White));
        using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString(view.Caption, font, textBrush, pill, format);
    }

    /// <summary>Прямокутник із заокругленими кутами.</summary>
    private static GraphicsPath RoundedRect(RectangleF r, float radius)
    {
        float d = Math.Max(0.1f, Math.Min(radius * 2, Math.Min(r.Width, r.Height)));
        var path = new GraphicsPath();
        path.AddArc(r.Left, r.Top, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Top, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.Left, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    /// <summary>Команди всіх учасників сесії (бокси видно, навіть коли боліди в гаражі).</summary>
    private static IEnumerable<Team> TeamsOf(ITrackSession session) => session switch
    {
        Race race => race.Cars.Select(c => c.Team),
        QualifyingSession q => q.Cars.Select(c => c.Car.Team),
        _ => Enumerable.Empty<Team>()
    };

    /// <summary>Три сектори траси кольоровими лініями поверх траси.</summary>
    private static void DrawSectors(Graphics g, TrackLayout layout, Func<Vec2, PointF> map, float width)
    {
        double[] bounds = layout.Sectors.Length >= 2
            ? new[] { 0, layout.Sectors[0], layout.Sectors[1], 1 }
            : new[] { 0, 1.0 / 3, 2.0 / 3, 1 };
        Polyline path = layout.TrackPath;
        for (int s = 0; s < 3; s++)
        {
            // Крок 0.25 % кола — лінія рівна, а межі секторів точні.
            var points = new List<PointF>();
            for (double f = bounds[s]; f < bounds[s + 1]; f += 0.0025)
                points.Add(map(path.PointAt(f)));
            points.Add(map(path.PointAt(bounds[s + 1] - 1e-6)));
            if (points.Count < 2) continue;
            using var pen = new Pen(SectorColors[s], width) { LineJoin = LineJoin.Round, StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawLines(pen, points.ToArray());
        }
    }

    /// <summary>Номери поворотів у білих кружечках.</summary>
    private static void DrawTurns(Graphics g, TrackLayout layout, Func<Vec2, PointF> map, float scale)
    {
        if (layout.Turns.Length == 0) return;
        float r = Math.Clamp(4.6f * scale, 6f, 9f);
        using var font = new Font("Segoe UI", Math.Clamp(r * 0.95f, 6f, 8.5f), FontStyle.Bold, GraphicsUnit.Pixel);
        using var fill = new SolidBrush(ThemeManager.Current.TurnFill);
        using var border = new Pen(ThemeManager.Current.TurnBorder, 1f);
        using var text = new SolidBrush(ThemeManager.Current.Text);
        using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        foreach (TurnMarker turn in layout.Turns)
        {
            PointF c = map(new Vec2(turn.LabelX, turn.LabelY));
            float rr = turn.Label.Length > 2 ? r * 1.25f : r;
            g.FillEllipse(fill, c.X - rr, c.Y - rr, rr * 2, rr * 2);
            g.DrawEllipse(border, c.X - rr, c.Y - rr, rr * 2, rr * 2);
            g.DrawString(turn.Label, font, text, new RectangleF(c.X - rr, c.Y - rr, rr * 2, rr * 2 + 1), format);
        }
    }

    /// <summary>Повертає болід, найближчий до точки кліку (не далі <paramref name="maxDistance"/> пікселів).</summary>
    public Car? HitTest(Point point, float maxDistance = 12f)
    {
        Car? best = null;
        double bestDistance = maxDistance;
        foreach (var (car, p) in _carPositions)
        {
            double d = Math.Sqrt((p.X - point.X) * (p.X - point.X) + (p.Y - point.Y) * (p.Y - point.Y));
            if (d <= bestDistance)
            {
                bestDistance = d;
                best = car;
            }
        }
        return best;
    }

    /// <summary>Колір команди (з кешем, щоб не розбирати рядок щокадру).</summary>
    public Color ColorOf(Team team)
    {
        if (!_colorCache.TryGetValue(team.Color, out Color color))
        {
            try { color = ColorTranslator.FromHtml(team.Color); }
            catch (Exception ex) when (ex is ArgumentException or FormatException) { color = Color.Gray; }
            _colorCache[team.Color] = color;
        }
        return color;
    }

    private static void DrawDirectionArrow(Graphics g, Polyline path, Func<Vec2, PointF> map)
    {
        const double at = 0.025;
        Vec2 tip = path.PointAt(at + 0.006);
        Vec2 dir = path.DirectionAt(at);
        var normal = new Vec2(-dir.Y, dir.X);
        double size = 3.5;
        Vec2 baseCenter = tip - dir * (size * 1.6);
        PointF[] triangle =
        {
            map(tip),
            map(baseCenter + normal * size),
            map(baseCenter - normal * size)
        };
        using var brush = new SolidBrush(AccentColor);
        g.FillPolygon(brush, triangle);
    }
}
