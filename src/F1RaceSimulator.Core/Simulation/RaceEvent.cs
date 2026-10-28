namespace F1RaceSimulator.Core.Simulation;

/// <summary>Тип події гонки або кваліфікації.</summary>
public enum RaceEventType
{
    RaceStarted,
    LapCompleted,
    Overtake,
    PitEntry,
    PitStop,
    Retirement,
    TechnicalWarning,
    LeaderFinished,
    RaceFinished,

    /// <summary>Новий найшвидший час кола в гонці.</summary>
    FastestLap,

    /// <summary>Усі боліди проїхали коло охолодження і стоять у парк-ферме.</summary>
    CarsParked,

    /// <summary>Почався сегмент кваліфікації (Q1, Q2 або Q3).</summary>
    QualifyingSegmentStarted,

    /// <summary>Пілот показав час кола в кваліфікації.</summary>
    QualifyingLap,

    /// <summary>Час кола анульовано (вихід за межі траси).</summary>
    QualifyingLapDeleted,

    /// <summary>Сегмент кваліфікації завершено, найповільніші вибули.</summary>
    QualifyingSegmentFinished,

    /// <summary>Кваліфікацію завершено, визначено поул.</summary>
    QualifyingFinished
}

/// <summary>
/// Подія сесії, яку <see cref="Race"/> або <see cref="QualifyingSession"/> надсилає спостерігачам.
/// </summary>
/// <param name="Type">Тип події.</param>
/// <param name="Lap">Коло лідера на момент події (у кваліфікації — номер сегмента).</param>
/// <param name="TimeSeconds">Час від початку сесії, секунди.</param>
/// <param name="Message">Текст для журналу подій.</param>
/// <param name="Car">Головний учасник події (якщо є).</param>
/// <param name="OtherCar">Другий учасник (наприклад, той, кого обігнали).</param>
public sealed record RaceEvent(
    RaceEventType Type,
    int Lap,
    double TimeSeconds,
    string Message,
    Car? Car = null,
    Car? OtherCar = null)
{
    /// <summary>Підпис часу події для журналу; якщо не задано — «Коло N».</summary>
    public string? Label { get; init; }

    /// <summary>Рядок для журналу у форматі «Коло 12: ...» або «Q2 · 08:15: ...».</summary>
    public override string ToString() => $"{Label ?? "Коло " + Lap}: {Message}";
}
