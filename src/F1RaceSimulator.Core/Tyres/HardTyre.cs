using F1RaceSimulator.Core.Models;

namespace F1RaceSimulator.Core.Tyres;

/// <summary>Жорсткі шини (Hard): найповільніші, але з найбільшим ресурсом.</summary>
public sealed class HardTyre : Tyre
{
    public HardTyre(double condition = 100) : base(condition) { }

    public override TyreCompound Compound => TyreCompound.Hard;

    public override double WearPerLap => 2.1;

    public override double PaceBonus => -0.005;
}
