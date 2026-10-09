namespace F1RaceSimulator.Core.Strategies;

/// <summary>
/// Агресивна стратегія: заїжджає раніше (поріг на 10 % вищий) і
/// обирає м'якшу гуму, використовуючи майже весь її ресурс.
/// Наслідує <see cref="ThresholdPitStrategy"/> і перевизначає лише параметри.
/// </summary>
public sealed class AggressivePitStrategy : ThresholdPitStrategy
{
    public AggressivePitStrategy(double threshold = 25)
        : base(Math.Min(100, threshold + 10))
    {
    }

    public override string Name => "Агресивна";

    /// <summary>Агресивна стратегія готова «доїздити» шини майже до кінця (95 %).</summary>
    protected override double MaxTyreUsage => 95;
}
