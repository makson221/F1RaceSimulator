using F1RaceSimulator.Core.Season;

namespace F1RaceSimulator.WinForms;

/// <summary>Точка входу програми.</summary>
internal static class Program
{
    [STAThread]
    private static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        SeasonData data;
        try
        {
            // Файли JSON лежать у папці Data поруч із F1RaceSimulator.exe.
            string dataDirectory = Path.Combine(AppContext.BaseDirectory, "Data");
            data = DataLoader.LoadSeason(dataDirectory);
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException)
        {
            MessageBox.Show(
                "Не вдалося завантажити дані сезону.\n\n" + ex.Message,
                "F1 Race Simulator",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        Application.Run(new MainForm(data));
    }
}
