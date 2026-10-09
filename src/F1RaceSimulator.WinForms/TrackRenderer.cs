using System.Drawing.Drawing2D;
using System.Drawing.Text;
using F1RaceSimulator.Core.Geometry;
using F1RaceSimulator.Core.Models;
using F1RaceSimulator.Core.Simulation;

namespace F1RaceSimulator.WinForms;

/// <summary>
/// Малює схему траси, піт-лейн і боліди засобами GDI+ (<see cref="Graphics"/>).
/// Відповідає лише за малювання і нічого не змінює в гонці
/// (принцип єдиної відповідальності).
/// </summary>
public sealed class TrackRenderer
{
    private static readonly Color TrackColor = Color.FromArgb(75, 86, 100);
    private static readonly Color PitLaneColor = Color.FromArgb(192, 57, 43);
    private static readonly Color AccentColor = Color.FromArgb(10, 98, 191);
    private static readonly Color LabelColor = Color.FromArgb(26, 35, 48);

    private readonly Dictionary<string, Color> _colorCache = new();
    private readonly List<(Car Car, PointF Point)> _carPositions = new();

    /// <summary>Екранні координати болідів після останнього малювання (для вибору мишею).</summary>
    public IReadOnlyList<(Car Car, PointF Point)> CarPositions => _carPositions;

    /// <summary>Малює трасу, піт-лейн, лінію старту, бокси та боліди.</summary>
    public void Draw(Graphics g, Rectangle bounds, Track track, Race? race, Car? selected)
    {
        ArgumentNullException.ThrowIfNull(g);
        ArgumentNullException.ThrowIfNull(track);

        _carPositions.Clear();
        TrackLayout layout = track.Layout;

        const float margin = 12f;
        float scale = (float)Math.Min(
            (bounds.Width - 2 * margin) / layout.Width,
            (bounds.Height - 2 * margin) / layout.Height);
        if (scale <= 0) return;

        float offsetX = bounds.Left + (bounds.Width - (float)layout.Width * scale) / 2f;
        float offsetY = bounds.Top + (bounds.Height - (float)layout.Height * scale) / 2f;
        PointF Map(Vec2 p) => new(offsetX + (float)p.X * scale, offsetY + (float)p.Y * scale);

        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        // 1. Траса — товста сіра лінія.
        PointF[] trackPoints = layout.TrackPath.Points.Select(Map).ToArray();
        using (var trackPen = new Pen(TrackColor, Math.Max(4f, 6f * scale)))
        {
            trackPen.LineJoin = LineJoin.Round;
            g.DrawPolygon(trackPen, trackPoints);
        }

        // 2. Піт-лейн — тонка червона лінія.
        PointF[] pitPoints = layout.PitLanePath.Points.Select(Map).ToArray();
        using (var pitPen = new Pen(PitLaneColor, Math.Max(1.5f, 1.8f * scale)))
        {
            pitPen.LineJoin = LineJoin.Round;
            g.DrawLines(pitPen, pitPoints);
        }

        // 3. Бокси команд у піт-лейні.
        if (race != null)
        {
            float box = Math.Max(3f, 2.6f * scale);
            foreach (Team team in race.Cars.Select(c => c.Team).Distinct())
            {
                PointF p = Map(layout.PitLanePath.PointAt(Race.BoxPositionFor(team)));
                using var brush = new SolidBrush(ColorOf(team));
                g.FillRectangle(brush, p.X - box / 2, p.Y - box / 2, box, box);
            }
        }

        float fontSize = Math.Clamp(3.6f * scale, 7.5f, 11f);
        using var font = new Font("Segoe UI", fontSize, FontStyle.Bold, GraphicsUnit.Point);
        using var labelBrush = new SolidBrush(LabelColor);

        // 4. Лінія старту/фінішу та підпис.
        if (layout.StartFinish.Length == 4)
        {
            var a = Map(new Vec2(layout.StartFinish[0], layout.StartFinish[1]));
            var b = Map(new Vec2(layout.StartFinish[2], layout.StartFinish[3]));
            using var linePen = new Pen(Color.Black, Math.Max(2f, 1.8f * scale));
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
                    using var leaderPen = new Pen(Color.Gray, 1f) { DashStyle = DashStyle.Dash };
                    g.DrawLine(leaderPen, label.X, y, mid.X, mid.Y);
                }
                g.DrawString(text, font, labelBrush, rect.Location);
            }
        }

        // 5. Стрілка напрямку руху поруч зі стартом.
        DrawDirectionArrow(g, layout.TrackPath, Map);

        // 6. Боліди. Спочатку задні, щоб лідер був зверху.
        if (race == null) return;
        float radius = Math.Max(3.5f, 4f * scale);
        foreach (Car car in race.Standings.Reverse())
        {
            if (car.State == CarState.Retired) continue;

            Vec2 position = car.IsInPits
                ? layout.PitLanePath.PointAt(car.PitLaneProgress)
                : layout.TrackPath.PointAt(Polyline.Wrap(car.Distance));
            PointF p = Map(position);
            _carPositions.Add((car, p));

            bool isSelected = car == selected;
            using (var fill = new SolidBrush(ColorOf(car.Team)))
                g.FillEllipse(fill, p.X - radius, p.Y - radius, radius * 2, radius * 2);
            using (var outline = new Pen(Color.White, 1f))
                g.DrawEllipse(outline, p.X - radius, p.Y - radius, radius * 2, radius * 2);

            if (isSelected)
            {
                float ring = radius + 3;
                using var ringPen = new Pen(AccentColor, 2f);
                g.DrawEllipse(ringPen, p.X - ring, p.Y - ring, ring * 2, ring * 2);
            }

            if (car.Position <= 3 || isSelected || car.IsInPits)
            {
                string text = car.IsInPits ? car.Driver.Code + " · PIT" : car.Driver.Code;
                SizeF size = g.MeasureString(text, font);
                var textPoint = new PointF(p.X - size.Width / 2, p.Y - radius - size.Height);
                using var halo = new SolidBrush(Color.FromArgb(200, Color.White));
                g.FillRectangle(halo, textPoint.X + 2, textPoint.Y + 2, size.Width - 4, size.Height - 4);
                g.DrawString(text, font, labelBrush, textPoint);
            }
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
