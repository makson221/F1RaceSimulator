using F1RaceSimulator.Core.Components;
using F1RaceSimulator.Core.Models;

namespace F1RaceSimulator.Core.Tyres;

/// <summary>
/// Абстрактний клас шини — основа ієрархії шин і один із компонентів боліда.
/// Демонструє абстракцію (спільний інтерфейс для всіх складів),
/// інкапсуляцію (стан змінюється лише через <see cref="Wear"/> і <see cref="Update"/>)
/// та поліморфізм (нащадки по-різному визначають знос, робочу температуру і темп).
/// </summary>
public abstract class Tyre : CarComponent
{
    /// <summary>Втрата темпу на повністю зношених шинах: 3.5 % (близько 2–3 с на коло).</summary>
    public const double PaceLossWhenWorn = 0.035;

    /// <summary>Температура шин після підігрівальних ковдр, °C.</summary>
    public const double BlanketTemperature = 70;

    /// <summary>Втрата темпу за кожен градус поза робочим діапазоном.</summary>
    public const double PaceLossPerDegree = 0.0004;

    /// <summary>Створює шину із заданим станом у відсотках.</summary>
    /// <param name="condition">Стан від 0 до 100 %. Нова шина — 100 %.</param>
    protected Tyre(double condition = 100)
    {
        if (double.IsNaN(condition))
            throw new ArgumentException("Стан шини не може бути NaN.", nameof(condition));
        Condition = Math.Clamp(condition, 0, 100);
        Temperature = BlanketTemperature;
    }

    /// <summary>Склад шини.</summary>
    public abstract TyreCompound Compound { get; }

    /// <summary>Базовий знос за одне коло у відсотках (на середній трасі, для середнього пілота).</summary>
    public abstract double WearPerLap { get; }

    /// <summary>Бонус до темпу нової шини: +0.006 означає на 0.6 % швидше.</summary>
    public abstract double PaceBonus { get; }

    /// <summary>Нижня межа робочої температури складу, °C.</summary>
    public abstract double OptimalMinTemperature { get; }

    /// <summary>Верхня межа робочої температури складу, °C.</summary>
    public abstract double OptimalMaxTemperature { get; }

    /// <summary>Наскільки склад гріється сильніше (+) чи слабше (−) за середній, °C.</summary>
    protected abstract double HeatingBias { get; }

    /// <summary>Поточний стан шини у відсотках (100 — нова, 0 — повністю зношена).</summary>
    public double Condition { get; private set; }

    /// <summary>Температура протектора, °C.</summary>
    public double Temperature { get; private set; }

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
    public string CompoundName => Compound.ToString();

    /// <summary>Назва компонента для картки боліда.</summary>
    public override string Name => "Шини";

    /// <summary>На скільки градусів температура виходить за робочий діапазон (0 — у діапазоні).</summary>
    public double DegreesOutsideWindow =>
        Temperature < OptimalMinTemperature ? OptimalMinTemperature - Temperature :
        Temperature > OptimalMaxTemperature ? Temperature - OptimalMaxTemperature :
        0;

    /// <summary>Шини перегріті.</summary>
    public bool IsOverheating => Temperature > OptimalMaxTemperature;

    /// <summary>Втрата темпу через холодні або перегріті шини.</summary>
    public double TemperaturePenalty => PaceLossPerDegree * DegreesOutsideWindow;

    /// <summary>Множник зносу: перегріті шини зношуються швидше.</summary>
    public double OverheatWearFactor => 1 + Math.Max(0, Temperature - OptimalMaxTemperature) / 15;

    /// <summary>
    /// Множник темпу: більше 1 — болід швидший, менше 1 — повільніший.
    /// Враховує склад, знос і температуру. Віртуальний: нащадок може змінити поведінку (див. <see cref="SoftTyre"/>).
    /// </summary>
    public virtual double PaceMultiplier =>
        1 + PaceBonus - PaceLossWhenWorn * (1 - Condition / 100) - TemperaturePenalty;

    /// <summary>Стан для картки: критично — шини майже зношені або сильно перегріті; холодні шини — попередження.</summary>
    public override ComponentStatus Status =>
        Condition < 20 || Temperature > OptimalMaxTemperature + 10 ? ComponentStatus.Critical :
        Condition < 50 || DegreesOutsideWindow > 0 ? ComponentStatus.Warning :
        ComponentStatus.Normal;

    public override double Level => Condition / 100;

    public override string Summary => $"{Letter} {Condition:0}% · {Temperature:0} °C";

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
        Condition = Math.Max(0, Condition - WearPerLap * laps * trackFactor * driverFactor * OverheatWearFactor);
    }

    /// <summary>
    /// Оновлює температуру шини: вона прямує до цільової, яка залежить від погоди,
    /// інтенсивності їзди, турбулентного повітря і складу. Нові шини прогріваються приблизно за коло.
    /// </summary>
    public override void Update(in ComponentContext context)
    {
        if (!context.IsRacing) return;                // у піт-лейні температура майже не змінюється
        Temperature = Approach(Temperature, TargetTemperature(context), context.Seconds, timeConstant: 35);
    }

    /// <summary>
    /// Температура, до якої прямує шина: температура асфальту (повітря + 12 °C) + тертя
    /// + інтенсивність їзди + ковзання в турбулентному повітрі + поправка складу.
    /// </summary>
    public double TargetTemperature(in ComponentContext context)
    {
        double trackTemperature = context.AirTemperature + 12;
        return trackTemperature + 50 + 14 * context.Push + (context.InDirtyAir ? 6 : 0) + HeatingBias;
    }

    /// <summary>Скільки ще кіл витримає шина до повного зносу за поточних умов.</summary>
    public double EstimatedLapsLeft(double trackFactor, double driverFactor)
    {
        double perLap = WearPerLap * trackFactor * driverFactor;
        return perLap <= 0 ? double.PositiveInfinity : Condition / perLap;
    }

    public override string ToString() => $"{CompoundName} ({Condition:0}%, {Temperature:0} °C)";
}
