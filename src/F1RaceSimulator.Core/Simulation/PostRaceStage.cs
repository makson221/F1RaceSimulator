namespace F1RaceSimulator.Core.Simulation;

/// <summary>
/// Що робить болід після клетчастого прапора. Як у реальній гонці, болід не зупиняється
/// на лінії фінішу: він проїжджає коло охолодження і заїжджає в парк-ферме (піт-лейн).
/// </summary>
public enum PostRaceStage
{
    /// <summary>Болід ще не фінішував.</summary>
    None,

    /// <summary>Повільне коло охолодження після фінішу.</summary>
    CoolDownLap,

    /// <summary>Заїзд піт-лейном до парк-ферме.</summary>
    PitLane,

    /// <summary>Болід стоїть у парк-ферме.</summary>
    Parked
}
