using F1RaceSimulator.Core.Components;
using F1RaceSimulator.Core.Models;
using F1RaceSimulator.Core.Strategies;
using F1RaceSimulator.Core.Tyres;

namespace F1RaceSimulator.Core.Simulation;

/// <summary>
/// Болід пілота під час гонки.
/// Приклад інкапсуляції: дистанція, шини, стан і кількість піт-стопів
/// мають приватні сеттери і змінюються лише через методи
/// <see cref="Advance"/>, <see cref="ChangeTyres"/>, <see cref="EnterPitLane"/> тощо.
/// </summary>
public sealed class Car
{
    private readonly List<TyreCompound> _usedCompounds = new();
    private double _pitSpan;
    private double _boxPosition;
    private bool _stopDone;

    public Car(Driver driver, Team team, Tyre startTyre, IPitStrategy strategy)
    {
        Driver = driver ?? throw new ArgumentNullException(nameof(driver));
        Team = team ?? throw new ArgumentNullException(nameof(team));
        Tyre = startTyre ?? throw new ArgumentNullException(nameof(startTyre));
        Strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
        _usedCompounds.Add(startTyre.Compound);
        Engine = new PowerUnit();
        Fuel = new FuelTank();
        Battery = new ErsBattery();
    }

    public Driver Driver { get; }

    public Team Team { get; }

    /// <summary>Поточний комплект шин.</summary>
    public Tyre Tyre { get; private set; }

    /// <summary>Стратегія піт-стопів (можна замінити під час гонки).</summary>
    public IPitStrategy Strategy { get; set; }

    /// <summary>Силова установка: температура і знос.</summary>
    public PowerUnit Engine { get; private set; }

    /// <summary>Паливний бак.</summary>
    public FuelTank Fuel { get; }

    /// <summary>Батарея системи рекуперації енергії.</summary>
    public ErsBattery Battery { get; }

    /// <summary>
    /// Усі компоненти боліда. Гонка оновлює їх однаковим викликом <see cref="CarComponent.Update"/>,
    /// а кожен компонент реагує по-своєму (поліморфізм).
    /// </summary>
    public IReadOnlyList<CarComponent> Components => new CarComponent[] { Tyre, Engine, Fuel, Battery };

    /// <summary>Поточний режим їзди: економія, рівний темп або атака.</summary>
    public DrivingMode Mode { get; internal set; } = DrivingMode.Normal;

    /// <summary>Інтенсивність їзди для поточного режиму (0.25 — економія, 0.5 — рівний темп, 1 — боротьба).</summary>
    public double Push => Mode switch
    {
        DrivingMode.Saving => 0.25,
        DrivingMode.Attack or DrivingMode.Defend => 1.0,
        _ => 0.5
    };

    /// <summary>
    /// Вплив режиму на темп: економія (lift and coast) коштує 0.4 %, боротьба додає 0.05 %.
    /// Головний приріст в атаці й захисті дає батарея ERS (<see cref="ErsBattery.PaceFactor"/>).
    /// </summary>
    public double ModePaceFactor => Mode switch
    {
        DrivingMode.Saving => 0.996,
        DrivingMode.Attack or DrivingMode.Defend => 1.0005,
        _ => 1.0
    };

    /// <summary>Болід їде менш ніж за секунду позаду суперника (турбулентне повітря).</summary>
    public bool InDirtyAir { get; internal set; }

    public CarState State { get; private set; } = CarState.OnTrack;

    /// <summary>Миттєва телеметрія: швидкість, передача, оберти, газ, гальмо.</summary>
    public CarTelemetry Telemetry { get; internal set; } = CarTelemetry.Stopped;

    /// <summary>Хронометраж: поточне, останнє і найкраще коло, сектори.</summary>
    public LapTimer Timing { get; } = new();

    /// <summary>Етап після фінішу: коло охолодження, піт-лейн, парк-ферме.</summary>
    public PostRaceStage PostRace { get; private set; } = PostRaceStage.None;

    /// <summary>Скільки кола проїхано після фінішу (коло охолодження).</summary>
    public double CoolDownDistance { get; private set; }

    /// <summary>Прогрес у піт-лейні на шляху до парк-ферме (0..1).</summary>
    public double ParcFermeProgress { get; private set; }

    /// <summary>Положення на колі під час кола охолодження (частка 0..1).</summary>
    public double CoolDownFraction => Geometry.Polyline.Wrap(Distance + CoolDownDistance);

    /// <summary>
    /// Пройдена дистанція в колах від лінії старту. Наприклад, 12.4 — пройдено 12 кіл і 40 % тринадцятого.
    /// На стартовій решітці значення від'ємне (болід стоїть позаду лінії).
    /// </summary>
    public double Distance { get; private set; }

    /// <summary>Скільки разів болід міняв шини.</summary>
    public int PitStops { get; private set; }

    /// <summary>Поточна позиція в гонці (1 — лідер).</summary>
    public int Position { get; internal set; }

    /// <summary>Позиція на старті (за результатами кваліфікації).</summary>
    public int GridPosition { get; internal set; }

    /// <summary>Прогрес у піт-лейні від 0 (в'їзд) до 1 (виїзд).</summary>
    public double PitLaneProgress { get; private set; }

    /// <summary>Скільки секунд ще триває зупинка в боксі.</summary>
    public double StopTimeRemaining { get; private set; }

    /// <summary>Тривалість останньої зупинки в боксі, секунди.</summary>
    public double LastStopDuration { get; private set; }

    /// <summary>Дистанція в момент в'їзду в піт-лейн.</summary>
    public double PitEntryDistance { get; private set; }

    /// <summary>Причина сходу (якщо болід зійшов).</summary>
    public string? RetirementReason { get; private set; }

    /// <summary>Час фінішу від старту гонки, секунди (якщо фінішував).</summary>
    public double? FinishTime { get; private set; }

    /// <summary>Кількість кіл, з якою болід фінішував.</summary>
    public int FinishedLaps { get; private set; }

    /// <summary>Склади шин, використані в цій гонці.</summary>
    public IReadOnlyList<TyreCompound> UsedCompounds => _usedCompounds;

    /// <summary>Чи виконано правило двох різних складів шин.</summary>
    public bool HasUsedTwoCompounds => _usedCompounds.Distinct().Count() >= 2;

    /// <summary>Чи бере болід участь у гонці (не фінішував і не зійшов).</summary>
    public bool IsRunning => State is CarState.OnTrack or CarState.InPitLane or CarState.PitStop;

    /// <summary>Чи знаходиться болід у піт-лейні або в боксі.</summary>
    public bool IsInPits => State is CarState.InPitLane or CarState.PitStop;

    /// <summary>Кількість повністю пройдених кіл.</summary>
    public int CompletedLaps => (int)Math.Floor(Math.Max(0, RaceDistance));

    /// <summary>
    /// Дистанція для порівняння позицій. У піт-лейні болід «просувається»
    /// з такою ж часткою, з якою проходить піт-лейн.
    /// </summary>
    public double RaceDistance => IsInPits ? PitEntryDistance + _pitSpan * PitLaneProgress : Distance;

    /// <summary>
    /// Множник зносу шин залежно від стилю пілота:
    /// дбайливий пілот (TyreManagement = 0.96) зношує гуму на ~9 % повільніше за середнього.
    /// </summary>
    public double TyreWearRate => 1.25 - 0.35 * Driver.TyreManagement;

    /// <summary>
    /// Базовий темп боліда (1.0 — найшвидший). Половину різниці дає пілот, половину — болід.
    /// </summary>
    public double BasePace => (1 - (1 - Driver.Pace) * 0.5) * (1 - (1 - Team.CarPerformance) * 0.5);

    /// <summary>
    /// Готує болід до гонки: заправляє паливо на всю дистанцію і ставить двигун із заданим зносом.
    /// </summary>
    public void PrepareForRace(Track track, double engineWear)
    {
        ArgumentNullException.ThrowIfNull(track);
        Fuel.FillForRace(track.Laps, track.LengthKm);
        Engine = new PowerUnit(engineWear);
    }

    /// <summary>Оновлює всі компоненти боліда (температури, паливо, заряд ERS).</summary>
    public void UpdateSystems(in ComponentContext context)
    {
        foreach (CarComponent component in Components)
            component.Update(context);
    }

    /// <summary>Розставляє болід на стартовій решітці.</summary>
    internal void PlaceOnGrid(int gridPosition, double distance)
    {
        GridPosition = gridPosition;
        Position = gridPosition;
        Distance = distance;
        State = CarState.OnTrack;
    }

    /// <summary>
    /// Рухає болід трасою на <paramref name="laps"/> кола і зношує шини.
    /// </summary>
    /// <exception cref="InvalidOperationException">Якщо болід не на трасі.</exception>
    public void Advance(double laps, double trackWearFactor)
    {
        if (State != CarState.OnTrack)
            throw new InvalidOperationException($"Болід {Driver.Code} не на трасі (стан {State}).");
        if (laps < 0)
            throw new ArgumentOutOfRangeException(nameof(laps), "Дистанція не може бути від'ємною.");

        Distance += laps;
        Tyre.Wear(laps, trackWearFactor, TyreWearRate);
    }

    /// <summary>Заїзд у піт-лейн.</summary>
    /// <param name="pitSpan">Частка кола, яку болід минає піт-лейном.</param>
    /// <param name="boxPosition">Положення боксу команди в піт-лейні (0..1).</param>
    public void EnterPitLane(double pitSpan, double boxPosition)
    {
        if (State != CarState.OnTrack)
            throw new InvalidOperationException("Заїхати в піт-лейн можна лише з траси.");
        State = CarState.InPitLane;
        PitEntryDistance = Distance;
        _pitSpan = pitSpan;
        _boxPosition = Math.Clamp(boxPosition, 0, 1);
        _stopDone = false;
        PitLaneProgress = 0;
    }

    /// <summary>
    /// Рухає болід піт-лейном. Повертає true, якщо болід саме доїхав до свого боксу.
    /// </summary>
    public bool MoveInPitLane(double progressDelta)
    {
        if (State != CarState.InPitLane) return false;

        PitLaneProgress += progressDelta;
        if (!_stopDone && PitLaneProgress >= _boxPosition)
        {
            PitLaneProgress = _boxPosition;
            State = CarState.PitStop;
            return true;
        }

        if (PitLaneProgress >= 1)
            ExitPitLane();
        return false;
    }

    /// <summary>Починає зупинку в боксі заданої тривалості.</summary>
    public void BeginStop(double seconds)
    {
        if (State != CarState.PitStop)
            throw new InvalidOperationException("Зупинка можлива лише в боксі.");
        StopTimeRemaining = seconds;
        LastStopDuration = seconds;
    }

    /// <summary>Відлік часу зупинки. Повертає true, коли механіки закінчили роботу.</summary>
    public bool TickStop(double seconds)
    {
        if (State != CarState.PitStop) return false;
        StopTimeRemaining -= seconds;
        if (StopTimeRemaining > 0) return false;

        StopTimeRemaining = 0;
        _stopDone = true;
        State = CarState.InPitLane;
        return true;
    }

    /// <summary>Встановлює новий комплект шин.</summary>
    public void ChangeTyres(Tyre newTyre)
    {
        Tyre = newTyre ?? throw new ArgumentNullException(nameof(newTyre));
        _usedCompounds.Add(newTyre.Compound);
        PitStops++;
    }

    /// <summary>Виїзд з піт-лейну назад на трасу.</summary>
    private void ExitPitLane()
    {
        State = CarState.OnTrack;
        Distance = PitEntryDistance + _pitSpan;
        PitLaneProgress = 0;
    }

    /// <summary>Сход з дистанції.</summary>
    public void Retire(string reason)
    {
        if (!IsRunning) return;
        if (IsInPits) Distance = RaceDistance;
        State = CarState.Retired;
        RetirementReason = reason;
        Telemetry = CarTelemetry.Stopped;   // болід зупинився — телеметрія нульова
    }

    /// <summary>
    /// Фініш: болід отримав клетчастий прапор. Він не зупиняється на лінії,
    /// а продовжує рух — починається коло охолодження.
    /// </summary>
    public void Finish(double raceTimeSeconds)
    {
        if (!IsRunning) return;
        FinishedLaps = CompletedLaps;
        if (IsInPits) Distance = RaceDistance;
        State = CarState.Finished;
        FinishTime = raceTimeSeconds;
        PostRace = PostRaceStage.CoolDownLap;
        CoolDownDistance = 0;
    }

    /// <summary>Рух колом охолодження на <paramref name="laps"/> кола.</summary>
    public void AdvanceCoolDown(double laps)
    {
        if (PostRace != PostRaceStage.CoolDownLap)
            throw new InvalidOperationException("Коло охолодження можливе лише після фінішу.");
        CoolDownDistance += Math.Max(0, laps);
    }

    /// <summary>Заїзд у піт-лейн після кола охолодження.</summary>
    public void EnterParcFerme()
    {
        if (PostRace != PostRaceStage.CoolDownLap)
            throw new InvalidOperationException("Заїхати в парк-ферме можна лише після кола охолодження.");
        PostRace = PostRaceStage.PitLane;
        ParcFermeProgress = 0;
    }

    /// <summary>
    /// Рух піт-лейном до місця стоянки <paramref name="stopAt"/> (0..1).
    /// Повертає true, коли болід зупинився в парк-ферме.
    /// </summary>
    public bool MoveToParcFerme(double progressDelta, double stopAt)
    {
        if (PostRace != PostRaceStage.PitLane) return PostRace == PostRaceStage.Parked;
        ParcFermeProgress = Math.Min(stopAt, ParcFermeProgress + Math.Max(0, progressDelta));
        if (ParcFermeProgress < stopAt) return false;
        PostRace = PostRaceStage.Parked;
        Telemetry = CarTelemetry.Stopped;
        return true;
    }

    public override string ToString() => $"{Driver.Code} ({Team.Name})";
}
