namespace F1RaceSimulator.Core.Models;

/// <summary>
/// Поворот траси з офіційною нумерацією F1 (дані хронометражу F1 через FastF1).
/// Координати задано в системі координат схеми (<see cref="TrackLayout"/>).
/// </summary>
public sealed class TurnMarker
{
    /// <summary>Номер повороту.</summary>
    public int Number { get; init; }

    /// <summary>Підпис на схемі, наприклад "1", "12A".</summary>
    public string Label { get; init; } = string.Empty;

    /// <summary>Частка кола (0..1), на якій розташований поворот.</summary>
    public double Fraction { get; init; }

    /// <summary>Точка повороту на осьовій лінії траси.</summary>
    public double X { get; init; }

    public double Y { get; init; }

    /// <summary>Місце для кружечка з номером (збоку від траси).</summary>
    public double LabelX { get; init; }

    public double LabelY { get; init; }

    public override string ToString() => "Поворот " + Label;
}
