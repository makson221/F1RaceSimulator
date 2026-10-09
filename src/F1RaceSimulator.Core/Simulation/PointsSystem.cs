namespace F1RaceSimulator.Core.Simulation;

/// <summary>Система нарахування очок Формули 1 (перші десять фінішерів).</summary>
public static class PointsSystem
{
    private static readonly int[] Points = { 25, 18, 15, 12, 10, 8, 6, 4, 2, 1 };

    /// <summary>Очки за фінішну позицію (1..10). За інші позиції та сход — 0.</summary>
    public static int ForPosition(int position) =>
        position >= 1 && position <= Points.Length ? Points[position - 1] : 0;
}
