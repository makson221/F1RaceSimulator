using System.Globalization;
using System.Text.Json.Serialization;
using F1RaceSimulator.Core.Geometry;

namespace F1RaceSimulator.Core.Models;

/// <summary>
/// Траса (етап календаря). Дані завантажуються з файлу data/tracks.json.
/// </summary>
public sealed class Track
{
    /// <summary>Ідентифікатор траси, наприклад "it-1922" (Монца).</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Номер етапу в календарі сезону.</summary>
    public int Round { get; init; }

    /// <summary>Назва Гран-прі, наприклад "Гран-прі Італії".</summary>
    public string GrandPrix { get; init; } = string.Empty;

    public string City { get; init; } = string.Empty;

    /// <summary>Назва автодрому.</summary>
    public string Circuit { get; init; } = string.Empty;

    /// <summary>Дата гонки у форматі РРРР-ММ-ДД.</summary>
    public string Date { get; init; } = string.Empty;

    /// <summary>Довжина кола в кілометрах.</summary>
    public double LengthKm { get; init; }

    /// <summary>Кількість кіл гонки.</summary>
    public int Laps { get; init; }

    /// <summary>Типовий час кола в гонці, секунди. Використовується для розрахунку темпу і відривів.</summary>
    public double TypicalLapSeconds { get; init; }

    /// <summary>Коефіцієнт абразивності: 1.0 — середній знос, більше — гума зношується швидше.</summary>
    public double TyreWearFactor { get; init; } = 1.0;

    /// <summary>Час проїзду піт-лейну без урахування зупинки, секунди.</summary>
    public double PitLaneSeconds { get; init; } = 22;

    /// <summary>Погода на етапі (текст для інтерфейсу).</summary>
    public string Weather { get; init; } = string.Empty;

    /// <summary>Температура повітря, °C. Впливає на нагрів шин і двигуна.</summary>
    public double AirTemperature { get; init; } = 25;

    /// <summary>Геометрія схеми траси.</summary>
    public TrackLayout Layout { get; init; } = new();

    /// <summary>Профіль швидкого кола за справжньою телеметрією F1 (швидкість, передачі, газ, гальмо).</summary>
    public SpeedProfile Profile { get; init; } = new();

    /// <summary>Номер сектора (1–3) для частки кола.</summary>
    public int SectorAt(double fraction)
    {
        double f = Polyline.Wrap(fraction);
        double[] s = Layout.Sectors;
        if (s.Length < 2) return f < 1.0 / 3 ? 1 : f < 2.0 / 3 ? 2 : 3;
        return f < s[0] ? 1 : f < s[1] ? 2 : 3;
    }

    /// <summary>Дата гонки як <see cref="DateOnly"/> (або null, якщо формат невірний).</summary>
    [JsonIgnore]
    public DateOnly? RaceDate =>
        DateOnly.TryParseExact(Date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
            ? d
            : null;

    public override string ToString() => $"{Round}. {GrandPrix} ({City})";
}

/// <summary>
/// Геометрія траси у власній системі координат. Контур побудовано за даними хронометражу F1
/// (позиції болідів на швидкому колі), повернутими до офіційної орієнтації карти F1.
/// Перша точка — лінія старту/фінішу. Розмір системи координат
/// (<see cref="Width"/> × <see cref="Height"/>) залежить від форми траси.
/// </summary>
public sealed class TrackLayout
{
    private Polyline? _trackPath;
    private Polyline? _pitLanePath;

    /// <summary>Точки осьової лінії траси. Перша точка — лінія старту/фінішу.</summary>
    public double[][] Points { get; init; } = Array.Empty<double[]>();

    /// <summary>Точки піт-лейну від в'їзду до виїзду (схематично).</summary>
    public double[][] PitLane { get; init; } = Array.Empty<double[]>();

    /// <summary>Частка кола (0..1), на якій розташований в'їзд у піт-лейн.</summary>
    public double PitEntry { get; init; }

    /// <summary>Частка кола (0..1), на якій розташований виїзд з піт-лейну.</summary>
    public double PitExit { get; init; }

    /// <summary>Відрізок лінії старту/фінішу: [x1, y1, x2, y2].</summary>
    public double[] StartFinish { get; init; } = Array.Empty<double>();

    /// <summary>Позиція підпису «Старт / фініш»: [x, y].</summary>
    public double[] LabelPosition { get; init; } = Array.Empty<double>();

    /// <summary>Чи потрібна виносна лінія від підпису до лінії старту.</summary>
    public bool LabelLeader { get; init; }

    /// <summary>Межі секторів 1/2 і 2/3 як частки кола (за офіційним хронометражем).</summary>
    public double[] Sectors { get; init; } = Array.Empty<double>();

    /// <summary>Повороти з офіційною нумерацією.</summary>
    public TurnMarker[] Turns { get; init; } = Array.Empty<TurnMarker>();

    /// <summary>
    /// Орієнтація схеми: "official" — як на офіційній карті F1 (кут Rotation з даних хронометражу),
    /// "north" — північ угорі (для нових трас без офіційних даних).
    /// </summary>
    public string Orientation { get; init; } = "north";

    /// <summary>Кут повороту координат телеметрії до офіційної орієнтації, градуси.</summary>
    public double Rotation { get; init; }

    /// <summary>Ширина системи координат схеми.</summary>
    public double Width { get; init; } = 340;

    /// <summary>Висота системи координат схеми.</summary>
    public double Height { get; init; } = 205;

    /// <summary>Замкнена ламана траси (створюється при першому зверненні).</summary>
    [JsonIgnore]
    public Polyline TrackPath => _trackPath ??= Polyline.FromArray(Points, isClosed: true);

    /// <summary>Відкрита ламана піт-лейну (створюється при першому зверненні).</summary>
    [JsonIgnore]
    public Polyline PitLanePath => _pitLanePath ??= Polyline.FromArray(PitLane, isClosed: false);

    /// <summary>Частка кола, яку болід «пропускає», проїжджаючи піт-лейном замість траси.</summary>
    [JsonIgnore]
    public double PitSpan => Polyline.Wrap(PitExit - PitEntry);
}
