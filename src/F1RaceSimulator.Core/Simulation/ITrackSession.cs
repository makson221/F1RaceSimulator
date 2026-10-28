using F1RaceSimulator.Core.Models;

namespace F1RaceSimulator.Core.Simulation;

/// <summary>
/// Положення боліда для малювання на схемі траси.
/// </summary>
/// <param name="Car">Болід.</param>
/// <param name="Fraction">Частка кола (на трасі) або частка піт-лейну (у піт-лейні).</param>
/// <param name="InPitLane">true — болід у піт-лейні (координату брати з піт-лейну).</param>
/// <param name="Label">Підпис біля боліда (код пілота, «PIT» тощо) або null.</param>
/// <param name="Highlight">Виділити підпис (лідер, обраний пілот).</param>
public readonly record struct CarPosition(Car Car, double Fraction, bool InPitLane, string? Label, bool Highlight);

/// <summary>
/// Сесія на трасі — кваліфікація або гонка. Інтерфейс дозволяє одному класу
/// малювання (<c>TrackRenderer</c>) показувати будь-яку сесію (поліморфізм через інтерфейс).
/// </summary>
public interface ITrackSession
{
    /// <summary>Траса сесії.</summary>
    Track Track { get; }

    /// <summary>Час від початку сесії, секунди.</summary>
    double ElapsedSeconds { get; }

    /// <summary>Сесію повністю завершено.</summary>
    bool IsCompleted { get; }

    /// <summary>Боліди, які зараз треба показати на схемі, від останнього до першого.</summary>
    IReadOnlyList<CarPosition> GetCarPositions();

    /// <summary>Просуває сесію на <paramref name="seconds"/> секунд.</summary>
    void Step(double seconds);
}
