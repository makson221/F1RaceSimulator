using F1RaceSimulator.Core.Geometry;
using F1RaceSimulator.Core.Models;
using F1RaceSimulator.Core.Tyres;

namespace F1RaceSimulator.Core.Simulation;

/// <summary>
/// Нейтралізація гонки. Після аварії виїжджає сейфті-кар: лідер їде за ним, решта
/// наздоганяє і вишиковується колоною, обгони заборонені, піт-стоп коштує мало часу.
/// Якщо болід просто зупинився на трасі — віртуальний сейфті-кар (VSC): усі знижують темп.
/// </summary>
public sealed partial class Race
{
    /// <summary>Коло за сейфті-каром повільніше за гоночне в стільки разів.</summary>
    public const double SafetyCarLapFactor = 1.45;

    /// <summary>Під VSC коло повільніше за гоночне в стільки разів.</summary>
    public const double VscLapFactor = 1.35;

    /// <summary>Дистанція між болідами в колоні за сейфті-каром, секунди.</summary>
    public const double SafetyCarGapSeconds = 0.8;

    /// <summary>Темп, з яким боліди наздоганяють колону за сейфті-каром (у стільки разів повільніше за гоночний).</summary>
    public const double CatchUpLapFactor = 1.15;

    private readonly HashSet<Car> _neutralizedStops = new();
    private int _safetyCarEndLap;
    private double _vscUntil;
    private int _greenFromLap;
    private bool _safetyCarInAnnounced;

    /// <summary>Чи нейтралізовано зараз гонку: сейфті-кар, віртуальний сейфті-кар або ні.</summary>
    public SafetyCarMode SafetyCar { get; private set; } = SafetyCarMode.None;

    /// <summary>Скільки разів у гонці виїжджав сейфті-кар.</summary>
    public int SafetyCarCount { get; private set; }

    /// <summary>Скільки разів оголошували віртуальний сейфті-кар.</summary>
    public int VirtualSafetyCarCount { get; private set; }

    /// <summary>Де на колі зараз машина безпеки (null — її немає на трасі). Вона їде трохи попереду лідера.</summary>
    public double? SafetyCarFraction
    {
        get
        {
            if (SafetyCar != SafetyCarMode.SafetyCar) return null;
            Car? leader = _standings.FirstOrDefault(c => c.State == CarState.OnTrack);
            return leader == null ? null : Polyline.Wrap(leader.Distance + 0.006);
        }
    }

    /// <summary>Виїзд сейфті-кара. Якщо він уже на трасі — нейтралізацію подовжують.</summary>
    private void DeploySafetyCar(string reason)
    {
        if (IsChequeredFlag || !IsRacing) return;
        int leaderLaps = Leader.CompletedLaps;
        if (SafetyCar == SafetyCarMode.SafetyCar)
        {
            _safetyCarEndLap = Math.Max(_safetyCarEndLap, leaderLaps + 2);
            _safetyCarInAnnounced = false;
            return;
        }

        SafetyCar = SafetyCarMode.SafetyCar;
        SafetyCarCount++;
        _safetyCarEndLap = leaderLaps + 3 + (int)(_random.NextDouble() * 3);    // 3–5 кіл
        _safetyCarInAnnounced = false;
        _neutralizedStops.Clear();
        _passes.Clear();
        Notify(RaceEventType.SafetyCar, $"Сейфті-кар виїжджає на трасу: {reason}. Обгони заборонені, пелотон збирається в колону.");
    }

    /// <summary>Віртуальний сейфті-кар на 1–2 кола, якщо зараз немає іншої нейтралізації.</summary>
    private void DeployVirtualSafetyCar(string reason)
    {
        if (IsChequeredFlag || !IsRacing || SafetyCar != SafetyCarMode.None) return;
        SafetyCar = SafetyCarMode.Virtual;
        VirtualSafetyCarCount++;
        _vscUntil = ElapsedSeconds + Track.TypicalLapSeconds * VscLapFactor * (0.8 + 0.8 * _random.NextDouble());
        _neutralizedStops.Clear();
        _passes.Clear();
        Notify(RaceEventType.SafetyCar, $"Віртуальний сейфті-кар: {reason}. Усі пілоти знижують темп.");
    }

    /// <summary>VSC знімають через заданий час: гонка продовжується з того місця, де кожен був.</summary>
    private void UpdateVirtualSafetyCar()
    {
        if (SafetyCar != SafetyCarMode.Virtual || ElapsedSeconds < _vscUntil) return;
        SafetyCar = SafetyCarMode.None;
        _greenFromLap = 0;
        Notify(RaceEventType.SafetyCar, "VSC завершено — зелені прапори, боротьба триває!");
    }

    /// <summary>Лідер почав нове коло: сейфті-кар оголошує заїзд у бокси або дає рестарт.</summary>
    private void OnLeaderLap(int lapsDone)
    {
        if (SafetyCar != SafetyCarMode.SafetyCar) return;
        if (lapsDone >= _safetyCarEndLap)
        {
            SafetyCar = SafetyCarMode.None;
            _greenFromLap = lapsDone;                 // обганяти можна, лише перетнувши лінію
            Notify(RaceEventType.SafetyCar, $"Рестарт! Зелені прапори, лідирує {Leader.Driver.FullName}.", Leader);
        }
        else if (lapsDone == _safetyCarEndLap - 1 && !_safetyCarInAnnounced)
        {
            _safetyCarInAnnounced = true;
            Notify(RaceEventType.SafetyCar, "Сейфті-кар заїде в бокси наприкінці цього кола — готуємося до рестарту.");
        }
    }

    /// <summary>Клетчастий прапор під час нейтралізації: гонка завершується за сейфті-каром.</summary>
    private void EndNeutralizationAtFlag()
    {
        if (SafetyCar == SafetyCarMode.None) return;
        Notify(RaceEventType.SafetyCar, SafetyCar == SafetyCarMode.SafetyCar
            ? "Гонка завершується за сейфті-каром."
            : "Гонка завершується під віртуальним сейфті-каром.");
        SafetyCar = SafetyCarMode.None;
    }

    /// <summary>
    /// Темп під сейфті-каром: лідер їде за машиною безпеки, інші наздоганяють колону
    /// і далі тримають дистанцію до боліда попереду.
    /// </summary>
    private double SafetyCarLapTime(Car car, double lapTime, Car? carAhead)
    {
        double safetyCarLap = Track.TypicalLapSeconds * SafetyCarLapFactor;
        if (carAhead == null) return Math.Max(lapTime, safetyCarLap);
        double gapSeconds = Polyline.Wrap(carAhead.Distance - car.Distance) * safetyCarLap;
        return gapSeconds > SafetyCarGapSeconds + 0.5
            ? Math.Max(lapTime, Track.TypicalLapSeconds * CatchUpLapFactor)
            : Math.Max(lapTime, safetyCarLap);
    }

    /// <summary>Найближчий болід попереду на трасі (з будь-якого кола) — за ним шикуються в колону.</summary>
    private Car? PhysicallyAhead(Car car)
    {
        Car? best = null;
        double bestGap = double.MaxValue;
        foreach (Car other in _cars)
        {
            if (other == car || other.State != CarState.OnTrack) continue;
            double gap = Polyline.Wrap(other.Distance - car.Distance);
            if (gap > 1e-9 && gap < bestGap)
            {
                bestGap = gap;
                best = other;
            }
        }
        return best;
    }

    /// <summary>
    /// Чи варто скористатися нейтралізацією для піт-стопу: шини вже помітно зношені або
    /// ще не виконано правило двох складів, а на свіжих жорстких можна доїхати до фінішу;
    /// до фінішу ще далеко, напарник зараз не в боксах.
    /// </summary>
    private bool IsCheapStop(Car car, int lapsRemaining)
    {
        if (SafetyCar == SafetyCarMode.None || _neutralizedStops.Contains(car) || lapsRemaining <= 6) return false;
        double hardStint = TyreFactory.Create(TyreCompound.Hard).EstimatedLapsLeft(Track.TyreWearFactor, car.TyreWearRate) * 0.75;
        bool needTyres = car.Tyre.Condition < 65 || (!car.HasUsedTwoCompounds && hardStint >= lapsRemaining);
        bool teammateInPits = _cars.Any(c => c != car && c.Team == car.Team && c.IsInPits);
        return needTyres && !teammateInPits;
    }
}
