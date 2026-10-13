using F1RaceSimulator.Core.Models;

namespace F1RaceSimulator.Core.Tyres;

/// <summary>Середні шини (Medium): компроміс між швидкістю та ресурсом.</summary>
public sealed class MediumTyre : Tyre
{
    public MediumTyre(double condition = 100) : base(condition) { }

    public override TyreCompound Compound => TyreCompound.Medium;

    public override double WearPerLap => 3.1;

    public override double PaceBonus => 0.0;

    /// <summary>Робочий діапазон температури: 90–112 °C (умовна модель).</summary>
    public override double OptimalMinTemperature => 90;

    public override double OptimalMaxTemperature => 112;

    /// <summary>Середній склад — точка відліку для нагріву.</summary>
    protected override double HeatingBias => 0;
}
