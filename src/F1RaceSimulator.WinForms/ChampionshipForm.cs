using F1RaceSimulator.Core.Season;

namespace F1RaceSimulator.WinForms;

/// <summary>
/// Вікно з таблицями чемпіонату: особистий залік пілотів і Кубок конструкторів.
/// Дані бере з єдиного об'єкта <see cref="Championship.Instance"/> (Singleton).
/// </summary>
public partial class ChampionshipForm : Form
{
    public ChampionshipForm()
    {
        InitializeComponent();
        ThemeManager.Attach(this);
    }

    private void ChampionshipForm_Load(object? sender, EventArgs e)
    {
        Championship championship = Championship.Instance;

        GridStyle.Setup(dgvDrivers, ("Поз", 8), ("Пілот", 30), ("Команда", 26), ("Очки", 12), ("Перемоги", 12), ("Подіуми", 12));
        foreach (StandingRow row in championship.GetDriverStandings())
            dgvDrivers.Rows.Add(row.Position, row.Name, row.Team, row.Points, row.Wins, row.Podiums);

        GridStyle.Setup(dgvTeams, ("Поз", 8), ("Команда", 44), ("Очки", 16), ("Перемоги", 16), ("Подіуми", 16));
        foreach (StandingRow row in championship.GetTeamStandings())
            dgvTeams.Rows.Add(row.Position, row.Name, row.Points, row.Wins, row.Podiums);

        lblRaces.Text = championship.Races.Count == 0
            ? "Ще не зіграно жодного етапу. Проведіть гонку до фінішу — результат з'явиться тут."
            : $"Зіграно етапів: {championship.Races.Count} з 23. Останній: {championship.Races[^1].GrandPrix}.";
    }
}
