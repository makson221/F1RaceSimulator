using F1RaceSimulator.Core.Geometry;

namespace F1RaceSimulator.Core.Simulation;

/// <summary>
/// Обгони та інциденти. Болід не може просто «проїхати крізь» суперника: наздогнавши його,
/// він їде слідом у турбулентному повітрі, а обігнати може лише в зоні гальмування після прямої.
/// Шанс залежить від траси (Монако чи Монца), переваги в темпі (шини, болід, пілот),
/// дистанції до суперника і майстерності обох пілотів у боротьбі.
/// </summary>
public sealed partial class Race
{
    /// <summary>Найменша дистанція до суперника, ближче якої болід позаду не може під'їхати, секунди.</summary>
    public const double MinFollowSeconds = 0.25;

    /// <summary>Спробувати обгін можна, якщо суперник ближче ніж за стільки секунд.</summary>
    public const double AttackWindowSeconds = 1.0;

    /// <summary>Скільки триває сам маневр обгону, секунди.</summary>
    public const double PassDurationSeconds = 2.5;

    /// <summary>На скільки секунд обгінник опиняється попереду після маневру.</summary>
    public const double PassMarginSeconds = 0.3;

    /// <summary>Імовірність контакту під час невдалої спроби обгону (за стандартної частоти сходів).</summary>
    public const double ContactChance = 0.0003;

    /// <summary>Імовірність помилки пілота (розворот або аварія) за коло без суперника поруч.</summary>
    public const double SoloIncidentChancePerLap = 0.0003;

    /// <summary>Скільки секунд болід стоїть після розвороту, перш ніж рушити знову (до випадкової добавки).</summary>
    private const double SpinStopSeconds = 1.5;

    /// <summary>Маневр обгону: скільки кола ще належить «додати» обгінникові, щоб пройти суперника.</summary>
    private readonly record struct PassManeuver(Car Target, double RemainingLaps, double LapsPerSecond);

    private readonly Dictionary<Car, PassManeuver> _passes = new();

    /// <summary>Хто кого переслідує і з якого моменту (щоб помітити, що болід застряг позаду).</summary>
    private readonly Dictionary<Car, (Car Rival, double Since)> _battles = new();

    /// <summary>До якого моменту болід після розвороту ще набирає швидкість (його можна вільно об'їхати).</summary>
    private readonly Dictionary<Car, double> _recoverUntil = new();

    /// <summary>
    /// Множник частоти інцидентів: залежить від заданої ймовірності сходів.
    /// Якщо сходи вимкнено (0), аварій і зіткнень теж немає.
    /// </summary>
    private double IncidentScale => Math.Clamp(RetirementChancePerLap / DefaultRetirementChancePerLap, 0, 4);

    /// <summary>Чи можна зараз атакувати: гонка йде, зелені прапори, болід уже перетнув лінію після рестарту.</summary>
    private bool CanAttack(Car car) =>
        !IsChequeredFlag && SafetyCar == SafetyCarMode.None && car.Distance >= _greenFromLap && !IsFreeStart(car);

    /// <summary>Чи триває зараз маневр обгону саме цього суперника.</summary>
    private bool IsPassing(Car car, Car target) => _passes.TryGetValue(car, out var pass) && pass.Target == target;

    /// <summary>Болід після розвороту ще не набрав швидкість — інші можуть його вільно об'їхати.</summary>
    private bool IsRecovering(Car car) => _recoverUntil.TryGetValue(car, out double until) && ElapsedSeconds < until;

    /// <summary>
    /// Спроба обгону: якщо за цей крок болід перетнув точку гальмування в кінці прямої,
    /// маючи суперника ближче ніж за секунду, кидається жереб. Успіх — починається маневр,
    /// невдача — болід лишається позаду (і зрідка зачіпає суперника).
    /// </summary>
    private void TryOvertake(Car car, Car target, double before, double after)
    {
        if (_passes.ContainsKey(car)) return;
        foreach (OvertakingZone zone in OvertakingZones)
        {
            if (!Crossed(before, after, zone.Fraction)) continue;

            // Відставання на початок кроку: суперник уже зробив свій крок, тому віднімаємо його.
            double gap = TimeGap(before, target.Distance - _stepDelta.GetValueOrDefault(target));
            if (gap > AttackWindowSeconds) return;

            double chance = OvertakeChance(car, target, zone.Strength, gap);
            if (_random.NextDouble() < chance)
            {
                double laps = SecondsToLaps(gap + PassMarginSeconds, zone.Fraction, Track.TypicalLapSeconds);
                _passes[car] = new PassManeuver(target, laps, laps / PassDurationSeconds);
            }
            else if (IncidentScale > 0 && _random.NextDouble() < ContactChance * IncidentScale * (car.Distance < 1 ? 3 : 1))
            {
                ResolveContact(car, target);
            }
            return;
        }
    }

    /// <summary>
    /// Шанс обгону в зоні гальмування. Головне — перевага в темпі (секунди на колі):
    /// вона має перевищити поріг траси. На Монці вистачає 0.4 с, у Мельбурні — 0.9 с,
    /// у Будапешті потрібна понад секунда, а в Монако — майже півтори. Далі множники: легкість обгону на трасі,
    /// якість зони, дистанція до суперника, майстерність обох пілотів і заряд ERS.
    /// </summary>
    private double OvertakeChance(Car car, Car target, double zoneStrength, double gapSeconds)
    {
        double delta = _cleanLapTime.TryGetValue(target, out double targetLap) && _cleanLapTime.TryGetValue(car, out double ownLap)
            ? targetLap - ownLap
            : 0;
        double ease = Math.Clamp(Track.Overtaking, 0, 1);
        if (car.Distance < 1) ease = Math.Max(ease, 0.4);        // перше коло: щільна група, холодні шини
        double threshold = PaceAdvantageNeeded(ease);
        double baseChance = Math.Clamp(0.02 + 0.35 * (delta - threshold), 0, 0.85);

        double proximity = gapSeconds <= 0.5 ? 1 : Math.Clamp((AttackWindowSeconds - gapSeconds) / (AttackWindowSeconds - 0.5), 0, 1);
        double craft = Math.Clamp(1 + 2.5 * (car.Driver.Racecraft - target.Driver.Racecraft), 0.6, 1.4);
        double ers = car.Battery.IsDeploying ? 1.15 : 1;
        return Math.Clamp(baseChance * (0.25 + 0.75 * ease) * zoneStrength * proximity * craft * ers, 0, 0.9);
    }

    /// <summary>Яка перевага в темпі (секунди на колі) потрібна, щоб обгін узагалі був можливий.</summary>
    private static double PaceAdvantageNeeded(double ease) => 1.4 * (1 - ease) + 0.1;

    /// <summary>
    /// Чи має сенс атакувати суперника попереду. Перші два кола боротьби пілот тисне завжди;
    /// якщо обігнати так і не вдалося, а переваги в темпі для цієї траси замало, він відпускає
    /// суперника на кілька десятих і береже шини, паливо й двигун до кращого моменту.
    /// </summary>
    private bool WorthAttacking(Car car, Car? rival, double gapSeconds)
    {
        if (rival == null || gapSeconds >= DirtyAirGapSeconds)
        {
            _battles.Remove(car);
            return true;
        }
        if (!_battles.TryGetValue(car, out var battle) || battle.Rival != rival)
            _battles[car] = battle = (rival, ElapsedSeconds);

        if (ElapsedSeconds - battle.Since < 2 * Track.TypicalLapSeconds) return true;
        double delta = _cleanLapTime.GetValueOrDefault(rival) - _cleanLapTime.GetValueOrDefault(car);
        return delta >= 0.5 * PaceAdvantageNeeded(Math.Clamp(Track.Overtaking, 0, 1));
    }

    /// <summary>
    /// Продовжує маневр обгону: обгінник за рахунок пізнього гальмування і кращого виходу
    /// з повороту за кілька секунд проходить суперника. Повертає додаткову дистанцію на цьому кроці.
    /// </summary>
    private double ContinuePass(Car car, double dt, double launch)
    {
        if (!_passes.TryGetValue(car, out var pass)) return 0;
        if (pass.Target.State != CarState.OnTrack || SafetyCar != SafetyCarMode.None)
        {
            _passes.Remove(car);
            return 0;
        }

        double extra = Math.Min(pass.RemainingLaps, pass.LapsPerSecond * dt * launch);
        double left = pass.RemainingLaps - extra;
        if (left <= 1e-9) _passes.Remove(car);
        else _passes[car] = pass with { RemainingLaps = left };
        return extra;
    }

    /// <summary>
    /// Контакт під час обгону: найчастіше обгінник розвертається і втрачає час,
    /// інколи зламане крило чи підвіска означають сход — тоді виїжджає сейфті-кар.
    /// </summary>
    private void ResolveContact(Car attacker, Car defender)
    {
        double roll = _random.NextDouble();
        if (roll < 0.45)
        {
            attacker.Retire($"зіткнення з {defender.Driver.LastName}");
            _passes.Remove(attacker);
            Notify(RaceEventType.Retirement,
                $"{attacker.Driver.FullName} — сход: зіткнення з {defender.Driver.FullName} під час атаки!", attacker, defender);
            DeploySafetyCar($"аварія {attacker.Driver.LastName}");
        }
        else if (roll < 0.55)
        {
            attacker.Retire($"зіткнення з {defender.Driver.LastName}");
            defender.Retire($"зіткнення з {attacker.Driver.LastName}");
            _passes.Remove(attacker);
            _passes.Remove(defender);
            Notify(RaceEventType.Retirement,
                $"Аварія! {attacker.Driver.FullName} і {defender.Driver.FullName} зіткнулися — обидва сходять.", attacker, defender);
            DeploySafetyCar($"аварія {attacker.Driver.LastName} і {defender.Driver.LastName}");
        }
        else
        {
            Spin(attacker, $"контакт із {defender.Driver.LastName}, розворот");
        }
    }

    /// <summary>
    /// Помилка пілота: розворот (болід зупиняється і знову розганяється) або аварія.
    /// Ризик вищий в атаці, на зношених шинах і в нестабільних пілотів. Повертає true, якщо болід зійшов.
    /// </summary>
    private bool CheckSoloIncident(Car car, double lapsDelta)
    {
        if (IncidentScale <= 0 || car.Distance < 0.2 || IsRecovering(car)) return false;
        double risk = SoloIncidentChancePerLap * IncidentScale * lapsDelta * (2 - car.Driver.Consistency)
                      * (car.Mode == DrivingMode.Attack ? 1.5 : 1)
                      * (car.Tyre.Condition < 15 ? 2 : 1);
        if (_random.NextDouble() >= risk) return false;

        if (_random.NextDouble() < 0.3)
        {
            car.Retire("аварія");
            _passes.Remove(car);
            Notify(RaceEventType.Retirement, $"{car.Driver.FullName} — аварія, болід у бар'єрі!", car);
            DeploySafetyCar($"аварія {car.Driver.LastName}");
            return true;
        }

        Spin(car, "помилка, розворот");
        return false;
    }

    /// <summary>Розворот: болід зупиняється на 1.5–3 с і розганяється знову, втрачаючи час і позиції.</summary>
    private void Spin(Car car, string what)
    {
        _launchFrom[car] = ElapsedSeconds + SpinStopSeconds + 1.5 * _random.NextDouble();
        _recoverUntil[car] = _launchFrom[car] + 5;
        _passes.Remove(car);
        Notify(RaceEventType.Incident, $"{car.Driver.FullName}: {what}. Жовті прапори в секторі {Track.SectorAt(Polyline.Wrap(car.Distance))}.", car);
    }
}
