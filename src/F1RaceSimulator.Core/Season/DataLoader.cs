using System.Text.Json;
using F1RaceSimulator.Core.Models;

namespace F1RaceSimulator.Core.Season;

/// <summary>
/// Завантажує дані сезону з файлів JSON (teams.json, drivers.json, tracks.json).
/// Відповідає лише за читання файлів (принцип єдиної відповідальності).
/// </summary>
public static class DataLoader
{
    public const string TeamsFile = "teams.json";
    public const string DriversFile = "drivers.json";
    public const string TracksFile = "tracks.json";

    /// <summary>Налаштування серіалізації: назви полів у JSON — camelCase.</summary>
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true
    };

    /// <summary>Завантажує всі дані сезону з указаної папки.</summary>
    /// <exception cref="FileNotFoundException">Якщо якогось файлу немає.</exception>
    /// <exception cref="InvalidDataException">Якщо файл пошкоджений або дані неузгоджені.</exception>
    public static SeasonData LoadSeason(string dataDirectory)
    {
        ArgumentException.ThrowIfNullOrEmpty(dataDirectory);
        if (!Directory.Exists(dataDirectory))
            throw new DirectoryNotFoundException($"Не знайдено папку з даними: {dataDirectory}");

        var teams = LoadList<Team>(Path.Combine(dataDirectory, TeamsFile));
        var drivers = LoadList<Driver>(Path.Combine(dataDirectory, DriversFile));
        var tracks = LoadList<Track>(Path.Combine(dataDirectory, TracksFile));
        return new SeasonData(teams, drivers, tracks);
    }

    /// <summary>Читає список об'єктів типу <typeparamref name="T"/> з файлу JSON.</summary>
    public static List<T> LoadList<T>(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Не знайдено файл даних: {path}", path);

        try
        {
            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<List<T>>(json, JsonOptions)
                   ?? throw new InvalidDataException($"Файл {Path.GetFileName(path)} порожній.");
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException(
                $"Помилка у файлі {Path.GetFileName(path)}: рядок {ex.LineNumber + 1}. {ex.Message}", ex);
        }
    }
}
