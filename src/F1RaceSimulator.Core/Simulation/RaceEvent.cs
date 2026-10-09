namespace F1RaceSimulator.Core.Simulation;

/// <summary>Тип події гонки.</summary>
public enum RaceEventType
{
    RaceStarted,
    LapCompleted,
    Overtake,
    PitEntry,
    PitStop,
    Retirement,
    LeaderFinished,
    RaceFinished
}

/// <summary>
/// Подія гонки, яку <see cref="Race"/> надсилає спостерігачам.
/// </summary>
/// <param name="Type">Тип події.</param>
/// <param name="Lap">Коло лідера на момент події.</param>
/// <param name="TimeSeconds">Час від старту, секунди.</param>
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
    /// <summary>Рядок для журналу у форматі «Коло 12: ...».</summary>
    public override string ToString() => $"Коло {Lap}: {Message}";
}
