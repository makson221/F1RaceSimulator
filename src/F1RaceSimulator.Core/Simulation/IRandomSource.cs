namespace F1RaceSimulator.Core.Simulation;

/// <summary>
/// Джерело випадкових чисел. Виділене в інтерфейс (принцип інверсії залежностей),
/// щоб у тестах можна було підставити передбачуване джерело.
/// </summary>
public interface IRandomSource
{
    /// <summary>Випадкове число з діапазону [0; 1).</summary>
    double NextDouble();
}

/// <summary>Реалізація на основі <see cref="Random"/>.</summary>
public sealed class SystemRandomSource : IRandomSource
{
    private readonly Random _random;

    /// <param name="seed">Зерно генератора. Однакове зерно дає однакову гонку.</param>
    public SystemRandomSource(int? seed = null)
    {
        _random = seed.HasValue ? new Random(seed.Value) : new Random();
    }

    public double NextDouble() => _random.NextDouble();
}
