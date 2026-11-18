using F1RaceSimulator.Core.Components;
using F1RaceSimulator.Core.Geometry;

namespace F1RaceSimulator.Core.Simulation;

/// <summary>
/// Стартова процедура гонки: прогрівальне коло, п'ять червоних вогнів і старт з місця
/// з різною реакцією пілотів.
/// </summary>
public sealed partial class Race
{
    /// <summary>Темп прогрівального кола відносно гоночного (55 %).</summary>
    public const double FormationLapPace = 0.55;

    /// <summary>Інтервал, з яким боліди рушають з решітки на прогрівальне коло, секунди.</summary>
    public const double FormationDepartureSeconds = 0.5;

    /// <summary>Пауза після того, як останній болід став на своє місце, до першого вогню, секунди.</summary>
    public const double GridSettleSeconds = 3.0;

    /// <summary>Кількість стартових вогнів.</summary>
    public const int StartLightCount = 5;

    /// <summary>Вогні загоряються по одному з цим інтервалом, секунди.</summary>
    public const double LightIntervalSeconds = 1.0;

    /// <summary>Найкоротша і найдовша пауза між п'ятим вогнем і стартом, секунди.</summary>
    public const double MinLightsOutDelay = 0.2, MaxLightsOutDelay = 3.0;

    private const double FormationStepSeconds = 0.5;
    private const double LightsStepSeconds = 0.1;

    /// <summary>Інтенсивність їзди на прогрівальному колі: пілоти «виляють», щоб прогріти шини.</summary>
    private const double FormationPush = 0.35;

    /// <summary>За скільки кола до свого місця на решітці болід починає гальмувати.</summary>
    private const double FormationBrakingLaps = 0.012;

    /// <summary>Болід на решітці: двигун працює, передача нейтральна, пілот тримає гальмо.</summary>
    private static readonly CarTelemetry GridIdle = new(0, 0, 4500, 0, true);

    /// <summary>Пройдена частина прогрівального кола для кожного боліда (1 — повернувся на своє місце).</summary>
    private readonly Dictionary<Car, double> _formation = new();

    /// <summary>
    /// З якого моменту гонки болід може рушити: на старті — час реакції на згасання вогнів,
    /// після розвороту — момент, коли пілот знову рушає.
    /// </summary>
    private readonly Dictionary<Car, double> _launchFrom = new();

    /// <summary>Якість розгону на старті (зчеплення, пробуксовка): 0.985–1.015.</summary>
    private readonly Dictionary<Car, double> _launchGrip = new();

    private readonly HashSet<Car> _badStarts = new();
    private readonly double _startZoneEnd;
    private double _phaseClock;
    private double _lightsOutDelay;

    /// <summary>Етап стартової процедури: прогрівальне коло, вогні або гонка.</summary>
    public RaceStartPhase StartPhase { get; private set; } = RaceStartPhase.FormationLap;

    /// <summary>Скільки секунд тривали прогрівальне коло і стартова процедура.</summary>
    public double PreRaceSeconds { get; private set; }

    /// <summary>Гонка триває (вогні вже згасли).</summary>
    public bool IsRacing => IsStarted && StartPhase == RaceStartPhase.Racing;

    /// <summary>Скільки червоних вогнів зараз горить (0–5). Після старту — 0: вогні згасли.</summary>
    public int StartLightsOn => StartPhase != RaceStartPhase.StartLights
        ? 0
        : Math.Clamp((int)Math.Floor((_phaseClock - GridSettleSeconds) / LightIntervalSeconds) + 1, 0, StartLightCount);

    /// <summary>Частка прогрівального кола, яку проїхав поулмен (0..1).</summary>
    public double FormationLapProgress =>
        StartPhase == RaceStartPhase.FormationLap ? _formation.GetValueOrDefault(_cars[0]) : 1;

    /// <summary>
    /// Розставляє боліди на решітці. Далі <see cref="Step"/> веде прогрівальне коло,
    /// запалює стартові вогні і дає старт, коли вони гаснуть.
    /// </summary>
    /// <param name="formationLap">
    /// false — без прогрівального кола і вогнів: гонка починається одразу (для тестів і розрахунків).
    /// </param>
    public void Start(bool formationLap = true)
    {
        if (IsStarted)
            throw new InvalidOperationException("Гонку вже розпочато.");

        for (int i = 0; i < _cars.Count; i++)
        {
            var car = _cars[i];
            // Кожна наступна позиція на решітці приблизно на 8 метрів позаду.
            car.PlaceOnGrid(i + 1, -0.0005 - 0.0015 * i);

            // Паливо на всю дистанцію; двигун уже пройшов кілька етапів сезону (знос 5–40 %).
            car.PrepareForRace(Track, engineWear: 5 + _random.NextDouble() * 35);
            car.Telemetry = formationLap ? GridIdle : CarTelemetry.Stopped;
            _formation[car] = formationLap ? 0 : 1;

            double period = 60 + _random.NextDouble() * 90;             // 60–150 секунд
            _noise[car] = new PaceNoise(
                Phase: _random.NextDouble() * 2 * Math.PI,
                Omega: 2 * Math.PI / period,
                Amplitude: 0.0015 + (1 - car.Driver.Consistency) * 0.015);

            // Реакція на згасання вогнів: 0.17–0.27 с, у майстрів боротьби — швидше.
            // Іноді старт не вдається: пробуксовка або ледь не заглух двигун.
            double racecraft = car.Driver.Racecraft;
            double reaction = 0.17 + 0.25 * (1 - racecraft) + 0.05 * _random.NextDouble();
            if (_random.NextDouble() < 0.02 + 0.1 * (1 - racecraft))
            {
                reaction += 0.5 + 0.6 * _random.NextDouble();
                _badStarts.Add(car);
            }
            _launchFrom[car] = reaction;
            _launchGrip[car] = 0.985 + 0.03 * _random.NextDouble();
        }

        _lightsOutDelay = MinLightsOutDelay + (MaxLightsOutDelay - MinLightsOutDelay) * _random.NextDouble();
        IsStarted = true;

        if (formationLap)
        {
            StartPhase = RaceStartPhase.FormationLap;
            Notify(RaceEventType.FormationLap,
                $"Прогрівальне коло: {Track.GrandPrix}. Пілоти прогрівають шини й гальма, попереду — {_cars[0].Driver.FullName}.",
                _cars[0]);
        }
        else
        {
            BeginRace();
        }
    }

    /// <summary>
    /// Прогрівальне коло: боліди по черзі рушають з решітки, їдуть повільно без обгонів,
    /// «виляють» і гріють шини, а наприкінці кола стають точно на свої місця.
    /// </summary>
    private void StepFormationLap(double dt)
    {
        PreRaceSeconds += dt;
        _phaseClock += dt;
        double lapTime = Track.TypicalLapSeconds / FormationLapPace;

        for (int i = 0; i < _cars.Count; i++)
        {
            Car car = _cars[i];
            double progress = _formation[car];
            double moving = _phaseClock - i * FormationDepartureSeconds;       // скільки секунд болід уже їде
            if (progress >= 1 || moving <= 0)
            {
                car.Telemetry = GridIdle;
                continue;
            }

            double fraction = Polyline.Wrap(car.Distance + progress);
            double rel = Track.Profile.RelativeSpeedAt(fraction);
            double cruise = (0.45 + 0.55 * rel) / lapTime;                     // кола за секунду, повороти згладжено
            double pullAway = Math.Min(1, moving / 6);                          // плавно рушає з місця
            double remaining = 1 - progress;
            double stopping = Math.Min(1, Math.Sqrt(remaining / FormationBrakingLaps));
            double delta = dt * cruise * Math.Max(0.04, pullAway * stopping);
            if (delta >= remaining - 1e-5) delta = remaining;                   // точно на своє місце
            _formation[car] = progress + delta;

            car.UpdateSystems(new ComponentContext(dt, delta * FuelTank.EasyLapFactor, Track.AirTemperature,
                FormationPush, false, IsRacing: true));
            car.Telemetry = progress + delta >= 1
                ? GridIdle
                : FormationTelemetry(delta / dt * Track.LengthKm * 3600, stopping < 1, fraction);
        }

        if (_cars.All(c => _formation[c] >= 1))
        {
            StartPhase = RaceStartPhase.StartLights;
            _phaseClock = 0;
            Notify(RaceEventType.StartLights,
                "Усі боліди на своїх місцях. Стартова процедура: зараз загоряться вогні!", _cars[0]);
        }
    }

    /// <summary>Телеметрія на прогрівальному колі: невисока швидкість, низькі передачі, частково газ.</summary>
    private CarTelemetry FormationTelemetry(double kmh, bool stopping, double fraction)
    {
        var sample = Track.Profile.SampleAt(fraction, Track.TypicalLapSeconds);
        bool brake = stopping || sample.Brake;
        int gear = Math.Clamp(1 + (int)(kmh / 35), 1, 7);
        int rpm = (int)Math.Clamp(6000 + kmh * 30, 6000, 11000);
        return new CarTelemetry(kmh, gear, rpm, brake ? 0 : 30, brake);
    }

    /// <summary>
    /// Стартові вогні: пауза, п'ять червоних вогнів по одному щосекунди,
    /// випадкова пауза — і всі вогні гаснуть. У цю мить починається гонка.
    /// </summary>
    private void StepStartLights(double dt)
    {
        double outAt = GridSettleSeconds + (StartLightCount - 1) * LightIntervalSeconds + _lightsOutDelay;
        double before = _phaseClock;
        _phaseClock = Math.Min(outAt, before + dt);
        PreRaceSeconds += _phaseClock - before;

        // Коли горять вогні, пілоти вмикають першу передачу і тримають оберти.
        bool revving = StartLightsOn > 0;
        foreach (Car car in _cars)
        {
            car.UpdateSystems(new ComponentContext(_phaseClock - before, 0, Track.AirTemperature, 0, false, IsRacing: false));
            car.Telemetry = revving ? new CarTelemetry(0, 1, 10500, 40, false) : GridIdle;
        }

        if (_phaseClock < outAt - 1e-9) return;
        BeginRace();
        double rest = before + dt - outAt;
        if (rest > 1e-9) StepInternal(rest);
    }

    /// <summary>Вогні згасли: старт гонки з місця.</summary>
    private void BeginRace()
    {
        StartPhase = RaceStartPhase.Racing;
        ElapsedSeconds = 0;
        _phaseClock = 0;
        foreach (Car car in _cars)
        {
            _formation[car] = 1;
            car.Timing.Start(0);
            car.Telemetry = new CarTelemetry(0, 1, 11000, 100, false);
        }

        Notify(RaceEventType.RaceStarted,
            $"Вогні згасли — старт! {Track.GrandPrix}, {UkrainianText.Laps(TotalLaps)}. На поулі — {_cars[0].Driver.FullName}.",
            _cars[0]);
        foreach (Car car in _cars.Where(_badStarts.Contains))
            Notify(RaceEventType.Incident, $"{car.Driver.FullName}: поганий старт — пробуксовка, болід втрачає позиції.", car);
    }

    /// <summary>
    /// Старт з місця: болід рушає після реакції пілота і розганяється (швидкість не більша за a·t),
    /// тому перше коло повільніше за наступні. Так само болід розганяється після розвороту.
    /// Повертає множник швидкості (0..1).
    /// </summary>
    private double LaunchFactor(Car car, double fraction, double dt, double lapTime)
    {
        double t = ElapsedSeconds - dt / 2 - _launchFrom.GetValueOrDefault(car);
        if (t <= 0) return 0;
        double limit = LaunchAccelerationKmhPerSecond * _launchGrip.GetValueOrDefault(car, 1) * t;
        double speed = Track.Profile.IsEmpty ? 220 : Track.Profile.SampleAt(fraction, lapTime).Speed;
        return speed <= 0 || limit >= speed ? 1 : Math.Max(0.02, limit / speed);
    }

    /// <summary>
    /// На розгоні до першого повороту боліди йдуть поруч, тому позиції вільно змінюються
    /// (хто краще стартував, той і попереду). Далі діють звичайні правила обгону.
    /// </summary>
    private bool IsFreeStart(Car car) => car.Distance < _startZoneEnd;

    /// <summary>Де закінчується розгін до першого повороту: трохи після першої зони гальмування.</summary>
    private static double StartZoneEnd(IReadOnlyList<OvertakingZone> zones)
    {
        double first = zones.Select(z => z.Fraction).Where(f => f > 0.02).DefaultIfEmpty(0.1).Min();
        return Math.Clamp(first, 0.05, 0.3);
    }
}
