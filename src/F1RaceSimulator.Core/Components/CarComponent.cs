namespace F1RaceSimulator.Core.Components;

/// <summary>Стан компонента боліда для підсвічування в інтерфейсі.</summary>
public enum ComponentStatus
{
    /// <summary>Усе гаразд.</summary>
    Normal,

    /// <summary>Варто звернути увагу (наприклад, перегрів).</summary>
    Warning,

    /// <summary>Критично: втрата темпу або ризик відмови.</summary>
    Critical
}

/// <summary>
/// Умови, в яких працює болід на поточному кроці симуляції.
/// Передається кожному компоненту в метод <see cref="CarComponent.Update"/>.
/// </summary>
/// <param name="Seconds">Тривалість кроку, секунди.</param>
/// <param name="Laps">Пройдена за крок дистанція, кола.</param>
/// <param name="AirTemperature">Температура повітря, °C.</param>
/// <param name="Push">Інтенсивність їзди: 0 — економія, 0.5 — рівний темп, 1 — атака.</param>
/// <param name="InDirtyAir">Болід їде менш ніж за секунду позаду суперника (турбулентне повітря).</param>
/// <param name="IsRacing">Болід їде трасою (false — піт-лейн або бокс).</param>
public readonly record struct ComponentContext(
    double Seconds,
    double Laps,
    double AirTemperature,
    double Push,
    bool InDirtyAir,
    bool IsRacing);

/// <summary>
/// Абстрактний компонент боліда: шини, силова установка, паливний бак, батарея ERS.
/// Кожен компонент сам знає, як змінюється його стан (поліморфний метод <see cref="Update"/>),
/// і як себе показати в інтерфейсі (<see cref="Summary"/>, <see cref="Level"/>, <see cref="Status"/>).
/// </summary>
public abstract class CarComponent
{
    /// <summary>Назва компонента для інтерфейсу.</summary>
    public abstract string Name { get; }

    /// <summary>Поточний стан: норма, увага, критично.</summary>
    public abstract ComponentStatus Status { get; }

    /// <summary>Рівень від 0 до 1 для смужки в інтерфейсі (ресурс, заряд, кількість палива).</summary>
    public abstract double Level { get; }

    /// <summary>Короткий текст зі значеннями, наприклад «103 °C · 64 %».</summary>
    public abstract string Summary { get; }

    /// <summary>Оновлює стан компонента за один крок симуляції.</summary>
    public abstract void Update(in ComponentContext context);

    /// <summary>Наближає значення до цільового з часовою сталою <paramref name="timeConstant"/> секунд.</summary>
    protected static double Approach(double value, double target, double seconds, double timeConstant) =>
        value + (target - value) * Math.Min(1, seconds / timeConstant);

    public override string ToString() => $"{Name}: {Summary}";
}
