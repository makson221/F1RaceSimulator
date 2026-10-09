namespace F1RaceSimulator.Core.Strategies;

/// <summary>Створює об'єкт стратегії за назвою з файлу drivers.json.</summary>
public static class PitStrategyFactory
{
    /// <param name="type">"threshold" (обережна) або "aggressive" (агресивна).</param>
    /// <param name="threshold">Поріг стану шин, %.</param>
    public static IPitStrategy Create(string? type, double threshold) =>
        (type ?? "threshold").Trim().ToLowerInvariant() switch
        {
            "aggressive" => new AggressivePitStrategy(threshold),
            "threshold" or "" => new ThresholdPitStrategy(threshold),
            _ => throw new ArgumentException($"Невідомий тип стратегії: \"{type}\".", nameof(type))
        };
}
