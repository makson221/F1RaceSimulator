namespace F1RaceSimulator.Core.Components;

/// <summary>
/// Силова установка (двигун). Має температуру та знос.
/// Атака і їзда в турбулентному повітрі нагрівають двигун; перегрів прискорює знос,
/// знижує потужність і збільшує ризик відмови. Числа — умовна навчальна модель.
/// </summary>
public sealed class PowerUnit : CarComponent
{
    /// <summary>Температура, вище якої двигун перегрівається, °C.</summary>
    public const double WarningTemperature = 120;

    /// <summary>Критична температура, °C.</summary>
    public const double CriticalTemperature = 130;

    /// <summary>Базовий знос за одне коло, %.</summary>
    public const double WearPerLap = 0.25;

    /// <param name="initialWear">Знос на початку гонки, % (двигун використовується кілька етапів).</param>
    public PowerUnit(double initialWear = 0)
    {
        Wear = Math.Clamp(initialWear, 0, 100);
        Temperature = 90;
    }

    public override string Name => "Двигун";

    /// <summary>Температура охолоджувальної рідини, °C.</summary>
    public double Temperature { get; private set; }

    /// <summary>Знос силової установки, % (100 — ресурс вичерпано).</summary>
    public double Wear { get; private set; }

    public override ComponentStatus Status =>
        Temperature >= CriticalTemperature || Wear >= 90 ? ComponentStatus.Critical :
        Temperature >= WarningTemperature || Wear >= 70 ? ComponentStatus.Warning :
        ComponentStatus.Normal;

    /// <summary>Залишок ресурсу (1 — новий двигун).</summary>
    public override double Level => 1 - Wear / 100;

    public override string Summary => $"{Temperature:0} °C · знос {Wear:0}%";

    /// <summary>Множник темпу: при перегріві електроніка обмежує потужність.</summary>
    public double PaceFactor =>
        Temperature >= CriticalTemperature ? 0.99 :
        Temperature >= WarningTemperature + 5 ? 0.995 :
        1.0;

    /// <summary>У скільки разів зростає ризик технічного сходу через знос і перегрів.</summary>
    public double FailureRiskMultiplier =>
        1 + Math.Pow(Wear / 50, 2) + (Temperature >= CriticalTemperature ? 2 : 0);

    /// <summary>
    /// Температура, до якої прямує двигун за поточних умов: база + спека + навантаження
    /// + турбулентне повітря (гірше охолодження) + знос (зношений двигун гріється сильніше).
    /// </summary>
    public double TargetTemperature(in ComponentContext context)
    {
        if (!context.IsRacing)
            return 80 + 0.35 * context.AirTemperature;                 // у піт-лейні двигун охолоджується
        return 88 + 0.35 * context.AirTemperature + 12 * context.Push
               + (context.InDirtyAir ? 5 : 0) + 0.1 * Wear;
    }

    public override void Update(in ComponentContext context)
    {
        Temperature = Approach(Temperature, TargetTemperature(context), context.Seconds, timeConstant: 25);

        double overheat = Math.Max(0, Temperature - WarningTemperature) / 10;
        Wear = Math.Min(100, Wear + context.Laps * WearPerLap * (1 + overheat));
    }
}
