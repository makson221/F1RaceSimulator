namespace F1RaceSimulator.Core.Models;

/// <summary>
/// Команда Формули 1. Дані завантажуються з файлу data/teams.json.
/// </summary>
public sealed class Team
{
    /// <summary>Короткий ідентифікатор команди, наприклад "MCL".</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Назва команди.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Колір команди у форматі HTML (#RRGGBB) для таблиці та схеми траси.</summary>
    public string Color { get; init; } = "#888888";

    /// <summary>
    /// Умовний рейтинг швидкості боліда (1.0 — найшвидший болід сезону).
    /// Значення вигадані для навчальної моделі.
    /// </summary>
    public double CarPerformance { get; init; } = 1.0;

    /// <summary>Порядковий номер боксу команди в піт-лейні (1 — найближчий до виїзду).</summary>
    public int PitBoxOrder { get; init; } = 1;

    public override string ToString() => Name;
}
