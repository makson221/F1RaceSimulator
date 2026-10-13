using System.Reflection;
using F1RaceSimulator.Core.Models;
using F1RaceSimulator.Core.Season;
using F1RaceSimulator.Core.Simulation;

namespace F1RaceSimulator.WinForms;

/// <summary>
/// Головне вікно програми. Показує трасу, таблицю позицій, журнал подій
/// і картку обраного пілота. Реалізує <see cref="IRaceObserver"/>,
/// щоб дізнаватися про фініш гонки (патерн Observer).
/// </summary>
public partial class MainForm : Form, IRaceObserver
{
    /// <summary>Варіанти швидкості: у скільки разів симуляція швидша за реальний час.</summary>
    private static readonly (string Text, double Scale)[] Speeds =
    {
        ("×10", 10), ("×25", 25), ("×50", 50), ("×100", 100)
    };

    private const int MaxLogLines = 300;
    private const double FlashSeconds = 2.5;

    private static readonly Color SelectedRowColor = Color.FromArgb(217, 231, 248);
    private static readonly Color GainedRowColor = Color.FromArgb(219, 241, 226);
    private static readonly Color LostRowColor = Color.FromArgb(251, 226, 226);
    private static readonly Color SoftColor = Color.FromArgb(196, 38, 38);
    private static readonly Color MediumColor = Color.FromArgb(168, 115, 0);
    private static readonly Color WarningColor = Color.FromArgb(154, 88, 0);
    private static readonly Color DangerColor = Color.FromArgb(179, 34, 34);

    private readonly SeasonData _data;
    private readonly IRandomSource _random = new SystemRandomSource();
    private readonly EventLogObserver _log = new();
    private readonly Dictionary<Car, int> _lastPositions = new();
    private readonly Dictionary<Car, int> _positionDelta = new();
    private readonly Dictionary<Car, DateTime> _flashUntil = new();

    private Race? _race;
    private Font? _boldFont;
    private Track _track;
    private Car? _selected;
    private double _timeScale = 25;
    private bool _running;
    private bool _suppressEvents;
    private bool _finishPending;
    private DateTime _lastTick;
    private int _tickCounter;

    public MainForm(SeasonData data)
    {
        _data = data ?? throw new ArgumentNullException(nameof(data));
        InitializeComponent();
        EnableDoubleBuffering(dgvStandings);

        _log.EventAdded += (_, e) => AppendLog(e.ToString());

        _suppressEvents = true;
        foreach (var speed in Speeds)
            tscSpeed.Items.Add(speed.Text);
        tscSpeed.SelectedIndex = 1;

        foreach (Track track in _data.Tracks)
            tscTrack.Items.Add(track);

        // За замовчуванням — найближчий етап календаря.
        _track = _data.NextTrack(DateOnly.FromDateTime(DateTime.Today));
        tscTrack.SelectedItem = _track;
        _suppressEvents = false;

        PrepareRace();
    }

    // ------------------------------------------------------------------
    // Підготовка гонки
    // ------------------------------------------------------------------

    /// <summary>Проводить кваліфікацію і ставить боліди на стартову решітку.</summary>
    private void PrepareRace()
    {
        SetRunning(false);
        string? selectedCode = _selected?.Driver.Code;

        if (_race != null)
        {
            _race.Unsubscribe(_log);
            _race.Unsubscribe(this);
        }

        _log.Clear();
        lstLog.Items.Clear();
        _lastPositions.Clear();
        _positionDelta.Clear();
        _flashUntil.Clear();
        _finishPending = false;

        // Кваліфікація визначає стартову решітку.
        var cars = CarFactory.CreateCars(_data);
        var qualifying = new Qualifying(_random).Run(cars, _track);
        var grid = CarFactory.CreateGrid(_data, qualifying);

        AppendLog("Кваліфікація: " + string.Join(", ", qualifying.Take(3).Select(q =>
            $"P{q.Position} {q.Car.Driver.LastName} {q.LapTimeText}")));

        _race = new Race(_track, grid, _random);
        _race.Subscribe(_log);
        _race.Subscribe(this);
        _race.Start();

        _selected = grid.FirstOrDefault(c => c.Driver.Code == selectedCode) ?? grid[0];
        foreach (Car car in grid)
            _lastPositions[car] = car.Position;

        trackPanel.Track = _track;
        trackPanel.Race = _race;
        trackPanel.SelectedCar = _selected;

        Text = $"F1 Race Simulator — {_track.GrandPrix} 2026, {_track.City}";
        grpTrack.Text = $"{_track.Circuit} — {_track.LengthKm:0.000} км, {UkrainianText.Laps(_track.Laps)}";
        tslWeather.Text = "Погода: " + _track.Weather;

        RefreshAll();
    }

    // ------------------------------------------------------------------
    // Таймер симуляції
    // ------------------------------------------------------------------

    private void TimerSim_Tick(object? sender, EventArgs e)
    {
        if (_race == null || !_running) return;

        DateTime now = DateTime.UtcNow;
        double realSeconds = Math.Min(0.1, (now - _lastTick).TotalSeconds);  // захист від «стрибка» після паузи
        _lastTick = now;

        _race.Step(realSeconds * _timeScale);
        trackPanel.Invalidate();

        if (++_tickCounter % 4 == 0 || _race.IsFinished)
            RefreshAll();

        if (_finishPending)
        {
            _finishPending = false;
            OnRaceFinished();
        }
    }

    private void SetRunning(bool running)
    {
        _running = running && _race is { IsFinished: false };
        if (_running)
        {
            _lastTick = DateTime.UtcNow;
            timerSim.Start();
        }
        else
        {
            timerSim.Stop();
        }
        UpdateStartButtons();
        RefreshStatus();
    }

    private void UpdateStartButtons()
    {
        string text;
        if (_race is { IsFinished: true }) text = "Фініш";
        else if (_running) text = "Пауза";
        else text = _race is { ElapsedSeconds: > 0 } ? "Продовжити" : "Старт";

        tsbStartPause.Text = (_running ? "❚❚ " : "▶ ") + text;
        mnuStartPause.Text = text;
        tsbStartPause.Enabled = mnuStartPause.Enabled = _race is { IsFinished: false };
    }

    // ------------------------------------------------------------------
    // IRaceObserver
    // ------------------------------------------------------------------

    /// <summary>Отримує події гонки. Фініш обробляємо після кроку симуляції.</summary>
    public void OnRaceEvent(Race race, RaceEvent raceEvent)
    {
        if (raceEvent.Type == RaceEventType.RaceFinished)
            _finishPending = true;
    }

    private void OnRaceFinished()
    {
        if (_race == null) return;
        SetRunning(false);
        RefreshAll();

        var results = _race.GetResults();
        Championship.Instance.AddRaceResult(_track, results);

        string podium = string.Join(Environment.NewLine, results.Take(3).Select(r =>
            $"{r.Position}. {r.Car.Driver.FullName} ({r.Car.Team.Name}) — {UkrainianText.Points(r.Points)}"));
        MessageBox.Show(this,
            $"{_track.GrandPrix} завершено!{Environment.NewLine}{Environment.NewLine}{podium}" +
            $"{Environment.NewLine}{Environment.NewLine}Результат додано до таблиці чемпіонату (F9).",
            "Фініш", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    // ------------------------------------------------------------------
    // Оновлення інтерфейсу
    // ------------------------------------------------------------------

    private void RefreshAll()
    {
        RefreshStandings();
        RefreshDriverCard();
        RefreshStatus();
        UpdateStartButtons();
        trackPanel.Invalidate();
    }

    /// <summary>Оновлює таблицю позицій і підсвічує тих, хто виграв або втратив місця.</summary>
    private void RefreshStandings()
    {
        if (_race == null) return;
        IReadOnlyList<Car> standings = _race.Standings;
        DateTime now = DateTime.UtcNow;

        if (dgvStandings.Rows.Count != standings.Count)
        {
            dgvStandings.Rows.Clear();
            dgvStandings.Rows.Add(standings.Count);
        }

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

            row.Cells[colColor.Index].Style.BackColor = trackPanel.ColorOf(car.Team);
            row.Cells[colColor.Index].Style.SelectionBackColor = trackPanel.ColorOf(car.Team);
            row.Cells[colPos.Index].Value = retired ? "—" : car.Position.ToString();
            row.Cells[colDriver.Index].Value = car.Driver.LastName + arrow;
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

            Color back = car == _selected ? SelectedRowColor
                : change > 0 ? GainedRowColor
                : change < 0 ? LostRowColor
                : Color.White;
            row.DefaultCellStyle.BackColor = back;
            row.DefaultCellStyle.SelectionBackColor = back;
            row.DefaultCellStyle.SelectionForeColor = retired ? Color.Gray : Color.Black;
            row.DefaultCellStyle.ForeColor = retired ? Color.Gray : Color.Black;
            _boldFont ??= new Font(dgvStandings.Font, FontStyle.Bold);
            row.DefaultCellStyle.Font = car == _selected ? _boldFont : null;
        }

        dgvStandings.ClearSelection();
    }

    /// <summary>Картка обраного пілота: телеметрія боліда (шини, двигун, паливо, ERS).</summary>
    private void RefreshDriverCard()
    {
        telemetryPanel.Race = _race;
        telemetryPanel.SelectedCar = _selected;
        if (_selected != null)
            telemetryPanel.TeamColor = trackPanel.ColorOf(_selected.Team);
        telemetryPanel.Invalidate();
    }

    private void RefreshStatus()
    {
        if (_race == null) return;
        tsslLap.Text = _race.IsFinished ? "Фініш" : $"Коло {_race.CurrentLap} / {_race.TotalLaps}";
        tsslLeader.Text = "Лідер: " + _race.Leader.Driver.FullName;
        tsslPits.Text = "Піт-стопів: " + _race.TotalPitStops;

        TimeSpan time = TimeSpan.FromSeconds(_race.ElapsedSeconds);
        string state = _race.IsFinished ? "Гонку завершено" : _running ? "Симуляція триває" : "Пауза";
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

    /// <summary>Вмикає подвійну буферизацію для DataGridView (властивість захищена, тому через рефлексію).</summary>
    private static void EnableDoubleBuffering(Control control)
    {
        typeof(Control)
            .GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic)?
            .SetValue(control, true, null);
    }

    // ------------------------------------------------------------------
    // Обробники подій елементів керування
    // ------------------------------------------------------------------

    private void StartPause_Click(object? sender, EventArgs e) => SetRunning(!_running);

    private void Restart_Click(object? sender, EventArgs e) => PrepareRace();

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
            PrepareRace();
        }
    }

    private void DgvStandings_CellClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex >= 0 && dgvStandings.Rows[e.RowIndex].Tag is Car car)
            SelectCar(car);
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
        if (answer == DialogResult.Yes)
            Championship.Instance.Reset();
    }

    private void MnuAbout_Click(object? sender, EventArgs e)
    {
        MessageBox.Show(this,
            "F1 Race Simulator 2026" + Environment.NewLine +
            "Курсовий проєкт з об'єктно-орієнтованого програмування." + Environment.NewLine + Environment.NewLine +
            "C#, .NET 8, Windows Forms." + Environment.NewLine +
            "Схеми трас: набір даних f1-circuits (github.com/bacinger/f1-circuits).",
            "Про програму", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void MnuExit_Click(object? sender, EventArgs e) => Close();

    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        timerSim.Stop();
        _boldFont?.Dispose();
    }
}
