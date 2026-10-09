using F1RaceSimulator.Core.Models;
using F1RaceSimulator.Core.Simulation;

namespace F1RaceSimulator.Core.Strategies;

/// <summary>
/// Стратегія піт-стопів (патерн Strategy).
/// Кожен пілот має власний об'єкт стратегії; гонка звертається до нього
/// через цей інтерфейс і не знає, як саме ухвалюється рішення.
/// </summary>
public interface IPitStrategy
{
    /// <summary>Назва стратегії для інтерфейсу.</summary>
    string Name { get; }

    /// <summary>Поріг стану шин (%), нижче якого стратегія розглядає піт-стоп.</summary>
    double Threshold { get; }

    /// <summary>Чи заїжджати на піт-стоп на цьому колі.</summary>
    /// <param name="car">Болід, для якого ухвалюється рішення.</param>
    /// <param name="lapsRemaining">Скільки кіл залишилося проїхати цьому боліду.</param>
    /// <param name="track">Поточна траса.</param>
    bool ShouldPit(Car car, int lapsRemaining, Track track);

    /// <summary>Який склад шин поставити під час піт-стопу.</summary>
    TyreCompound ChooseCompound(Car car, int lapsRemaining, Track track);
}
