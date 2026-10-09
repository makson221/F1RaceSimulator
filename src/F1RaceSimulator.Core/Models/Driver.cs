namespace F1RaceSimulator.Core.Models;

/// <summary>
/// Пілот Формули 1. Дані завантажуються з файлу data/drivers.json.
/// Характеристики (Pace, Consistency, TyreManagement) — умовні рейтинги
/// для навчальної моделі, а не офіційна статистика.
/// </summary>
public sealed class Driver
{
    /// <summary>Трилітерний код пілота, наприклад "VER".</summary>
    public string Code { get; init; } = string.Empty;

    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    /// <summary>Ідентифікатор команди (посилання на <see cref="Team.Id"/>).</summary>
    public string TeamId { get; init; } = string.Empty;

    /// <summary>Швидкість пілота: 1.0 — найшвидший, 0.98 — повільніший на кілька десятих.</summary>
    public double Pace { get; init; } = 0.99;

    /// <summary>Стабільність від 0 до 1: чим вища, тим менше коливання темпу і ризик сходу.</summary>
    public double Consistency { get; init; } = 0.9;

    /// <summary>Дбайливість до шин від 0 до 1: чим вища, тим повільніше зношується гума.</summary>
    public double TyreManagement { get; init; } = 0.9;

    /// <summary>Тип стратегії піт-стопів: "threshold" або "aggressive".</summary>
    public string Strategy { get; init; } = "threshold";

    /// <summary>Поріг стану шин у відсотках, нижче якого пілот планує піт-стоп.</summary>
    public double PitThreshold { get; init; } = 25;

    /// <summary>Повне ім'я, наприклад "Макс Ферстаппен".</summary>
    public string FullName => $"{FirstName} {LastName}";

    public override string ToString() => FullName;
}
