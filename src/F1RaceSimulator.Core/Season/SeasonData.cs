using F1RaceSimulator.Core.Models;

namespace F1RaceSimulator.Core.Season;

/// <summary>
/// Дані сезону: команди, пілоти та траси календаря.
/// </summary>
public sealed class SeasonData
{
    private readonly Dictionary<string, Team> _teamsById;

    public SeasonData(IReadOnlyList<Team> teams, IReadOnlyList<Driver> drivers, IReadOnlyList<Track> tracks)
    {
        Teams = teams ?? throw new ArgumentNullException(nameof(teams));
        Drivers = drivers ?? throw new ArgumentNullException(nameof(drivers));
        Tracks = (tracks ?? throw new ArgumentNullException(nameof(tracks))).OrderBy(t => t.Round).ToList();
        _teamsById = teams.ToDictionary(t => t.Id, StringComparer.OrdinalIgnoreCase);
        Validate();
    }

    public IReadOnlyList<Team> Teams { get; }

    public IReadOnlyList<Driver> Drivers { get; }

    /// <summary>Траси в порядку календаря.</summary>
    public IReadOnlyList<Track> Tracks { get; }

    /// <summary>Команда за ідентифікатором.</summary>
    /// <exception cref="KeyNotFoundException">Якщо команду не знайдено.</exception>
    public Team GetTeam(string id) =>
        _teamsById.TryGetValue(id, out var team)
            ? team
            : throw new KeyNotFoundException($"Команду \"{id}\" не знайдено.");

    /// <summary>Пілоти вказаної команди.</summary>
    public IEnumerable<Driver> DriversOf(Team team) =>
        Drivers.Where(d => string.Equals(d.TeamId, team.Id, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Найближчий етап, що ще не відбувся (за датою), або останній етап сезону.
    /// </summary>
    public Track NextTrack(DateOnly today) =>
        Tracks.FirstOrDefault(t => t.RaceDate is { } d && d >= today) ?? Tracks[^1];

    /// <summary>Наступний етап календаря після <paramref name="track"/> (null — сезон завершено).</summary>
    public Track? NextTrackAfter(Track track)
    {
        ArgumentNullException.ThrowIfNull(track);
        return Tracks.FirstOrDefault(t => t.Round > track.Round);
    }

    /// <summary>Перевіряє цілісність даних: посилання на команди, унікальність кодів, наявність геометрії.</summary>
    private void Validate()
    {
        if (Teams.Count == 0) throw new InvalidDataException("Список команд порожній.");
        if (Drivers.Count == 0) throw new InvalidDataException("Список пілотів порожній.");
        if (Tracks.Count == 0) throw new InvalidDataException("Список трас порожній.");

        foreach (var driver in Drivers)
        {
            if (!_teamsById.ContainsKey(driver.TeamId))
                throw new InvalidDataException(
                    $"Пілот {driver.Code} посилається на неіснуючу команду \"{driver.TeamId}\".");
        }

        var duplicate = Drivers.GroupBy(d => d.Code).FirstOrDefault(g => g.Count() > 1);
        if (duplicate != null)
            throw new InvalidDataException($"Код пілота \"{duplicate.Key}\" повторюється.");

        foreach (var track in Tracks)
        {
            if (track.Layout.Points.Length < 3 || track.Layout.PitLane.Length < 2)
                throw new InvalidDataException($"Для траси \"{track.Id}\" не задано схему.");
            if (track.Laps <= 0 || track.TypicalLapSeconds <= 0)
                throw new InvalidDataException($"Для траси \"{track.Id}\" невірно задано кількість кіл або час кола.");
        }
    }
}
