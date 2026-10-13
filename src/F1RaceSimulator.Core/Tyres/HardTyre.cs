using F1RaceSimulator.Core.Models;

namespace F1RaceSimulator.Core.Tyres;

/// <summary>Жорсткі шини (Hard): найповільніші, але з найбільшим ресурсом.</summary>
public sealed class HardTyre : Tyre
{
    public HardTyre(double condition = 100) : base(condition) { }

    public override TyreCompound Compound => TyreCompound.Hard;

    public override double WearPerLap => 2.1;

    public override double PaceBonus => -0.005;

    /// <summary>Робочий діапазон температури: 90–116 °C (умовна модель).</summary>
    public override double OptimalMinTemperature => 90;

    public override double OptimalMaxTemperature => 116;

    /// <summary>Жорсткий склад гріється на 4 °C слабше за середній, тому в холод важко виходить на робочу температуру.</summary>
    protected override double HeatingBias => -4;
}
