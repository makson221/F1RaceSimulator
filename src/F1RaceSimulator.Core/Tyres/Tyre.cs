using F1RaceSimulator.Core.Models;

namespace F1RaceSimulator.Core.Tyres;

/// <summary>
/// Абстрактний клас шини — основа ієрархії шин.
/// Демонструє абстракцію (спільний інтерфейс для всіх складів),
/// інкапсуляцію (стан змінюється лише через <see cref="Wear"/>)
/// та поліморфізм (нащадки по-різному визначають знос і темп).
/// </summary>
public abstract class Tyre
{
    /// <summary>Втрата темпу на повністю зношених шинах: 3.5 % (близько 2–3 с на коло).</summary>
    public const double PaceLossWhenWorn = 0.035;

    /// <summary>Створює шину із заданим станом у відсотках.</summary>
    /// <param name="condition">Стан від 0 до 100 %. Нова шина — 100 %.</param>
    protected Tyre(double condition = 100)
    {
        if (double.IsNaN(condition))
            throw new ArgumentException("Стан шини не може бути NaN.", nameof(condition));
        Condition = Math.Clamp(condition, 0, 100);
    }

    /// <summary>Склад шини.</summary>
    public abstract TyreCompound Compound { get; }

    /// <summary>Базовий знос за одне коло у відсотках (на середній трасі, для середнього пілота).</summary>
    public abstract double WearPerLap { get; }

    /// <summary>Бонус до темпу нової шини: +0.006 означає на 0.6 % швидше.</summary>
    public abstract double PaceBonus { get; }

    /// <summary>Поточний стан шини у відсотках (100 — нова, 0 — повністю зношена).</summary>
    public double Condition { get; private set; }

    /// <summary>Скільки кіл проїхано на цьому комплекті.</summary>
    public double LapsUsed { get; private set; }

    /// <summary>Літера складу для таблиці: S, M або H.</summary>
    public string Letter => Compound switch
    {
        TyreCompound.Soft => "S",
        TyreCompound.Medium => "M",
        TyreCompound.Hard => "H",
        _ => "?"
    };

    /// <summary>Назва складу англійською, як у телетрансляціях: Soft, Medium, Hard.</summary>
    public string Name => Compound.ToString();

    /// <summary>
    /// Множник темпу: більше 1 — болід швидший, менше 1 — повільніший.
    /// Віртуальний: нащадок може змінити поведінку (див. <see cref="SoftTyre"/>).
    /// </summary>
    public virtual double PaceMultiplier => 1 + PaceBonus - PaceLossWhenWorn * (1 - Condition / 100);

    /// <summary>
    /// Зношує шину після проїзду певної дистанції.
    /// </summary>
    /// <param name="laps">Проїхана дистанція в колах (може бути дробовою).</param>
    /// <param name="trackFactor">Абразивність траси (<see cref="Track.TyreWearFactor"/>).</param>
    /// <param name="driverFactor">Вплив стилю пілота (див. <c>Car.TyreWearRate</c>).</param>
    public void Wear(double laps, double trackFactor, double driverFactor)
    {
        if (laps <= 0) return;
        LapsUsed += laps;
        Condition = Math.Max(0, Condition - WearPerLap * laps * trackFactor * driverFactor);
    }

    /// <summary>Скільки ще кіл витримає шина до повного зносу за поточних умов.</summary>
    public double EstimatedLapsLeft(double trackFactor, double driverFactor)
    {
        double perLap = WearPerLap * trackFactor * driverFactor;
        return perLap <= 0 ? double.PositiveInfinity : Condition / perLap;
    }

    public override string ToString() => $"{Name} ({Condition:0}%)";
}
