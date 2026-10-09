namespace F1RaceSimulator.Core.Simulation;

/// <summary>Рядок фінішного протоколу гонки.</summary>
/// <param name="Position">Фінішна позиція.</param>
/// <param name="Car">Болід.</param>
/// <param name="Laps">Кількість пройдених кіл.</param>
/// <param name="Status">"Фініш", "+1 коло" або причина сходу.</param>
/// <param name="Points">Нараховані очки.</param>
public sealed record RaceResult(int Position, Car Car, int Laps, string Status, int Points);
