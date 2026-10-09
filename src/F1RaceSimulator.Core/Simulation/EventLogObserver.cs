namespace F1RaceSimulator.Core.Simulation;

/// <summary>
/// Спостерігач, що веде журнал подій гонки.
/// Інтерфейс програми підписується на його подію <see cref="EventAdded"/>.
/// </summary>
public sealed class EventLogObserver : IRaceObserver
{
    private readonly List<RaceEvent> _events = new();

    /// <summary>Чи записувати в журнал проходження кожного кола (за замовчуванням ні — їх забагато).</summary>
    public bool IncludeLapEvents { get; set; }

    /// <summary>Усі записані події в порядку появи.</summary>
    public IReadOnlyList<RaceEvent> Events => _events;

    /// <summary>Виникає після додавання нової події до журналу.</summary>
    public event EventHandler<RaceEvent>? EventAdded;

    public void OnRaceEvent(Race race, RaceEvent raceEvent)
    {
        if (raceEvent.Type == RaceEventType.LapCompleted && !IncludeLapEvents) return;
        _events.Add(raceEvent);
        EventAdded?.Invoke(this, raceEvent);
    }

    /// <summary>Кількість подій заданого типу.</summary>
    public int Count(RaceEventType type) => _events.Count(e => e.Type == type);

    public void Clear() => _events.Clear();
}
