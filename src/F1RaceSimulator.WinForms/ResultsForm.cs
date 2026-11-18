using F1RaceSimulator.Core.Models;
using F1RaceSimulator.Core.Season;
using F1RaceSimulator.Core.Simulation;

namespace F1RaceSimulator.WinForms;

/// <summary>
/// Вікно після Гран-прі: фінішний протокол з нарахованими очками і таблиця чемпіонату
/// з тим, скільки очок і місць кожен пілот здобув на цьому етапі.
/// Кнопка «Наступний етап» повертає <see cref="DialogResult.OK"/>.
/// </summary>
public partial class ResultsForm : Form
{
    private static Color GoodColor => ThemeManager.Current.Good;
    private static Color BadColor => ThemeManager.Current.Danger;

    /// <summary>Висота рядка: 22 пілоти мають поміститися без прокручування.</summary>
    private const int RowHeight = 26;

    public ResultsForm(Race race, Track? nextTrack)
    {
        ArgumentNullException.ThrowIfNull(race);
        InitializeComponent();
        lblInfo.Tag = "muted";
        ThemeManager.Attach(this);

        Track track = race.Track;
        lblTitle.Text = $"{track.GrandPrix} 2026 — результати";
        string fastest = race.FastestLapCar is { } fl
            ? $"Найшвидше коло: {fl.Driver.FullName}, {LapTimer.Format(race.FastestLap)}. "
            : string.Empty;
        string neutral = race.SafetyCarCount + race.VirtualSafetyCarCount == 0
            ? "без сейфті-кара"
            : $"сейфті-кар: {race.SafetyCarCount}, VSC: {race.VirtualSafetyCarCount}";
        lblInfo.Text = $"Етап {track.Round} з 23 · {track.Circuit}, {UkrainianText.Laps(track.Laps)}. {fastest}" +
                       $"Обгонів: {race.TotalOvertakes} · {neutral} · очки: 25-18-15-12-10-8-6-4-2-1.";

        FillRace(race);
        FillChampionship(track.Round);

        if (nextTrack != null)
        {
            btnNext.Text = $"Наступний етап: {nextTrack.GrandPrix} ▶";
        }
        else
        {
            btnNext.Text = "Сезон завершено";
            btnNext.Enabled = false;
        }
    }

    /// <summary>
    /// Вікно займає майже весь екран, щоб протокол і таблиця чемпіонату
    /// були видні повністю, без прокручування і обрізаних імен.
    /// </summary>
    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        Rectangle area = Screen.FromControl(Owner ?? this).WorkingArea;
        Size = new Size(Math.Min(1600, area.Width - 40), Math.Min(960, area.Height - 30));
        split.SplitterDistance = (int)(split.Width * 0.57);
        if (Owner != null) CenterToParent(); else CenterToScreen();
    }

    private void FillRace(Race race)
    {
        GridStyle.Setup(dgvRace, ("Поз", 6), ("Пілот", 24), ("Команда", 19), ("Старт", 7), ("Статус", 19), ("Найкраще коло", 15), ("Очки", 8));
        dgvRace.RowTemplate.Height = RowHeight;
        foreach (RaceResult r in race.GetResults())
        {
            int index = dgvRace.Rows.Add(
                r.Car.State == CarState.Retired ? "—" : r.Position.ToString(),
                r.Car.Driver.FullName,
                r.Car.Team.Name,
                r.Car.GridPosition,
                r.Status,
                LapTimer.Format(r.Car.Timing.BestLap),
                r.Points > 0 ? "+" + r.Points : "");
            DataGridViewRow row = dgvRace.Rows[index];
            if (r.Points > 0) row.Cells[6].Style.ForeColor = GoodColor;
            if (r.Car == race.FastestLapCar) row.Cells[5].Style.ForeColor = ThemeManager.Current.Purple;
            if (r.Position <= 3 && r.Car.State == CarState.Finished)
                row.DefaultCellStyle.Font = new Font(dgvRace.Font, FontStyle.Bold);
        }
        dgvRace.ClearSelection();
    }

    private void FillChampionship(int round)
    {
        Championship championship = Championship.Instance;
        var before = championship.GetDriverStandings(round - 1).ToDictionary(r => r.Code, r => r.Position);
        var thisRace = championship.Races.FirstOrDefault(r => r.Round == round)?.Results
            .ToDictionary(e => e.DriverCode, e => e.Points) ?? new Dictionary<string, int>();

        grpChampionship.Text = $"Чемпіонат пілотів після {round}-го етапу";
        GridStyle.Setup(dgvChampionship, ("Поз", 8), ("±", 8), ("Пілот", 34), ("Команда", 28), ("Очки", 11), ("Етап", 11));
        dgvChampionship.RowTemplate.Height = RowHeight;
        foreach (StandingRow row in championship.GetDriverStandings())
        {
            string change = "";
            if (before.TryGetValue(row.Code, out int was) && was != row.Position)
                change = was > row.Position ? $"▲{was - row.Position}" : $"▼{row.Position - was}";
            int gained = thisRace.GetValueOrDefault(row.Code);
            int index = dgvChampionship.Rows.Add(row.Position, change, row.Name, row.Team, row.Points, gained > 0 ? "+" + gained : "");
            DataGridViewRow r = dgvChampionship.Rows[index];
            r.Cells[1].Style.ForeColor = change.StartsWith("▲") ? GoodColor : BadColor;
            if (gained > 0) r.Cells[5].Style.ForeColor = GoodColor;
        }
        dgvChampionship.ClearSelection();
    }
}
