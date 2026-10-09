using System.Text.Json;
using F1RaceSimulator.Core.Models;
using F1RaceSimulator.Core.Simulation;

namespace F1RaceSimulator.Core.Season;

/// <summary>Результат одного пілота в одній гонці (зберігається у файлі чемпіонату).</summary>
public sealed class ChampionshipEntry
{
    public string DriverCode { get; set; } = string.Empty;
    public string DriverName { get; set; } = string.Empty;
    public string TeamId { get; set; } = string.Empty;
    public string TeamName { get; set; } = string.Empty;
    public int Position { get; set; }
    public int Points { get; set; }
    public string Status { get; set; } = string.Empty;
}

/// <summary>Результати одного етапу.</summary>
public sealed class CompletedRace
{
    public int Round { get; set; }
    public string GrandPrix { get; set; } = string.Empty;
    public List<ChampionshipEntry> Results { get; set; } = new();
}

/// <summary>Рядок турнірної таблиці.</summary>
public sealed record StandingRow(int Position, string Code, string Name, string Team, int Points, int Wins, int Podiums);

/// <summary>
/// Чемпіонат сезону (патерн Singleton): в програмі існує лише один об'єкт,
/// доступний через <see cref="Instance"/>. Зберігає результати етапів і рахує очки.
/// </summary>
public sealed class Championship
{
    private static readonly Lazy<Championship> LazyInstance = new(() => new Championship());

    private readonly List<CompletedRace> _races = new();

    /// <summary>Приватний конструктор: створити об'єкт ззовні неможливо.</summary>
    private Championship() { }

    /// <summary>Єдиний екземпляр чемпіонату.</summary>
    public static Championship Instance => LazyInstance.Value;

    /// <summary>Завершені етапи в порядку календаря.</summary>
    public IReadOnlyList<CompletedRace> Races => _races;

    /// <summary>Виникає після будь-якої зміни результатів.</summary>
    public event EventHandler? Changed;

    /// <summary>
    /// Додає результати гонки. Якщо етап уже був зіграний, його результати замінюються.
    /// </summary>
    public void AddRaceResult(Track track, IReadOnlyList<RaceResult> results)
    {
        ArgumentNullException.ThrowIfNull(track);
        ArgumentNullException.ThrowIfNull(results);

        _races.RemoveAll(r => r.Round == track.Round);
        _races.Add(new CompletedRace
        {
            Round = track.Round,
            GrandPrix = track.GrandPrix,
            Results = results.Select(r => new ChampionshipEntry
            {
                DriverCode = r.Car.Driver.Code,
                DriverName = r.Car.Driver.FullName,
                TeamId = r.Car.Team.Id,
                TeamName = r.Car.Team.Name,
                Position = r.Position,
                Points = r.Points,
                Status = r.Status
            }).ToList()
        });
        _races.Sort((a, b) => a.Round.CompareTo(b.Round));
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Таблиця пілотів: сортування за очками, потім за кількістю перемог.</summary>
    public IReadOnlyList<StandingRow> GetDriverStandings()
    {
        var rows = _races
            .SelectMany(r => r.Results)
            .GroupBy(e => e.DriverCode)
            .Select(g => new
            {
                Code = g.Key,
                Name = g.Last().DriverName,
                Team = g.Last().TeamName,
                Points = g.Sum(e => e.Points),
                Wins = g.Count(e => e.Position == 1 && e.Points > 0),
                Podiums = g.Count(e => e.Position <= 3 && e.Points > 0)
            })
            .OrderByDescending(x => x.Points)
            .ThenByDescending(x => x.Wins)
            .ThenBy(x => x.Name)
            .ToList();

        return rows.Select((x, i) => new StandingRow(i + 1, x.Code, x.Name, x.Team, x.Points, x.Wins, x.Podiums)).ToList();
    }

    /// <summary>Кубок конструкторів: сума очок обох пілотів команди.</summary>
    public IReadOnlyList<StandingRow> GetTeamStandings()
    {
        var rows = _races
            .SelectMany(r => r.Results)
            .GroupBy(e => e.TeamId)
            .Select(g => new
            {
                Code = g.Key,
                Name = g.Last().TeamName,
                Points = g.Sum(e => e.Points),
                Wins = g.Count(e => e.Position == 1 && e.Points > 0),
                Podiums = g.Count(e => e.Position <= 3 && e.Points > 0)
            })
            .OrderByDescending(x => x.Points)
            .ThenByDescending(x => x.Wins)
            .ThenBy(x => x.Name)
            .ToList();

        return rows.Select((x, i) => new StandingRow(i + 1, x.Code, x.Name, x.Name, x.Points, x.Wins, x.Podiums)).ToList();
    }

    /// <summary>Очки пілота за весь сезон.</summary>
    public int PointsOf(string driverCode) =>
        _races.SelectMany(r => r.Results).Where(e => e.DriverCode == driverCode).Sum(e => e.Points);

    /// <summary>Зберігає чемпіонат у файл JSON.</summary>
    public void Save(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        string json = JsonSerializer.Serialize(_races, DataLoader.JsonOptions);
        File.WriteAllText(path, json);
    }

    /// <summary>Завантажує чемпіонат з файлу JSON, замінюючи поточні результати.</summary>
    /// <exception cref="InvalidDataException">Якщо файл пошкоджений.</exception>
    public void Load(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        if (!File.Exists(path))
            throw new FileNotFoundException($"Файл не знайдено: {path}", path);

        List<CompletedRace>? races;
        try
        {
            races = JsonSerializer.Deserialize<List<CompletedRace>>(File.ReadAllText(path), DataLoader.JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException("Файл чемпіонату пошкоджений: " + ex.Message, ex);
        }

        _races.Clear();
        if (races != null) _races.AddRange(races.OrderBy(r => r.Round));
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Очищає всі результати сезону.</summary>
    public void Reset()
    {
        _races.Clear();
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
