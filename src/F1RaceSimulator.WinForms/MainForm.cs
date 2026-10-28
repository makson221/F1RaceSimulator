using F1RaceSimulator.Core.Components;
using F1RaceSimulator.Core.Models;
using F1RaceSimulator.Core.Season;
using F1RaceSimulator.Core.Simulation;

namespace F1RaceSimulator.WinForms;

/// <summary>
/// Головне вікно програми. Веде гоночний вікенд: спочатку кваліфікація (Q1 → Q2 → Q3),
/// потім гонка з колом охолодження й парк-ферме, після якої пілоти отримують очки
/// чемпіонату і можна переходити до наступного етапу.
/// Реалізує <see cref="IRaceObserver"/> та <see cref="IQualifyingObserver"/> (патерн Observer).
/// </summary>
public partial class MainForm : Form, IRaceObserver, IQualifyingObserver
{
    /// <summary>Етап вікенду, що зараз показується.</summary>
    private enum WeekendPhase { Qualifying, Race }

    /// <summary>Варіанти швидкості: у скільки разів симуляція швидша за реальний час.</summary>
    private static readonly (string Text, double Scale)[] Speeds =
    {
        ("×1", 1), ("×10", 10), ("×25", 25), ("×50", 50), ("×100", 100), ("×200", 200)
    };

    private const int MaxLogLines = 400;
    private const double FlashSeconds = 2.5;

    private static readonly Color SelectedRowColor = Color.FromArgb(217, 231, 248);
    private static readonly Color GainedRowColor = Color.FromArgb(219, 241, 226);
    private static readonly Color LostRowColor = Color.FromArgb(251, 226, 226);
    private static readonly Color DropZoneColor = Color.FromArgb(253, 240, 240);
    private static readonly Color SoftColor = Color.FromArgb(196, 38, 38);
    private static readonly Color MediumColor = Color.FromArgb(168, 115, 0);
    private static readonly Color WarningColor = Color.FromArgb(154, 88, 0);
    private static readonly Color DangerColor = Color.FromArgb(179, 34, 34);
    private static readonly Color GoodColor = Color.FromArgb(22, 112, 59);
    private static readonly Color PurpleColor = Color.FromArgb(128, 0, 160);
    private static readonly Color MutedColor = Color.FromArgb(120, 128, 140);

    private readonly SeasonData _data;
    private readonly IRandomSource _random = new SystemRandomSource();
    private readonly EventLogObserver _log = new();
    private readonly Dictionary<Car, int> _lastPositions = new();
    private readonly Dictionary<Car, int> _positionDelta = new();
    private readonly Dictionary<Car, DateTime> _flashUntil = new();
    private readonly string _autosavePath;

    private WeekendPhase _phase;
    private QualifyingSession? _qualifying;
    private Race? _race;
    private Font? _boldFont;
    private Track _track;
    private Car? _selected;
    private double _timeScale = 25;
    private bool _running;
    private bool _suppressEvents;
    private bool _qualifyingDonePending;
    private bool _finishPending;
    private bool _parkedPending;
    private DateTime _lastTick;
    private int _tickCounter;

    public MainForm(SeasonData data)
    {
        _data = data ?? throw new ArgumentNullException(nameof(data));
        InitializeComponent();
        GridStyle.EnableDoubleBuffering(dgvStandings);
        SetupGrids();

        _autosavePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "F1RaceSimulator", "championship-2026.json");
        LoadAutosave();

        _log.EventAdded += (_, e) => AppendLog(e.ToString());
        Championship.Instance.Changed += (_, _) => RefreshSeason();

        _suppressEvents = true;
        foreach (var speed in Speeds)
            tscSpeed.Items.Add(speed.Text);
        tscSpeed.SelectedIndex = 2;

        foreach (Track track in _data.Tracks)
            tscTrack.Items.Add(track);

        // За замовчуванням — перший незіграний етап, а якщо сезон не розпочато — найближчий за календарем.
        _track = Championship.Instance.Races.Count > 0
            ? _data.Tracks.FirstOrDefault(t => !Championship.Instance.HasRound(t.Round)) ?? _data.Tracks[^1]
            : _data.NextTrack(DateOnly.FromDateTime(DateTime.Today));
        tscTrack.SelectedItem = _track;
        _suppressEvents = false;

        PrepareWeekend();
        RefreshSeason();
    }

    /// <summary>Поточна сесія на трасі (кваліфікація або гонка).</summary>
    private ITrackSession? Session => _phase == WeekendPhase.Qualifying ? _qualifying : _race;

    // ------------------------------------------------------------------
    // Підготовка вікенду
    // ------------------------------------------------------------------

    /// <summary>Створює боліди і кваліфікацію; гонка з'явиться після Q3.</summary>
    private void PrepareWeekend()
    {
        SetRunning(false);
        string? selectedCode = _selected?.Driver.Code;
        DetachSessions();

        _log.Clear();
        lstLog.Items.Clear();
        ResetFlashes();
        _qualifyingDonePending = _finishPending = _parkedPending = false;

        var cars = CarFactory.CreateCars(_data);
        _qualifying = new QualifyingSession(_track, cars, _random);
        _qualifying.Subscribe(_log);
        _qualifying.Subscribe(this);
        _qualifying.Start();
        _race = null;
        _phase = WeekendPhase.Qualifying;

        _selected = cars.FirstOrDefault(c => c.Driver.Code == selectedCode) ?? cars[0];

        trackPanel.Track = _track;
        trackPanel.Session = _qualifying;
        trackPanel.SelectedCar = _selected;

        Text = $"F1 Race Simulator — {_track.GrandPrix} 2026, {_track.City}";
        grpTrack.Text = $"{_track.Circuit} — {_track.LengthKm:0.000} км, {UkrainianText.Laps(_track.Laps)}, {_track.Layout.Turns.Length} поворотів";
        tslWeather.Text = "Погода: " + _track.Weather;

        ShowPhaseGrid();
        RefreshAll();
    }

    /// <summary>Ставить боліди на решітку за результатами кваліфікації (гонка на паузі).</summary>
    private void PrepareRace()
    {
        if (_qualifying == null) return;
        SetRunning(false);

        IReadOnlyList<QualifyingResult> results = _qualifying.GetResults();
        var grid = CarFactory.CreateGrid(_data, results);
        _qualifying.Unsubscribe(_log);
        _qualifying.Unsubscribe(this);

        _race = new Race(_track, grid, _random);
        _race.Subscribe(_log);
        _race.Subscribe(this);
        _race.Start();
        _phase = WeekendPhase.Race;

        ResetFlashes();
        foreach (Car car in grid)
            _lastPositions[car] = car.Position;

        string? code = _selected?.Driver.Code;
        _selected = grid.FirstOrDefault(c => c.Driver.Code == code) ?? grid[0];
        trackPanel.Session = _race;
        trackPanel.SelectedCar = _selected;

        AppendLog($"Стартова решітка: поул — {results[0].Car.Driver.FullName} ({results[0].LapTimeText}). Натисніть «Старт гонки».");
        ShowPhaseGrid();
        RefreshAll();
    }

    private void DetachSessions()
    {
        if (_qualifying != null)
        {
            _qualifying.Unsubscribe(_log);
            _qualifying.Unsubscribe(this);
        }
        if (_race != null)
        {
            _race.Unsubscribe(_log);
            _race.Unsubscribe(this);
        }
    }

    private void ResetFlashes()
    {
        _lastPositions.Clear();
        _positionDelta.Clear();
        _flashUntil.Clear();
    }

    /// <summary>У вкладці «Позиції» показуємо таблицю кваліфікації або гонки.</summary>
    private void ShowPhaseGrid()
    {
        bool qualifying = _phase == WeekendPhase.Qualifying;
        dgvQualifying.Visible = qualifying;
        dgvStandings.Visible = !qualifying;
        tabPositions.Text = qualifying ? "Кваліфікація" : "Гонка";
        dgvTelemetry.Rows.Clear();
    }

    // ------------------------------------------------------------------
    // Таймер симуляції
    // ------------------------------------------------------------------

    private void TimerSim_Tick(object? sender, EventArgs e)
    {
        ITrackSession? session = Session;
        if (session == null || !_running) return;

        DateTime now = DateTime.UtcNow;
        double realSeconds = Math.Min(0.1, (now - _lastTick).TotalSeconds);  // захист від «стрибка» після паузи
        _lastTick = now;

        session.Step(realSeconds * _timeScale);
        trackPanel.Invalidate();

        if (++_tickCounter % 4 == 0 || session.IsCompleted)
            RefreshAll();
        else
            telemetryPanel.Invalidate();

        HandlePendingEvents();
    }

    /// <summary>Обробляє важливі події вже після кроку симуляції (не посеред нього).</summary>
    private void HandlePendingEvents()
    {
        if (_qualifyingDonePending)
        {
            _qualifyingDonePending = false;
            PrepareRace();
        }
        if (_finishPending)
        {
            _finishPending = false;
            OnRaceFinished();
        }
        if (_parkedPending)
        {
            _parkedPending = false;
            OnCarsParked();
        }
    }

    private void SetRunning(bool running)
    {
        _running = running && Session is { IsCompleted: false };
        if (_running)
        {
            _lastTick = DateTime.UtcNow;
            timerSim.Start();
        }
        else
        {
            timerSim.Stop();
        }
        UpdateButtons();
        RefreshStatus();
    }

    private void UpdateButtons()
    {
        string text;
        if (_phase == WeekendPhase.Qualifying)
        {
            text = _running ? "Пауза" : _qualifying is { ElapsedSeconds: > 0 } ? "Продовжити" : "Старт кваліфікації";
        }
        else if (_race is { IsCompleted: true }) text = "Гонку завершено";
        else if (_running) text = "Пауза";
        else text = _race is { ElapsedSeconds: > 0 } ? "Продовжити" : "Старт гонки";

        tsbStartPause.Text = (_running ? "❚❚ " : "▶ ") + text;
        mnuStartPause.Text = text;
        tsbStartPause.Enabled = mnuStartPause.Enabled = Session is { IsCompleted: false };
        // Кнопка пропуску потрібна лише під час кваліфікації — у гонці ховаємо її, щоб звільнити місце.
        tsbSkipQualifying.Visible = _phase == WeekendPhase.Qualifying;
        tsbSkipQualifying.Enabled = mnuSkipQualifying.Enabled = _phase == WeekendPhase.Qualifying && _qualifying is { IsCompleted: false };

        Track? next = _data.NextTrackAfter(_track);
        tsbNextRound.Enabled = mnuNextRound.Enabled = next != null;
        tsbNextRound.ToolTipText = next == null ? "Це останній етап сезону" : "Наступний етап: " + next.GrandPrix;
    }

    // ------------------------------------------------------------------
    // Observer: події кваліфікації та гонки
    // ------------------------------------------------------------------

    public void OnQualifyingEvent(QualifyingSession session, RaceEvent qualifyingEvent)
    {
        if (qualifyingEvent.Type == RaceEventType.QualifyingFinished)
            _qualifyingDonePending = true;
    }

    /// <summary>Фініш обробляємо після кроку симуляції: очки — одразу, підсумки — коли всі в парк-ферме.</summary>
    public void OnRaceEvent(Race race, RaceEvent raceEvent)
    {
        if (raceEvent.Type == RaceEventType.RaceFinished)
            _finishPending = true;
        else if (raceEvent.Type == RaceEventType.CarsParked)
            _parkedPending = true;
    }

    /// <summary>Протокол остаточний — нараховуємо очки і зберігаємо чемпіонат.</summary>
    private void OnRaceFinished()
    {
        if (_race == null) return;
        Championship.Instance.AddRaceResult(_track, _race.GetResults());
        SaveAutosave();
        RefreshAll();
    }

    /// <summary>Усі фінішери в парк-ферме — показуємо підсумки етапу.</summary>
    private void OnCarsParked()
    {
        if (_race == null) return;
        SetRunning(false);
        RefreshAll();

        Track? next = _data.NextTrackAfter(_track);
        using var form = new ResultsForm(_race, next);
        if (form.ShowDialog(this) == DialogResult.OK && next != null)
            GoToTrack(next);
    }

    // ------------------------------------------------------------------
    // Оновлення інтерфейсу
    // ------------------------------------------------------------------

    private void SetupGrids()
    {
        GridStyle.Setup(dgvQualifying,
            ("", 1), ("Поз", 8), ("Пілот", 20), ("Команда", 18), ("Q1", 13), ("Q2", 13), ("Q3", 13), ("Стан", 17));
        GridStyle.Setup(dgvTelemetry,
            ("", 1), ("Поз", 6), ("Пілот", 11), ("км/год", 9), ("Пер.", 6), ("Об/хв", 9), ("Газ", 7), ("Гальмо", 8),
            ("Режим", 12), ("Шини", 9), ("Шини °C", 9), ("Мотор °C", 9), ("Паливо", 9), ("ERS", 7),
            ("Останнє коло", 12), ("Найкраще", 12));
        GridStyle.Setup(dgvSeason,
            ("Поз", 8), ("Пілот", 30), ("Команда", 26), ("Очки", 10), ("Перемоги", 12), ("Подіуми", 12));
        dgvSeason.CellClick += (_, _) => dgvSeason.ClearSelection();
    }

    private void RefreshAll()
    {
        if (_phase == WeekendPhase.Qualifying) RefreshQualifying();
        else RefreshStandings();
        if (tabRight.SelectedTab == tabTelemetry) RefreshTelemetryGrid();
        RefreshDriverCard();
        RefreshStatus();
        UpdateButtons();
        trackPanel.Invalidate();
    }

    /// <summary>Таблиця кваліфікації: Q1/Q2/Q3, зона вибування підсвічена червоним.</summary>
    private void RefreshQualifying()
    {
        if (_qualifying == null) return;
        IReadOnlyList<QualifyingCar> standings = _qualifying.Standings;
        GridStyle.EnsureRows(dgvQualifying, standings.Count);

        // Найкращий час у кожному сегменті — фіолетовим, як на табло F1.
        var segments = new[] { QualifyingSegment.Q1, QualifyingSegment.Q2, QualifyingSegment.Q3 };
        double?[] fastest = segments.Select(s => standings.Select(q => q.Best(s)).Where(t => t != null).Min()).ToArray();
        int advance = _qualifying.AdvanceCount(_qualifying.Segment);
        _boldFont ??= new Font(dgvStandings.Font, FontStyle.Bold);

        for (int i = 0; i < standings.Count; i++)
        {
            QualifyingCar q = standings[i];
            DataGridViewRow row = dgvQualifying.Rows[i];
            row.Tag = q.Car;
            Color team = trackPanel.ColorOf(q.Car.Team);
            row.Cells[0].Style.BackColor = row.Cells[0].Style.SelectionBackColor = team;
            row.Cells[1].Value = q.Position;
            row.Cells[2].Value = q.Car.Driver.LastName;
            row.Cells[3].Value = q.Car.Team.Name;
            for (int s = 0; s < 3; s++)
            {
                double? time = q.Best(segments[s]);
                DataGridViewCell cell = row.Cells[4 + s];
                cell.Value = time == null ? "" : LapTimer.Format(time);
                cell.Style.ForeColor = time != null && time == fastest[s] ? PurpleColor : Color.Black;
            }
            row.Cells[7].Value = q.StageText;
            row.Cells[7].Style.ForeColor = q.Stage == QualifyingStage.FlyingLap ? GoodColor : MutedColor;

            bool inDropZone = !q.IsEliminated && !_qualifying.IsCompleted && q.Position > advance && advance > 0;
            Color back = q.Car == _selected ? SelectedRowColor : inDropZone ? DropZoneColor : Color.White;
            row.DefaultCellStyle.BackColor = row.DefaultCellStyle.SelectionBackColor = back;
            row.DefaultCellStyle.ForeColor = q.IsEliminated ? Color.Gray : Color.Black;
            row.DefaultCellStyle.Font = q.Car == _selected ? _boldFont : null;
        }
        dgvQualifying.ClearSelection();
    }

    /// <summary>Таблиця гонки; підсвічує тих, хто виграв або втратив місця.</summary>
    private void RefreshStandings()
    {
        if (_race == null) return;
        IReadOnlyList<Car> standings = _race.Standings;
        DateTime now = DateTime.UtcNow;
        GridStyle.EnsureRows(dgvStandings, standings.Count);
        _boldFont ??= new Font(dgvStandings.Font, FontStyle.Bold);

        for (int i = 0; i < standings.Count; i++)
        {
            Car car = standings[i];
            DataGridViewRow row = dgvStandings.Rows[i];
            row.Tag = car;

            // Зміна позиції: накопичуємо різницю, поки рядок підсвічений.
            if (_lastPositions.TryGetValue(car, out int previous) && previous != car.Position)
            {
                bool stillFlashing = _flashUntil.TryGetValue(car, out var until) && until > now;
                int delta = (stillFlashing ? _positionDelta.GetValueOrDefault(car) : 0) + (previous - car.Position);
                _positionDelta[car] = delta;
                _flashUntil[car] = now.AddSeconds(FlashSeconds);
            }
            _lastPositions[car] = car.Position;

            bool flashing = _flashUntil.TryGetValue(car, out var flashUntil) && flashUntil > now;
            int change = flashing ? _positionDelta.GetValueOrDefault(car) : 0;

            string arrow = change > 0 ? $"  ▲{change}" : change < 0 ? $"  ▼{-change}" : string.Empty;
            bool retired = car.State == CarState.Retired;
            bool finished = car.State == CarState.Finished;

            row.Cells[colColor.Index].Style.BackColor = trackPanel.ColorOf(car.Team);
            row.Cells[colColor.Index].Style.SelectionBackColor = trackPanel.ColorOf(car.Team);
            row.Cells[colPos.Index].Value = retired ? "—" : car.Position.ToString();
            row.Cells[colDriver.Index].Value = car.Driver.LastName + (finished ? " 🏁" : "") + arrow;
            row.Cells[colTeam.Index].Value = car.Team.Name;
            row.Cells[colGap.Index].Value = _race.GapText(car);
            row.Cells[colPits.Index].Value = car.PitStops.ToString();

            DataGridViewCell tyreCell = row.Cells[colTyre.Index];
            if (car.IsInPits)
            {
                tyreCell.Value = car.State == CarState.PitStop ? "STOP" : "PIT";
                tyreCell.Style.ForeColor = DangerColor;
            }
            else if (retired)
            {
                tyreCell.Value = "—";
                tyreCell.Style.ForeColor = Color.Gray;
            }
            else
            {
                tyreCell.Value = $"{car.Tyre.Letter} {car.Tyre.Condition:0}%";
                tyreCell.Style.ForeColor =
                    car.Tyre.Condition < car.Strategy.Threshold ? DangerColor :
                    car.Tyre.Condition < 50 ? WarningColor :
                    car.Tyre.Compound == TyreCompound.Soft ? SoftColor :
                    car.Tyre.Compound == TyreCompound.Medium ? MediumColor : Color.Black;
            }

            // Очки за поточну позицію (після фінішу — остаточні).
            int points = retired ? 0 : PointsSystem.ForPosition(car.Position);
            DataGridViewCell pointsCell = row.Cells[colPoints.Index];
            pointsCell.Value = points > 0 ? "+" + points : "";
            pointsCell.Style.ForeColor = _race.IsFinished ? GoodColor : MutedColor;

            Color back = car == _selected ? SelectedRowColor
                : change > 0 ? GainedRowColor
                : change < 0 ? LostRowColor
                : Color.White;
            row.DefaultCellStyle.BackColor = back;
            row.DefaultCellStyle.SelectionBackColor = back;
            row.DefaultCellStyle.SelectionForeColor = retired ? Color.Gray : Color.Black;
            row.DefaultCellStyle.ForeColor = retired ? Color.Gray : Color.Black;
            row.DefaultCellStyle.Font = car == _selected ? _boldFont : null;
        }

        dgvStandings.ClearSelection();
    }

    /// <summary>Повна телеметрія всіх пілотів одночасно.</summary>
    private void RefreshTelemetryGrid()
    {
        if (_phase == WeekendPhase.Qualifying && _qualifying != null)
        {
            IReadOnlyList<QualifyingCar> list = _qualifying.Standings;
            GridStyle.EnsureRows(dgvTelemetry, list.Count);
            double now = _qualifying.ElapsedSeconds;
            for (int i = 0; i < list.Count; i++)
            {
                QualifyingCar q = list[i];
                bool moving = q.Stage is not QualifyingStage.Garage;
                string current = q.Stage == QualifyingStage.FlyingLap ? LapTimer.Format(q.Timing.CurrentLapTime(now)) : LapTimer.Format(q.LastLap);
                FillTelemetryRow(dgvTelemetry.Rows[i], q.Car, q.Position, q.Telemetry, q.StageText,
                    moving ? "S нові" : "—", "—", "—", "—", "—", current, LapTimer.Format(q.Timing.BestLap));
            }
        }
        else if (_race != null)
        {
            IReadOnlyList<Car> list = _race.Standings;
            GridStyle.EnsureRows(dgvTelemetry, list.Count);
            for (int i = 0; i < list.Count; i++)
            {
                Car car = list[i];
                bool running = car.IsRunning || car.PostRace is PostRaceStage.CoolDownLap or PostRaceStage.PitLane;
                FillTelemetryRow(dgvTelemetry.Rows[i], car, car.Position, car.Telemetry,
                    car.State == CarState.OnTrack ? TelemetryPanel.ModeText(car) : TelemetryPanel.StateText(car),
                    car.State == CarState.Retired ? "—" : $"{car.Tyre.Letter} {car.Tyre.Condition:0}%",
                    running ? $"{car.Tyre.Temperature:0}" : "—",
                    car.State == CarState.Retired ? "—" : $"{car.Engine.Temperature:0}",
                    $"{car.Fuel.FuelKg:0.0} кг",
                    $"{car.Battery.Charge:0}%",
                    LapTimer.Format(car.Timing.LastLap), LapTimer.Format(car.Timing.BestLap));

                DataGridViewRow row = dgvTelemetry.Rows[i];
                row.Cells[11].Style.ForeColor = car.Engine.Status == ComponentStatus.Normal ? Color.Black : DangerColor;
                row.Cells[10].Style.ForeColor = car.Tyre.IsOverheating ? DangerColor : Color.Black;
                row.Cells[15].Style.ForeColor = car == _race.FastestLapCar ? PurpleColor : Color.Black;
                row.DefaultCellStyle.ForeColor = car.State == CarState.Retired ? Color.Gray : Color.Black;
            }
        }
        dgvTelemetry.ClearSelection();
    }

    private void FillTelemetryRow(DataGridViewRow row, Car car, int position, CarTelemetry t, string mode,
        string tyre, string tyreTemp, string engineTemp, string fuel, string ers, string last, string best)
    {
        row.Tag = car;
        Color team = trackPanel.ColorOf(car.Team);
        row.Cells[0].Style.BackColor = row.Cells[0].Style.SelectionBackColor = team;
        row.Cells[1].Value = car.State == CarState.Retired ? "—" : position.ToString();
        row.Cells[2].Value = car.Driver.Code;
        row.Cells[3].Value = $"{t.SpeedKmh:0}";
        row.Cells[4].Value = t.GearText;
        row.Cells[5].Value = t.Rpm > 0 ? t.Rpm.ToString("N0") : "0";
        row.Cells[6].Value = $"{t.Throttle}%";
        row.Cells[7].Value = t.Brake ? "■ ТАК" : "";
        row.Cells[7].Style.ForeColor = DangerColor;
        row.Cells[6].Style.ForeColor = t.Throttle >= 95 ? GoodColor : Color.Black;
        row.Cells[8].Value = mode;
        row.Cells[9].Value = tyre;
        row.Cells[10].Value = tyreTemp;
        row.Cells[11].Value = engineTemp;
        row.Cells[12].Value = fuel;
        row.Cells[13].Value = ers;
        row.Cells[14].Value = last;
        row.Cells[15].Value = best;
        Color back = car == _selected ? SelectedRowColor : Color.White;
        row.DefaultCellStyle.BackColor = row.DefaultCellStyle.SelectionBackColor = back;
    }

    /// <summary>Вкладка «Чемпіонат»: таблиця пілотів після всіх зіграних етапів.</summary>
    private void RefreshSeason()
    {
        Championship championship = Championship.Instance;
        IReadOnlyList<StandingRow> rows = championship.GetDriverStandings();
        GridStyle.EnsureRows(dgvSeason, rows.Count);
        for (int i = 0; i < rows.Count; i++)
        {
            StandingRow r = rows[i];
            DataGridViewRow row = dgvSeason.Rows[i];
            row.Cells[0].Value = r.Position;
            row.Cells[1].Value = r.Name;
            row.Cells[2].Value = r.Team;
            row.Cells[3].Value = r.Points;
            row.Cells[4].Value = r.Wins;
            row.Cells[5].Value = r.Podiums;
        }
        dgvSeason.ClearSelection();

        int played = championship.Races.Count;
        if (played == 0)
        {
            lblSeason.Text = "Ще не зіграно жодного етапу. Очки нараховуються після кожного Гран-прі: 25-18-15-12-10-8-6-4-2-1.";
        }
        else
        {
            CompletedRace last = championship.Races[^1];
            var team = championship.GetTeamStandings().FirstOrDefault();
            lblSeason.Text = $"Зіграно етапів: {played} з {_data.Tracks.Count}. Останній — {last.GrandPrix}." +
                             (team != null ? $" Кубок конструкторів: {team.Name}, {UkrainianText.Points(team.Points)}." : "");
        }
        tabSeason.Text = played == 0 ? "Чемпіонат" : $"Чемпіонат ({played}/{_data.Tracks.Count})";
    }

    /// <summary>Картка обраного пілота: телеметрія боліда (шини, двигун, паливо, ERS) і часи кіл.</summary>
    private void RefreshDriverCard()
    {
        telemetryPanel.Race = _phase == WeekendPhase.Race ? _race : null;
        telemetryPanel.Qualifying = _phase == WeekendPhase.Qualifying ? _qualifying : null;
        telemetryPanel.SelectedCar = _selected;
        if (_selected != null)
            telemetryPanel.TeamColor = trackPanel.ColorOf(_selected.Team);
        telemetryPanel.Invalidate();
    }

    private void RefreshStatus()
    {
        if (_phase == WeekendPhase.Qualifying && _qualifying != null)
        {
            TimeSpan left = TimeSpan.FromSeconds(Math.Max(0, _qualifying.TimeRemaining));
            string segment = _qualifying.IsCompleted ? "Кваліфікацію завершено"
                : _qualifying.IsBreak ? $"Перерва перед {_qualifying.Segment + 1}"
                : _qualifying.Segment.ToString();
            tslSession.Text = _qualifying.IsCompleted ? segment : $"{segment} · {(int)left.TotalMinutes:00}:{left.Seconds:00}";
            tsslLap.Text = "Кваліфікація";
            QualifyingCar? pole = _qualifying.Standings.FirstOrDefault();
            tsslLeader.Text = pole?.Timing.BestLap != null ? $"Найкращий час: {pole.Car.Driver.FullName}, {LapTimer.Format(pole.Timing.BestLap)}" : "Найкращий час: —";
            tsslPits.Text = _qualifying.Segment == QualifyingSegment.Q3 || _qualifying.IsCompleted
                ? "Q3: боротьба за поул"
                : $"У {_qualifying.Segment + 1} проходять: {_qualifying.AdvanceCount(_qualifying.Segment)}";
            tsslState.Text = _running ? "Симуляція триває" : "Пауза";
            return;
        }

        if (_race == null) return;
        tslSession.Text = _race.IsFinished ? "Гонка · фініш" : $"Гонка · коло {_race.CurrentLap}/{_race.TotalLaps}";
        tsslLap.Text = _race.IsFinished ? "Фініш" : $"Коло {_race.CurrentLap} / {_race.TotalLaps}";
        tsslLeader.Text = "Лідер: " + _race.Leader.Driver.FullName;
        tsslPits.Text = "Піт-стопів: " + _race.TotalPitStops;

        TimeSpan time = TimeSpan.FromSeconds(_race.ElapsedSeconds);
        string state = _race.IsCompleted ? "Усі в парк-ферме"
            : _race.IsFinished ? "Коло охолодження"
            : _running ? "Симуляція триває" : "Пауза";
        tsslState.Text = $"{state} · час гонки {(int)time.TotalHours}:{time.Minutes:00}:{time.Seconds:00}";
    }

    private void AppendLog(string line)
    {
        lstLog.Items.Insert(0, line);
        while (lstLog.Items.Count > MaxLogLines)
            lstLog.Items.RemoveAt(lstLog.Items.Count - 1);
    }

    private void SelectCar(Car car)
    {
        _selected = car;
        trackPanel.SelectedCar = car;
        RefreshAll();
    }

    /// <summary>Перемикає календар на іншу трасу (обробник списку трас підготує вікенд).</summary>
    private void GoToTrack(Track track)
    {
        if (tscTrack.SelectedItem != track)
            tscTrack.SelectedItem = track;
        else
            PrepareWeekend();
    }

    // ------------------------------------------------------------------
    // Автозбереження чемпіонату
    // ------------------------------------------------------------------

    private void LoadAutosave()
    {
        try
        {
            if (File.Exists(_autosavePath))
                Championship.Instance.Load(_autosavePath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            // Пошкоджений файл не заважає почати новий сезон.
        }
    }

    private void SaveAutosave()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_autosavePath)!);
            Championship.Instance.Save(_autosavePath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            AppendLog("Не вдалося автоматично зберегти чемпіонат: " + ex.Message);
        }
    }

    // ------------------------------------------------------------------
    // Обробники подій елементів керування
    // ------------------------------------------------------------------

    private void StartPause_Click(object? sender, EventArgs e) => SetRunning(!_running);

    private void Restart_Click(object? sender, EventArgs e) => PrepareWeekend();

    /// <summary>Пропустити кваліфікацію: досимулювати Q1–Q3 миттєво і перейти до решітки.</summary>
    private void SkipQualifying_Click(object? sender, EventArgs e)
    {
        if (_phase != WeekendPhase.Qualifying || _qualifying == null) return;
        SetRunning(false);
        _qualifying.RunToEnd();
        HandlePendingEvents();
        tabRight.SelectedTab = tabPositions;
    }

    private void NextRound_Click(object? sender, EventArgs e)
    {
        Track? next = _data.NextTrackAfter(_track);
        if (next == null) return;
        if (_race is { IsStarted: true, IsFinished: false } || _qualifying is { ElapsedSeconds: > 0 } && _phase == WeekendPhase.Qualifying)
        {
            var answer = MessageBox.Show(this, $"Перервати {_track.GrandPrix} і перейти до етапу «{next.GrandPrix}»? Очки за незавершену гонку не нараховуються.",
                "Наступний етап", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (answer != DialogResult.Yes) return;
        }
        GoToTrack(next);
    }

    private void TscSpeed_SelectedIndexChanged(object? sender, EventArgs e)
    {
        int index = tscSpeed.SelectedIndex;
        if (index >= 0 && index < Speeds.Length)
            _timeScale = Speeds[index].Scale;
    }

    private void TscTrack_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_suppressEvents) return;
        if (tscTrack.SelectedItem is Track track && track != _track)
        {
            _track = track;
            PrepareWeekend();
        }
    }

    private void Grid_CellClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (sender is DataGridView grid && e.RowIndex >= 0 && e.RowIndex < grid.Rows.Count && grid.Rows[e.RowIndex].Tag is Car car)
            SelectCar(car);
    }

    private void TabRight_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (tabRight.SelectedTab == tabTelemetry) RefreshTelemetryGrid();
        else if (tabRight.SelectedTab == tabSeason) RefreshSeason();
    }

    private void TrackPanel_CarClicked(object? sender, CarEventArgs e) => SelectCar(e.Car);

    private void MnuSaveChampionship_Click(object? sender, EventArgs e)
    {
        using var dialog = new SaveFileDialog
        {
            Filter = "Файли чемпіонату (*.json)|*.json",
            FileName = "championship-2026.json",
            Title = "Зберегти чемпіонат"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            Championship.Instance.Save(dialog.FileName);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, "Не вдалося зберегти файл.\n\n" + ex.Message, "Помилка",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void MnuLoadChampionship_Click(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "Файли чемпіонату (*.json)|*.json",
            Title = "Відкрити чемпіонат"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            Championship.Instance.Load(dialog.FileName);
            SaveAutosave();
            MessageBox.Show(this, $"Завантажено етапів: {Championship.Instance.Races.Count}.", "Чемпіонат",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            MessageBox.Show(this, "Не вдалося відкрити файл.\n\n" + ex.Message, "Помилка",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void MnuStandings_Click(object? sender, EventArgs e)
    {
        using var form = new ChampionshipForm();
        form.ShowDialog(this);
    }

    private void MnuResetSeason_Click(object? sender, EventArgs e)
    {
        var answer = MessageBox.Show(this, "Видалити результати всіх зіграних етапів?", "Скинути сезон",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (answer != DialogResult.Yes) return;
        Championship.Instance.Reset();
        SaveAutosave();
    }

    private void MnuAbout_Click(object? sender, EventArgs e)
    {
        MessageBox.Show(this,
            "F1 Race Simulator 2026" + Environment.NewLine +
            "Курсовий проєкт з об'єктно-орієнтованого програмування." + Environment.NewLine + Environment.NewLine +
            "C#, .NET 8, Windows Forms." + Environment.NewLine +
            "Схеми трас, номери поворотів і профілі швидкості: офіційні дані хронометражу F1" + Environment.NewLine +
            "(github.com/TracingInsights, FastF1; Apache-2.0).",
            "Про програму", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void MnuExit_Click(object? sender, EventArgs e) => Close();

    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        timerSim.Stop();
        _boldFont?.Dispose();
    }
}
