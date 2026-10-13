namespace F1RaceSimulator.Core.Components;

/// <summary>
/// Батарея системи рекуперації енергії (ERS). Заряджається під час гальмувань,
/// а в атаці віддає енергію, додаючи боліду швидкості.
/// </summary>
public sealed class ErsBattery : CarComponent
{
    /// <summary>Заряд, що повертається за коло рекуперацією, %.</summary>
    public const double HarvestPerLap = 18;

    /// <summary>Витрата заряду за коло в режимі віддачі, %.</summary>
    public const double DeployPerLap = 45;

    /// <summary>Приріст темпу під час віддачі енергії.</summary>
    public const double DeployBoost = 0.0035;

    /// <summary>Мінімальний заряд, з якого можна почати віддачу, %.</summary>
    public const double MinChargeToDeploy = 40;

    /// <summary>Нижче цього заряду віддача припиняється, %.</summary>
    public const double ReserveCharge = 5;

    public ErsBattery(double charge = 100)
    {
        Charge = Math.Clamp(charge, 0, 100);
    }

    public override string Name => "Батарея ERS";

    /// <summary>Заряд, %.</summary>
    public double Charge { get; private set; }

    /// <summary>Чи віддає батарея енергію зараз.</summary>
    public bool IsDeploying { get; private set; }

    /// <summary>Множник темпу від ERS (1.0035 під час віддачі).</summary>
    public double PaceFactor => IsDeploying ? 1 + DeployBoost : 1;

    public override ComponentStatus Status => Charge < MinChargeToDeploy / 2 ? ComponentStatus.Warning : ComponentStatus.Normal;

    public override double Level => Charge / 100;

    public override string Summary => IsDeploying ? $"{Charge:0}% · віддача" : $"{Charge:0}%";

    public override void Update(in ComponentContext context)
    {
        // Енергію віддають в атаці: почати можна з 40 % заряду, віддача триває до 5 %.
        bool attacking = context.IsRacing && context.Push >= 1;
        IsDeploying = attacking && (IsDeploying ? Charge > ReserveCharge : Charge >= MinChargeToDeploy);

        double delta = HarvestPerLap * context.Laps;
        if (IsDeploying) delta -= DeployPerLap * context.Laps;
        Charge = Math.Clamp(Charge + delta, 0, 100);
    }
}
