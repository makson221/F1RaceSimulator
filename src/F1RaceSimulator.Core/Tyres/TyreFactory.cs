using F1RaceSimulator.Core.Models;

namespace F1RaceSimulator.Core.Tyres;

/// <summary>
/// Фабрика шин (патерн Factory Method).
/// Код, якому потрібна шина, не знає про конкретні класи-нащадки:
/// він просить фабрику створити шину потрібного складу.
/// </summary>
public static class TyreFactory
{
    /// <summary>Створює шину за складом.</summary>
    /// <param name="compound">Склад шини.</param>
    /// <param name="condition">Початковий стан, %. Нова шина — 100.</param>
    public static Tyre Create(TyreCompound compound, double condition = 100) => compound switch
    {
        TyreCompound.Soft => new SoftTyre(condition),
        TyreCompound.Medium => new MediumTyre(condition),
        TyreCompound.Hard => new HardTyre(condition),
        _ => throw new ArgumentOutOfRangeException(nameof(compound), compound, "Невідомий склад шин.")
    };

    /// <summary>
    /// Створює шину за текстовим кодом: "S", "M", "H" або "Soft", "Medium", "Hard".
    /// </summary>
    /// <exception cref="ArgumentException">Якщо код не розпізнано.</exception>
    public static Tyre Create(string code, double condition = 100)
    {
        ArgumentNullException.ThrowIfNull(code);
        return code.Trim().ToUpperInvariant() switch
        {
            "S" or "SOFT" => Create(TyreCompound.Soft, condition),
            "M" or "MEDIUM" => Create(TyreCompound.Medium, condition),
            "H" or "HARD" => Create(TyreCompound.Hard, condition),
            _ => throw new ArgumentException($"Невідомий склад шин: \"{code}\".", nameof(code))
        };
    }

    /// <summary>Базовий знос за коло для складу без створення об'єкта для інтерфейсу.</summary>
    public static double WearPerLapOf(TyreCompound compound) => Create(compound).WearPerLap;
}
