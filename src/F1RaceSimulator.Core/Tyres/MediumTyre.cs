using F1RaceSimulator.Core.Models;

namespace F1RaceSimulator.Core.Tyres;

/// <summary>Середні шини (Medium): компроміс між швидкістю та ресурсом.</summary>
public sealed class MediumTyre : Tyre
{
    public MediumTyre(double condition = 100) : base(condition) { }

    public override TyreCompound Compound => TyreCompound.Medium;

    public override double WearPerLap => 3.1;

    public override double PaceBonus => 0.0;
}
