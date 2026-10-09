namespace F1RaceSimulator.Core.Simulation;

/// <summary>Стан боліда під час гонки.</summary>
public enum CarState
{
    /// <summary>Їде трасою.</summary>
    OnTrack,

    /// <summary>Їде піт-лейном з обмеженням швидкості.</summary>
    InPitLane,

    /// <summary>Стоїть у боксі, механіки міняють шини.</summary>
    PitStop,

    /// <summary>Отримав клетчастий прапор.</summary>
    Finished,

    /// <summary>Зійшов з дистанції.</summary>
    Retired
}
