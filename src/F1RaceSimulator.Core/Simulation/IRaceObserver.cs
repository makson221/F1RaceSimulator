namespace F1RaceSimulator.Core.Simulation;

/// <summary>
/// Спостерігач за гонкою (патерн Observer).
/// Гонка повідомляє всіх підписаних спостерігачів про події,
/// не знаючи, хто вони: журнал, таблиця чи вікно програми.
/// </summary>
public interface IRaceObserver
{
    /// <summary>Викликається для кожної події гонки.</summary>
    void OnRaceEvent(Race race, RaceEvent raceEvent);
}
