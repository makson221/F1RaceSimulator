namespace F1RaceSimulator.Core.Simulation;

/// <summary>
/// Спостерігач за кваліфікацією (патерн Observer, як <see cref="IRaceObserver"/> для гонки).
/// </summary>
public interface IQualifyingObserver
{
    /// <summary>Викликається для кожної події кваліфікації.</summary>
    void OnQualifyingEvent(QualifyingSession session, RaceEvent qualifyingEvent);
}
