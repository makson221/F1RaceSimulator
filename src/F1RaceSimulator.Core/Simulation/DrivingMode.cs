namespace F1RaceSimulator.Core.Simulation;

/// <summary>Режим їзди пілота на поточному відрізку.</summary>
public enum DrivingMode
{
    /// <summary>Економія: пілот береже шини, двигун або паливо.</summary>
    Saving,

    /// <summary>Рівний гоночний темп.</summary>
    Normal,

    /// <summary>Атака: болід менш ніж за секунду позаду суперника, віддає енергію ERS.</summary>
    Attack,

    /// <summary>Захист: суперник позаду ближче ніж за секунду, пілот теж використовує ERS.</summary>
    Defend
}
