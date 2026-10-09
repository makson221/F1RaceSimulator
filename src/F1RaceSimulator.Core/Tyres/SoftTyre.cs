using F1RaceSimulator.Core.Models;

namespace F1RaceSimulator.Core.Tyres;

/// <summary>
/// М'які шини (Soft): найшвидші, але найшвидше зношуються.
/// Мають «обрив» (cliff): нижче 20 % темп падає різкіше, ніж у інших складів.
/// </summary>
public sealed class SoftTyre : Tyre
{
    /// <summary>Поріг стану, нижче якого починається «обрив» темпу.</summary>
    public const double CliffThreshold = 20;

    public SoftTyre(double condition = 100) : base(condition) { }

    public override TyreCompound Compound => TyreCompound.Soft;

    public override double WearPerLap => 4.6;

    public override double PaceBonus => 0.006;

    /// <summary>
    /// Перевизначений множник темпу: додаткова втрата 1 % при стані нижче 20 %.
    /// Приклад поліморфізму: той самий виклик дає іншу поведінку.
    /// </summary>
    public override double PaceMultiplier =>
        Condition < CliffThreshold ? base.PaceMultiplier - 0.01 : base.PaceMultiplier;
}
