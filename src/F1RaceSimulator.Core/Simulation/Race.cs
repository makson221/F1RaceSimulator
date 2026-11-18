using F1RaceSimulator.Core.Components;
using F1RaceSimulator.Core.Geometry;
using F1RaceSimulator.Core.Models;
using F1RaceSimulator.Core.Tyres;

namespace F1RaceSimulator.Core.Simulation;

/// <summary>
/// Гонка — центральний клас симуляції.
/// Рухає боліди за профілем швидкості траси, вирішує піт-стопи через стратегії пілотів,
/// веде хронометраж, визначає позиції та повідомляє спостерігачів про події (патерн Observer).
/// Перед гонкою боліди проїжджають прогрівальне коло і стартують за вогнями,
/// після клетчастого прапора — коло охолодження і парк-ферме.
/// Клас нічого не знає про інтерфейс програми.
/// <para>
/// Клас розділено на кілька файлів (partial): <c>Race.cs</c> — рух і хронометраж,
/// <c>Race.Start.cs</c> — прогрівальне коло і вогні, <c>Race.Overtaking.cs</c> — обгони
/// та інциденти, <c>Race.SafetyCar.cs</c> — сейфті-кар.
/// </para>
/// </summary>
public sealed partial class Race : ITrackSession
{
    /// <summary>Імовірність технічного сходу за одне коло для одного боліда.</summary>
    public const double DefaultRetirementChancePerLap = 0.0005;

    /// <summary>Максимальний крок симуляції, секунди. Великі кроки діляться на менші.</summary>
    public const double MaxStepSeconds = 1.0;

    /// <summary>Відставання від суперника, при якому болід потрапляє в турбулентне повітря, секунди.</summary>
    public const double DirtyAirGapSeconds = 1.0;

    /// <summary>Втрата темпу в турбулентному повітрі (менше притискної сили): 0.25 %.</summary>
    public const double DirtyAirPaceFactor = 0.9975;

    /// <summary>Повторне попередження того самого типу для пілота — не частіше ніж раз на стільки кіл.</summary>
    public const int WarningIntervalLaps = 10;

    /// <summary>Розгін зі старту з місця: приріст швидкості, км/год за секунду (0–100 км/год ≈ 2.8 с).</summary>
    public const double LaunchAccelerationKmhPerSecond = 36;

    /// <summary>Темп кола охолодження відносно гоночного (60 %).</summary>
    public const double CoolDownPace = 0.6;

    /// <summary>Частка звичайної витрати палива на колі охолодження (малі оберти).</summary>
    public const double CoolDownFuelFactor = FuelTank.EasyLapFactor;

    /// <summary>Імовірність повільного піт-стопу (заїло гайку колеса, затримка з домкратом).</summary>
    public const double SlowStopChance = 0.07;

    /// <summary>Технічні причини сходу (аварії й зіткнення моделюються окремо).</summary>
    private static readonly string[] RetirementReasons =
    {
        "відмова силової установки",
        "проблема з коробкою передач",
        "гідравліка",
        "електроніка",
        "пошкодження підвіски",
        "перегрів гальм"
    };

    private readonly List<Car> _cars;
    private readonly List<IRaceObserver> _observers = new();
    private readonly IRandomSource _random;
    private readonly Dictionary<Car, PaceNoise> _noise = new();
    private readonly Dictionary<(string, string), double> _lastOvertake = new();
    private readonly Dictionary<(string Code, string Kind), double> _warningTimes = new();

    /// <summary>Темп кожного боліда на поточному кроці без турбулентного повітря (для рішень про обгін).</summary>
    private readonly Dictionary<Car, double> _cleanLapTime = new();

    /// <summary>Скільки кола проїхав кожен болід за поточний крок.</summary>
    private readonly Dictionary<Car, double> _stepDelta = new();

    private List<Car> _standings;
    private int _leaderLapsAnnounced;
    private bool _parkedAnnounced;

    /// <summary>Параметри плавного коливання темпу боліда (синусоїда).</summary>
    private readonly record struct PaceNoise(double Phase, double Omega, double Amplitude);

    /// <param name="track">Траса гонки.</param>
    /// <param name="grid">Боліди в порядку стартової решітки (перший — на поулі).</param>
    /// <param name="random">Джерело випадковості.</param>
    /// <param name="retirementChancePerLap">
    /// Імовірність технічного сходу за коло. 0 — без сходів і без аварій (для тестів);
    /// від цього значення пропорційно залежить і частота інцидентів на трасі.
    /// </param>
    public Race(Track track, IEnumerable<Car> grid, IRandomSource random,
        double retirementChancePerLap = DefaultRetirementChancePerLap)
    {
        Track = track ?? throw new ArgumentNullException(nameof(track));
        _random = random ?? throw new ArgumentNullException(nameof(random));
        ArgumentNullException.ThrowIfNull(grid);

        _cars = grid.ToList();
        if (_cars.Count == 0)
            throw new ArgumentException("Для гонки потрібен хоча б один болід.", nameof(grid));
        if (track.Laps <= 0)
            throw new ArgumentException("Кількість кіл повинна бути додатною.", nameof(track));
        if (track.TypicalLapSeconds <= 0)
            throw new ArgumentException("Час кола повинен бути додатним.", nameof(track));

        RetirementChancePerLap = retirementChancePerLap;
        _standings = _cars.ToList();
        OvertakingZones = OvertakingZone.Detect(track.Profile);
        _startZoneEnd = StartZoneEnd(OvertakingZones);
    }

    public Track Track { get; }

    /// <summary>Боліди в порядку стартової решітки.</summary>
    public IReadOnlyList<Car> Cars => _cars;

    /// <summary>Поточна класифікація (перший елемент — лідер).</summary>
    public IReadOnlyList<Car> Standings => _standings;

    public double RetirementChancePerLap { get; }

    /// <summary>Час гонки від згасання стартових вогнів, секунди (до старту — 0).</summary>
    public double ElapsedSeconds { get; private set; }

    /// <summary>Повний час сесії: прогрівальне коло, стартова процедура і гонка, секунди.</summary>
    public double SessionSeconds => PreRaceSeconds + ElapsedSeconds;

    /// <summary>Викликано <see cref="Start"/>: боліди на решітці або вже їдуть.</summary>
    public bool IsStarted { get; private set; }

    /// <summary>Лідер отримав клетчастий прапор.</summary>
    public bool IsChequeredFlag { get; private set; }

    /// <summary>Усі боліди фінішували або зійшли: протокол остаточний, очки нараховано.</summary>
    public bool IsFinished { get; private set; }

    /// <summary>
    /// Гонку повністю завершено: після фінішу всі боліди проїхали коло охолодження
    /// і стоять у парк-ферме.
    /// </summary>
    public bool IsCompleted => IsFinished && _cars.All(c => c.State != CarState.Finished || c.PostRace == PostRaceStage.Parked);

    /// <summary>Найшвидше коло гонки, секунди (null — ще немає).</summary>
    public double? FastestLap { get; private set; }

    /// <summary>Хто показав найшвидше коло.</summary>
    public Car? FastestLapCar { get; private set; }

    public int TotalLaps => Track.Laps;

    public Car Leader => _standings[0];

    /// <summary>Поточне коло лідера (1..TotalLaps).</summary>
    public int CurrentLap => Math.Clamp(Leader.CompletedLaps + 1, 1, TotalLaps);

    /// <summary>Загальна кількість піт-стопів у гонці.</summary>
    public int TotalPitStops => _cars.Sum(c => c.PitStops);

    /// <summary>Скільки обгонів на трасі було в гонці (без змін позицій через піт-стопи).</summary>
    public int TotalOvertakes { get; private set; }

    /// <summary>Зони обгону цієї траси — точки гальмування після прямих.</summary>
    public IReadOnlyList<OvertakingZone> OvertakingZones { get; }

    /// <summary>Підписати спостерігача на події гонки.</summary>
    public void Subscribe(IRaceObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        if (!_observers.Contains(observer))
            _observers.Add(observer);
    }

    /// <summary>Відписати спостерігача.</summary>
    public void Unsubscribe(IRaceObserver observer) => _observers.Remove(observer);

    /// <summary>
    /// Просуває симуляцію на <paramref name="seconds"/> секунд. Коли боліди після прогрівального
    /// кола стали на решітку, крок зупиняється: стартові вогні інтерфейс показує в реальному часі.
    /// </summary>
    public void Step(double seconds)
    {
        if (!IsStarted)
            throw new InvalidOperationException("Спочатку викличте Start().");
        if (IsCompleted || seconds <= 0) return;

        while (seconds > 1e-9 && !IsCompleted)
        {
            RaceStartPhase phase = StartPhase;
            double limit = phase switch
            {
                RaceStartPhase.FormationLap => FormationStepSeconds,
                RaceStartPhase.StartLights => LightsStepSeconds,
                _ => MaxStepSeconds
            };
            double dt = Math.Min(limit, seconds);
            switch (phase)
            {
                case RaceStartPhase.FormationLap:
                    StepFormationLap(dt);
                    break;
                case RaceStartPhase.StartLights:
                    StepStartLights(dt);
                    break;
                default:
                    StepInternal(dt);
                    break;
            }
            seconds -= dt;
            if (phase == RaceStartPhase.FormationLap && StartPhase == RaceStartPhase.StartLights)
                break;
        }
    }

    /// <summary>
    /// Проводить гонку до остаточного протоколу — коли всі фінішували або зійшли
    /// (для тестів і швидкого розрахунку). Якщо гонку не розпочато, спершу прогрівальне коло.
    /// </summary>
    /// <exception cref="InvalidOperationException">Якщо гонка не завершилась за розумний час.</exception>
    public void RunToFinish()
    {
        if (!IsStarted) Start();
        double limit = TotalLaps * Track.TypicalLapSeconds * 3;
        while (!IsFinished)
        {
            if (ElapsedSeconds > limit)
                throw new InvalidOperationException("Гонка не завершилася за відведений час.");
            Step(MaxStepSeconds);
        }
    }

    /// <summary>Проводить гонку разом із колом охолодження — доки всі боліди не стануть у парк-ферме.</summary>
    public void RunToCompletion()
    {
        RunToFinish();
        double limit = ElapsedSeconds + Track.TypicalLapSeconds * 6;
        while (!IsCompleted && ElapsedSeconds < limit)
            Step(MaxStepSeconds);
    }

    private void StepInternal(double dt)
    {
        ElapsedSeconds += dt;
        _stepDelta.Clear();
        UpdateVirtualSafetyCar();

        // Боліди рухаються в порядку класифікації: суперник попереду вже зробив свій крок,
        // тому відомо, де він зараз, — і болід позаду не може «проїхати крізь» нього.
        List<Car> order = _standings.ToList();
        Car? leaderOnTrack = order.FirstOrDefault(c => c.State == CarState.OnTrack);
        var inPitsBefore = order.Where(c => c.IsInPits).ToHashSet();

        // Відставання від суперника попереду і відрив від суперника позаду на початок кроку
        // (для турбулентного повітря і режимів атаки й захисту).
        var gapAhead = new double[order.Count];
        var gapBehind = new double[order.Count];
        for (int i = 0; i < order.Count; i++)
        {
            gapAhead[i] = GapToCar(order, i - 1, order[i]);
            gapBehind[i] = i + 1 < order.Count && order[i + 1].State == CarState.OnTrack
                ? TimeGap(order[i + 1].RaceDistance, order[i].RaceDistance)
                : double.PositiveInfinity;
        }

        for (int i = 0; i < order.Count; i++)
        {
            Car car = order[i];
            switch (car.State)
            {
                case CarState.OnTrack:
                    UpdateOnTrack(car, dt, gapAhead[i], gapBehind[i], CarAheadOnTrack(order, i), car == leaderOnTrack);
                    break;
                case CarState.InPitLane:
                    UpdateInPitLane(car, dt);
                    break;
                case CarState.PitStop:
                    UpdatePitStop(car, dt);
                    break;
                case CarState.Finished:
                    UpdatePostRace(car, dt);
                    break;
            }
        }

        UpdateStandings(inPitsBefore);

        if (!IsFinished && _cars.All(c => !c.IsRunning))
        {
            IsFinished = true;
            var winner = _standings[0];
            Notify(RaceEventType.RaceFinished,
                $"Протокол остаточний. Перемога — {winner.Driver.FullName} ({winner.Team.Name}).", winner);
        }

        if (IsCompleted && !_parkedAnnounced)
        {
            _parkedAnnounced = true;
            var podium = _standings.Where(c => c.State == CarState.Finished).Take(3).ToList();
            Notify(RaceEventType.CarsParked,
                "Усі боліди в парк-ферме. Подіум: " + string.Join(", ", podium.Select((c, i) => $"{i + 1}. {c.Driver.LastName}")) + ".",
                podium.FirstOrDefault());
        }
    }

    /// <summary>
    /// Суперник, з яким болід бореться за позицію: найближчий попереду в класифікації,
    /// хто зараз на трасі (не в піт-лейні). Боліди, що відстають на коло, сюди не потрапляють:
    /// за синіх прапорів вони пропускають лідерів.
    /// </summary>
    private static Car? CarAheadOnTrack(List<Car> order, int index)
    {
        Car car = order[index];
        for (int j = index - 1; j >= 0; j--)
        {
            Car other = order[j];
            if (other.State != CarState.OnTrack) continue;
            return other.Distance - car.Distance < 0.5 ? other : null;
        }
        return null;
    }

    /// <summary>
    /// Коло охолодження після фінішу: болід плавно скидає швидкість, проїжджає коло
    /// і заїжджає піт-лейном у парк-ферме (призери — на перші місця біля подіуму).
    /// </summary>
    private void UpdatePostRace(Car car, double dt)
    {
        switch (car.PostRace)
        {
            case PostRaceStage.CoolDownLap:
            {
                double since = ElapsedSeconds - (car.FinishTime ?? ElapsedSeconds);
                double slow = 1 - (1 - CoolDownPace) * Math.Min(1, since / 12);   // 12 с плавного гальмування
                double lapTime = Track.TypicalLapSeconds / slow;
                double before = car.Distance + car.CoolDownDistance;
                double rel = Track.Profile.RelativeSpeedAt(car.CoolDownFraction);
                double laps = dt * (0.5 + 0.5 * rel) / lapTime;            // профіль згладжено: у поворотах не так повільно
                car.AdvanceCoolDown(laps);
                // На колі охолодження двигун працює на малих обертах: палива йде приблизно втричі менше.
                car.UpdateSystems(new ComponentContext(dt, laps * CoolDownFuelFactor, Track.AirTemperature, 0.1, false, IsRacing: true));
                var sample = Track.Profile.SampleAt(car.CoolDownFraction, lapTime);
                car.Telemetry = Track.Profile.IsEmpty
                    ? new CarTelemetry(200 * slow, 6, 9000, 40, false)
                    : new CarTelemetry(sample.Speed * (0.5 + 0.5 * slow), Math.Max(2, sample.Gear - 1),
                        (int)(sample.Rpm * 0.8), Math.Min(sample.Throttle, 45), false);

                if (car.CoolDownDistance > 0.5 && Crossed(before, car.Distance + car.CoolDownDistance, Track.Layout.PitEntry))
                    car.EnterParcFerme();
                else if (car.CoolDownDistance > 2.5)          // запобіжник: не довше двох з половиною кіл
                    car.EnterParcFerme();
                break;
            }
            case PostRaceStage.PitLane:
                car.Telemetry = CarTelemetry.PitLimiter;
                car.MoveToParcFerme(dt / Track.PitLaneSeconds, ParcFermePosition(car));
                break;
        }
    }

    /// <summary>Місце стоянки в парк-ферме: перші три — біля подіуму, решта — біля своїх боксів.</summary>
    private static double ParcFermePosition(Car car) =>
        car.Position <= 3 ? 0.9 - (car.Position - 1) * 0.03 : BoxPositionFor(car.Team);

    /// <summary>
    /// Відставання в секундах між двома дистанціями. На коротких відстанях враховується
    /// швидкість у цьому місці траси (у повільному повороті метри «коштують» більше часу).
    /// </summary>
    private double TimeGap(double behind, double ahead)
    {
        double d = ahead - behind;
        if (d <= 0) return 0;
        if (d > 0.05) return d * Track.TypicalLapSeconds;
        double rel = Math.Max(0.2, Track.Profile.RelativeSpeedAt((behind + ahead) / 2));
        return d * Track.TypicalLapSeconds / rel;
    }

    /// <summary>Скільки кола болід проїжджає за <paramref name="seconds"/> секунд у цьому місці траси.</summary>
    private double SecondsToLaps(double seconds, double fraction, double lapTime) =>
        seconds * Math.Max(0.2, Track.Profile.RelativeSpeedAt(fraction)) / lapTime;

    /// <summary>Відставання боліда від суперника з індексом <paramref name="aheadIndex"/>, секунди.</summary>
    private double GapToCar(List<Car> standings, int aheadIndex, Car car) =>
        aheadIndex < 0 || standings[aheadIndex].State != CarState.OnTrack
            ? double.PositiveInfinity
            : TimeGap(car.RaceDistance, standings[aheadIndex].RaceDistance);

    private void UpdateOnTrack(Car car, double dt, double gapAheadSeconds, double gapBehindSeconds, Car? rival, bool leadsOnTrack)
    {
        bool neutralized = SafetyCar != SafetyCarMode.None;
        if (neutralized)
        {
            // Під сейфті-каром боротьби немає: рівний темп, без ERS і турбулентного повітря.
            car.InDirtyAir = false;
            car.Mode = DrivingMode.Normal;
        }
        else
        {
            ChooseDrivingMode(car, gapAheadSeconds, gapBehindSeconds, WorthAttacking(car, rival, gapAheadSeconds));
        }

        // Темп: пілот і болід, шини (знос і температура), двигун, ERS, режим їзди
        // і плавне коливання форми. Окремо — втрата в турбулентному повітрі.
        var noise = _noise[car];
        double pace = car.BasePace
                      * car.Tyre.PaceMultiplier
                      * car.Engine.PaceFactor
                      * car.Battery.PaceFactor
                      * car.ModePaceFactor
                      * (1 + noise.Amplitude * Math.Sin(ElapsedSeconds * noise.Omega + noise.Phase));
        double cleanLapTime = Track.TypicalLapSeconds / pace + car.Fuel.LapTimePenaltySeconds;
        _cleanLapTime[car] = cleanLapTime;
        double lapTime = car.InDirtyAir
            ? Track.TypicalLapSeconds / (pace * DirtyAirPaceFactor) + car.Fuel.LapTimePenaltySeconds
            : cleanLapTime;

        // Нейтралізація: обмеження темпу і з ким тримати дистанцію.
        Car? target = rival;
        double followSeconds = MinFollowSeconds;
        if (SafetyCar == SafetyCarMode.SafetyCar)
        {
            target = leadsOnTrack ? null : PhysicallyAhead(car);
            lapTime = SafetyCarLapTime(car, lapTime, target);
            followSeconds = SafetyCarGapSeconds;
        }
        else if (SafetyCar == SafetyCarMode.Virtual)
        {
            lapTime = Math.Max(lapTime, Track.TypicalLapSeconds * VscLapFactor);
        }

        // Швидкість за профілем траси: на прямих — вища за середню, у поворотах — нижча.
        // Береться в середині кроку (метод середньої точки), щоб рух був точнішим.
        double fraction = Polyline.Wrap(car.Distance);
        double launch = LaunchFactor(car, fraction, dt, lapTime);
        double rel = Track.Profile.RelativeSpeedAt(fraction);
        double lapsDelta = dt * rel * launch / lapTime;
        rel = Track.Profile.RelativeSpeedAt(fraction + lapsDelta / 2);
        lapsDelta = dt * rel * launch / lapTime;

        // Обгін: маневр, що вже триває, і нова спроба в зоні гальмування.
        lapsDelta += ContinuePass(car, dt, launch);
        double before = car.Distance;
        if (target != null && !neutralized && CanAttack(car))
            TryOvertake(car, target, before, before + lapsDelta);
        if (car.State != CarState.OnTrack) return;       // зіткнення під час спроби обгону

        // Не можна проїхати крізь суперника: якщо обгін не вдався, болід їде слідом за ним.
        if (target != null && !IsPassing(car, target) && !IsFreeStart(car) && !IsRecovering(target))
            lapsDelta = LimitByCarAhead(car, target, lapsDelta, followSeconds, fraction, lapTime);

        // Під сейфті-каром шини й двигун майже відпочивають, палива йде менше.
        double load = neutralized ? 0.6 : 1;
        car.Advance(lapsDelta, Track.TyreWearFactor * (neutralized ? 0.35 : 1));
        _stepDelta[car] = lapsDelta;
        car.UpdateSystems(new ComponentContext(dt, lapsDelta * load, Track.AirTemperature,
            neutralized ? 0.15 : car.Push, car.InDirtyAir, IsRacing: true));
        UpdateTelemetry(car, lapTime, launch);
        HandleSectors(car, before, car.Distance, dt);

        // Спочатку перетин лінії: болід, що доїхав до фінішу на останніх краплях палива, фінішує.
        HandleLineCrossing(car, before, car.Distance, dt);
        if (car.State != CarState.OnTrack) return;

        if (car.Fuel.IsEmpty)
        {
            car.Retire("закінчилося паливо");
            Notify(RaceEventType.Retirement, $"{car.Driver.FullName} — сход (закінчилося паливо).", car);
            DeployVirtualSafetyCar($"болід {car.Driver.LastName} зупинився на трасі");
            return;
        }

        // Технічний сход: ризик зростає зі зносом і перегрівом двигуна, нестабільні пілоти ризикують більше.
        double chance = RetirementChancePerLap * lapsDelta * (2 - car.Driver.Consistency) * car.Engine.FailureRiskMultiplier;
        if (car.Distance > 0 && _random.NextDouble() < chance)
        {
            bool engineFault = car.Engine.Wear >= 60 || car.Engine.Temperature >= PowerUnit.CriticalTemperature;
            string reason = engineFault
                ? RetirementReasons[0]
                : RetirementReasons[(int)(_random.NextDouble() * RetirementReasons.Length) % RetirementReasons.Length];
            car.Retire(reason);
            Notify(RaceEventType.Retirement, $"{car.Driver.FullName} — сход ({reason}).", car);
            if (_random.NextDouble() < 0.35)                      // болід зупинився в небезпечному місці
                DeployVirtualSafetyCar($"болід {car.Driver.LastName} зупинився на трасі");
            return;
        }

        // Помилка пілота без суперника поруч: розворот або аварія.
        if (!neutralized && CheckSoloIncident(car, lapsDelta)) return;

        // В'їзд у піт-лейн: стратегія пілота вирішує, чи заїжджати. Під сейфті-каром
        // піт-стоп коштує вдвічі менше часу, тому команди охоче цим користуються.
        if (!IsChequeredFlag && car.Distance > 0.5 && Crossed(before, car.Distance, Track.Layout.PitEntry))
        {
            int lapsRemaining = TotalLaps - car.CompletedLaps;
            bool cheapStop = IsCheapStop(car, lapsRemaining);
            if (cheapStop || car.Strategy.ShouldPit(car, lapsRemaining, Track))
            {
                car.EnterPitLane(Track.Layout.PitSpan, BoxPositionFor(car.Team));
                _passes.Remove(car);
                if (neutralized) _neutralizedStops.Add(car);
                Notify(RaceEventType.PitEntry, cheapStop
                    ? $"{car.Driver.FullName} користується нейтралізацією і заїжджає на піт-стоп (шини {car.Tyre.Condition:0}%)."
                    : $"{car.Driver.FullName} заїжджає на піт-стоп (шини {car.Tyre.Condition:0}%).", car);
            }
        }
    }

    /// <summary>
    /// Обмежує крок так, щоб болід не наздогнав суперника ближче, ніж на
    /// <paramref name="followSeconds"/>. Якщо болід уже надто близько, він їде трохи
    /// повільніше за суперника і поступово відпускає його на безпечну дистанцію.
    /// </summary>
    private double LimitByCarAhead(Car car, Car target, double lapsDelta, double followSeconds, double fraction, double lapTime)
    {
        double gap = Polyline.Wrap(target.Distance - car.Distance);
        if (gap > 0.5) return lapsDelta;                                   // насправді суперник позаду
        double allowed = gap - SecondsToLaps(followSeconds, fraction, lapTime);
        if (lapsDelta <= allowed) return lapsDelta;
        if (allowed >= 0) return allowed;
        double follow = _stepDelta.TryGetValue(target, out double aheadDelta) ? aheadDelta : lapsDelta;
        return Math.Max(0, Math.Min(lapsDelta, follow * 0.97));
    }

    /// <summary>Телеметрія з профілю траси під темп боліда.</summary>
    private void UpdateTelemetry(Car car, double lapTime, double launch)
    {
        if (Track.Profile.IsEmpty)
        {
            car.Telemetry = new CarTelemetry(220 * launch * Track.TypicalLapSeconds / lapTime, 6, 10500, 100, false);
            return;
        }
        var s = Track.Profile.SampleAt(Polyline.Wrap(car.Distance), lapTime);
        int throttle = s.Throttle;
        if (car.Mode == DrivingMode.Saving && !s.Brake && throttle > 90) throttle = 85;   // lift and coast
        car.Telemetry = launch < 1
            ? new CarTelemetry(s.Speed * launch, Math.Clamp((int)(s.Gear * launch) + 1, 1, 8), launch <= 0 ? 4000 : 11000, launch <= 0 ? 0 : 100, false)
            : new CarTelemetry(s.Speed, s.Gear, s.Rpm, throttle, s.Brake);
    }

    /// <summary>Перетин меж секторів 1/2 і 2/3 — хронометраж секторів.</summary>
    private void HandleSectors(Car car, double before, double after, double dt)
    {
        double[] sectors = Track.Layout.Sectors;
        if (after <= 0 || sectors.Length < 2) return;
        for (int i = 0; i < 2; i++)
        {
            if (Crossed(before, after, sectors[i]))
                car.Timing.CompleteSector(i + 1, CrossingTime(before, after, sectors[i], dt));
        }
    }

    /// <summary>Момент перетину позначки <paramref name="fraction"/> всередині кроку (лінійна інтерполяція).</summary>
    private double CrossingTime(double before, double after, double fraction, double dt)
    {
        double mark = Math.Floor(after - fraction) + fraction;
        return after > before ? ElapsedSeconds - dt * (after - mark) / (after - before) : ElapsedSeconds;
    }

    /// <summary>
    /// Обирає режим їзди. Економія — якщо перегріваються шини чи двигун або не вистачає палива
    /// (з гістерезисом: щоб вийти з економії, показники мають повернутися в норму із запасом).
    /// Атака — якщо суперник попереду ближче ніж за секунду і є сенс атакувати; захист — якщо
    /// суперник позаду ближче ніж за секунду, а попереду вільно (обидва — з другого кола).
    /// Пілот, що застряг у «поїзді» без шансів обігнати, їде рівним темпом і береже шини.
    /// Перехід в економію записується в журнал.
    /// </summary>
    private void ChooseDrivingMode(Car car, double gapAheadSeconds, double gapBehindSeconds, bool worthAttacking)
    {
        bool saving = car.Mode == DrivingMode.Saving;
        double lapsToGo = Math.Max(0, TotalLaps - car.Distance);

        // Після фінішу потрібне паливо на коло охолодження і 1 кг проби для FIA (регламент),
        // тому команда тримає ще 0.2 кола запасу; вийти з економії можна із запасом 0.5 кола.
        double afterFlagLaps = car.Fuel.ConsumptionPerLap > 0
            ? CoolDownFuelFactor + FuelTank.SampleKg / car.Fuel.ConsumptionPerLap
            : 0;
        double fuelMargin = car.Fuel.LapsLeft - lapsToGo - afterFlagLaps;
        bool fuelShort = car.Fuel.CapacityKg > 0 && fuelMargin < (saving ? 0.5 : 0.2);
        bool tyresHot = car.Tyre.Temperature > car.Tyre.OptimalMaxTemperature + (saving ? 0 : 5);
        bool engineHot = car.Engine.Temperature >= PowerUnit.WarningTemperature - (saving ? 5 : 0);

        car.InDirtyAir = gapAheadSeconds < DirtyAirGapSeconds;
        bool racing = car.Distance >= 1;
        DrivingMode mode = fuelShort || tyresHot || engineHot ? DrivingMode.Saving
            : racing && car.InDirtyAir ? (worthAttacking ? DrivingMode.Attack : DrivingMode.Normal)
            : racing && gapBehindSeconds < DirtyAirGapSeconds ? DrivingMode.Defend
            : DrivingMode.Normal;

        if (mode == DrivingMode.Saving && !saving && car.Distance >= 1)
        {
            string name = car.Driver.FullName;
            if (engineHot)
                Warn(car, "engine", $"{name}: двигун перегрівся до {car.Engine.Temperature:0} °C, пілот знижує темп.");
            else if (tyresHot)
                Warn(car, "tyres", $"{name}: перегрів шин {car.Tyre.CompoundName} ({car.Tyre.Temperature:0} °C), пілот береже гуму.");
            else
                Warn(car, "fuel", $"{name} економить паливо: {car.Fuel.FuelKg:0.0} кг на {UkrainianText.Laps((int)Math.Ceiling(lapsToGo))}.");
        }
        car.Mode = mode;
    }

    /// <summary>
    /// Надсилає технічне попередження. Одне й те саме попередження для пілота
    /// повторюється не частіше ніж раз на <see cref="WarningIntervalLaps"/> кіл.
    /// </summary>
    private void Warn(Car car, string kind, string message)
    {
        var key = (car.Driver.Code, kind);
        double interval = WarningIntervalLaps * Track.TypicalLapSeconds;
        if (_warningTimes.TryGetValue(key, out double last) && ElapsedSeconds - last < interval) return;
        _warningTimes[key] = ElapsedSeconds;
        Notify(RaceEventType.TechnicalWarning, message, car);
    }

    private void UpdateInPitLane(Car car, double dt)
    {
        car.UpdateSystems(new ComponentContext(dt, 0, Track.AirTemperature, 0, false, IsRacing: false));
        car.Telemetry = CarTelemetry.PitLimiter;
        double before = car.RaceDistance;
        bool reachedBox = car.MoveInPitLane(dt / Track.PitLaneSeconds);
        if (reachedBox)
        {
            // Зазвичай механіки працюють 2.0–3.3 с, але іноді заїдає гайку колеса чи домкрат.
            bool slow = _random.NextDouble() < SlowStopChance;
            double stopSeconds = slow ? 4.5 + _random.NextDouble() * 5.5 : 2.0 + _random.NextDouble() * 1.3;
            car.BeginStop(stopSeconds);
        }

        HandleSectors(car, before, car.RaceDistance, dt);
        HandleLineCrossing(car, before, car.RaceDistance, dt);
    }

    private void UpdatePitStop(Car car, double dt)
    {
        car.UpdateSystems(new ComponentContext(dt, 0, Track.AirTemperature, 0, false, IsRacing: false));
        car.Telemetry = CarTelemetry.Stopped;
        if (!car.TickStop(dt)) return;

        int lapsRemaining = Math.Max(1, TotalLaps - car.CompletedLaps);
        TyreCompound compound = car.Strategy.ChooseCompound(car, lapsRemaining, Track);
        Tyre oldTyre = car.Tyre;
        car.ChangeTyres(TyreFactory.Create(compound));

        string text = car.LastStopDuration > 4
            ? $"{car.Driver.FullName} — повільний піт-стоп {car.LastStopDuration:0.0} с (проблема з колесом), {oldTyre.CompoundName} → {car.Tyre.CompoundName}."
            : $"{car.Driver.FullName} — піт-стоп {car.LastStopDuration:0.0} с, {oldTyre.CompoundName} → {car.Tyre.CompoundName}.";
        Notify(RaceEventType.PitStop, text, car);
    }

    /// <summary>Перетин лінії старту/фінішу: завершене коло або фініш.</summary>
    private void HandleLineCrossing(Car car, double before, double after, double dt)
    {
        if (after < 1 || Math.Floor(after) <= Math.Floor(before)) return;
        int lapsDone = (int)Math.Floor(after);

        // Точний момент перетину лінії всередині кроку (лінійна інтерполяція).
        double crossingTime = CrossingTime(before, after, 0, dt);
        RecordLap(car, crossingTime);

        if (IsChequeredFlag)
        {
            car.Finish(crossingTime);             // після лідера кожен фінішує, перетнувши лінію
            return;
        }

        if (lapsDone >= TotalLaps)
        {
            IsChequeredFlag = true;
            EndNeutralizationAtFlag();
            car.Finish(crossingTime);
            Notify(RaceEventType.LeaderFinished,
                $"Клетчастий прапор! Перемога — {car.Driver.FullName} ({car.Team.Name}).", car);
            return;
        }

        if (lapsDone > _leaderLapsAnnounced)
        {
            _leaderLapsAnnounced = lapsDone;
            Notify(RaceEventType.LapCompleted, $"{car.Driver.LastName} лідирує, починається коло {lapsDone + 1}.", car);
            OnLeaderLap(lapsDone);
        }
    }

    /// <summary>Зараховує коло боліду і перевіряє найшвидше коло гонки.</summary>
    private void RecordLap(Car car, double crossingTime)
    {
        double lap = car.Timing.CompleteLap(crossingTime);
        if (car.Timing.LapsTimed < 2 || car.State != CarState.OnTrack) return;   // перше коло — зі старту з місця
        if (FastestLap == null || lap < FastestLap)
        {
            bool announce = FastestLap != null && car.Timing.LapsTimed >= 5;
            FastestLap = lap;
            FastestLapCar = car;
            if (announce)
                Notify(RaceEventType.FastestLap, $"Найшвидше коло: {car.Driver.FullName} — {QualifyingResult.FormatLapTime(lap)}.", car);
        }
    }

    /// <summary>
    /// Боліди для схеми траси: на решітці і прогрівальному колі, на трасі, у піт-лейні,
    /// на колі охолодження, у парк-ферме.
    /// </summary>
    public IReadOnlyList<CarPosition> GetCarPositions()
    {
        var list = new List<CarPosition>(_cars.Count);
        foreach (Car car in _standings.AsEnumerable().Reverse())
        {
            if (car.State == CarState.Retired) continue;
            bool top = car.Position <= 3;
            if (StartPhase != RaceStartPhase.Racing)
            {
                double fraction = Polyline.Wrap(car.Distance + _formation.GetValueOrDefault(car, 1));
                list.Add(new CarPosition(car, fraction, false, top ? car.Driver.Code : null, top));
            }
            else if (car.State == CarState.Finished)
            {
                list.Add(car.PostRace == PostRaceStage.CoolDownLap
                    ? new CarPosition(car, car.CoolDownFraction, false, top ? car.Driver.Code : null, top)
                    : new CarPosition(car, car.ParcFermeProgress, true, top ? $"P{car.Position} {car.Driver.Code}" : null, top));
            }
            else if (car.IsInPits)
            {
                list.Add(new CarPosition(car, car.PitLaneProgress, true, car.Driver.Code + " · PIT", true));
            }
            else
            {
                list.Add(new CarPosition(car, Polyline.Wrap(car.Distance), false, top ? car.Driver.Code : null, top));
            }
        }
        return list;
    }

    /// <summary>
    /// Сортує боліди за пройденою дистанцією і фіксує обгони. Зміна позицій на виїзді з піт-лейну
    /// (<paramref name="inPitsBefore"/> — хто на початку кроку був у боксах) обгоном не вважається.
    /// </summary>
    private void UpdateStandings(HashSet<Car>? inPitsBefore = null)
    {
        var ordered = _cars
            .OrderBy(c => c.State == CarState.Retired ? 1 : 0)
            .ThenByDescending(SortKey)
            .ThenBy(c => c.FinishTime ?? double.MaxValue)
            .ToList();

        for (int i = 0; i < ordered.Count - 1; i++)
        {
            Car a = ordered[i], b = ordered[i + 1];
            bool aWasBehind = a.Position > b.Position;
            bool pitExit = inPitsBefore != null && (inPitsBefore.Contains(a) || inPitsBefore.Contains(b));
            if (aWasBehind && a.State == CarState.OnTrack && b.State == CarState.OnTrack && !pitExit)
            {
                var key = string.CompareOrdinal(a.Driver.Code, b.Driver.Code) < 0
                    ? (a.Driver.Code, b.Driver.Code)
                    : (b.Driver.Code, a.Driver.Code);
                if (!_lastOvertake.TryGetValue(key, out double last) || ElapsedSeconds - last > 30)
                {
                    TotalOvertakes++;
                    Notify(RaceEventType.Overtake, ElapsedSeconds < 30
                        ? $"Старт: {a.Driver.FullName} обходить {b.Driver.LastName} і виходить на P{i + 1}."
                        : $"Обгін: {a.Driver.FullName} виходить на P{i + 1}, позаду — {b.Driver.LastName}.", a, b);
                }
                _lastOvertake[key] = ElapsedSeconds;
            }
        }

        for (int i = 0; i < ordered.Count; i++)
            ordered[i].Position = i + 1;

        _standings = ordered;
    }

    private static double SortKey(Car car) =>
        car.State == CarState.Finished ? car.FinishedLaps : car.RaceDistance;

    /// <summary>Чи перетнула дистанція позначку <paramref name="fraction"/> кола.</summary>
    public static bool Crossed(double before, double after, double fraction) =>
        Math.Floor(after - fraction) > Math.Floor(before - fraction);

    /// <summary>Положення боксу команди в піт-лейні (частка від 0 до 1).</summary>
    public static double BoxPositionFor(Team team) =>
        Math.Clamp(0.76 - (team.PitBoxOrder - 1) * 0.054, 0.1, 0.9);

    /// <summary>Відставання від лідера в секундах (приблизно, за типовим часом кола).</summary>
    public double GapToLeaderSeconds(Car car)
    {
        ArgumentNullException.ThrowIfNull(car);
        Car leader = Leader;
        if (car == leader) return 0;
        if (car.State == CarState.Finished && leader.State == CarState.Finished
            && car.FinishedLaps == leader.FinishedLaps)
            return (car.FinishTime ?? 0) - (leader.FinishTime ?? 0);
        return (SortKey(leader) - SortKey(car)) * Track.TypicalLapSeconds;
    }

    /// <summary>Скільки кіл болід відстає від лідера.</summary>
    public int LapsBehindLeader(Car car)
    {
        ArgumentNullException.ThrowIfNull(car);
        if (car == Leader) return 0;
        if (car.State == CarState.Finished && Leader.State == CarState.Finished)
            return Leader.FinishedLaps - car.FinishedLaps;
        return Math.Max(0, (int)Math.Floor(SortKey(Leader) - SortKey(car)));
    }

    /// <summary>Текст відставання для таблиці: «Лідер», «+3.4», «+1 коло», «Сход», «Решітка».</summary>
    public string GapText(Car car)
    {
        ArgumentNullException.ThrowIfNull(car);
        if (car.State == CarState.Retired) return "Сход";
        if (StartPhase != RaceStartPhase.Racing) return car.GridPosition == 1 ? "Поул" : "Решітка";
        if (car == Leader) return "Лідер";
        int lapsBehind = LapsBehindLeader(car);
        if (lapsBehind >= 1) return "+" + UkrainianText.Laps(lapsBehind);
        return "+" + GapToLeaderSeconds(car).ToString("0.0");
    }

    /// <summary>Фінішний протокол (або поточна класифікація, якщо гонка ще триває).</summary>
    public IReadOnlyList<RaceResult> GetResults()
    {
        var results = new List<RaceResult>();
        for (int i = 0; i < _standings.Count; i++)
        {
            Car car = _standings[i];
            int position = i + 1;
            string status;
            int points = 0;

            switch (car.State)
            {
                case CarState.Finished:
                    int down = LapsBehindLeader(car);
                    status = down == 0 ? "Фініш" : "+" + UkrainianText.Laps(down);
                    points = PointsSystem.ForPosition(position);
                    break;
                case CarState.Retired:
                    status = "Сход: " + car.RetirementReason;
                    break;
                default:
                    status = "У гонці";
                    break;
            }

            int laps = car.State == CarState.Finished ? car.FinishedLaps : car.CompletedLaps;
            results.Add(new RaceResult(position, car, laps, status, points));
        }
        return results;
    }

    private void Notify(RaceEventType type, string message, Car? car = null, Car? other = null)
    {
        var raceEvent = new RaceEvent(type, CurrentLapSafe(), ElapsedSeconds, message, car, other)
        {
            Label = StartPhase == RaceStartPhase.Racing ? null : "Перед стартом"
        };
        foreach (var observer in _observers.ToList())
            observer.OnRaceEvent(this, raceEvent);
    }

    private int CurrentLapSafe() => _standings.Count == 0 ? 1 : CurrentLap;
}
