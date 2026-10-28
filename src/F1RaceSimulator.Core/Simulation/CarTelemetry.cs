namespace F1RaceSimulator.Core.Simulation;

/// <summary>
/// Миттєва телеметрія боліда: швидкість, передача, оберти, газ і гальмо.
/// Значення беруться з профілю траси (<see cref="Models.SpeedProfile"/>)
/// і масштабуються під темп конкретного боліда.
/// </summary>
/// <param name="SpeedKmh">Швидкість, км/год.</param>
/// <param name="Gear">Передача: 0 — нейтраль, 1–8.</param>
/// <param name="Rpm">Оберти двигуна, об/хв.</param>
/// <param name="Throttle">Педаль газу, %.</param>
/// <param name="Brake">Чи натиснуте гальмо.</param>
public readonly record struct CarTelemetry(double SpeedKmh, int Gear, int Rpm, int Throttle, bool Brake)
{
    /// <summary>Болід стоїть (на решітці, у боксі чи в парк-ферме).</summary>
    public static CarTelemetry Stopped => new(0, 0, 0, 0, false);

    /// <summary>Рух піт-лейном з обмежувачем швидкості 80 км/год.</summary>
    public static CarTelemetry PitLimiter => new(80, 3, 8500, 35, false);

    /// <summary>Передача словом: «N» для нейтралі.</summary>
    public string GearText => Gear <= 0 ? "N" : Gear.ToString();
}
