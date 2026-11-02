using System.Reflection;

namespace F1RaceSimulator.WinForms;

/// <summary>
/// Однакове оформлення таблиць (DataGridView) у всіх вікнах програми:
/// лише читання, без заголовків рядків, подвійна буферизація; кольори — з поточної теми.
/// </summary>
public static class GridStyle
{
    /// <summary>Налаштовує таблицю і створює стовпці (назва, вага ширини).</summary>
    public static void Setup(DataGridView grid, params (string Header, float Weight)[] columns)
    {
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.AllowUserToResizeRows = false;
        grid.ReadOnly = true;
        grid.RowHeadersVisible = false;
        grid.MultiSelect = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.BorderStyle = BorderStyle.None;
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            Font = new Font("Segoe UI", 8.25F, FontStyle.Bold),
            Alignment = DataGridViewContentAlignment.MiddleLeft,
            WrapMode = DataGridViewTriState.True
        };
        ThemeManager.ApplyGrid(grid);
        EnableDoubleBuffering(grid);

        grid.Columns.Clear();
        foreach (var (header, weight) in columns)
        {
            var column = new DataGridViewTextBoxColumn
            {
                HeaderText = header,
                FillWeight = weight,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                MinimumWidth = 4
            };
            if (header.Length == 0)
            {
                column.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                column.Width = 6;
            }
            grid.Columns.Add(column);
        }
    }

    /// <summary>Гарантує, що в таблиці рівно <paramref name="count"/> рядків.</summary>
    public static void EnsureRows(DataGridView grid, int count)
    {
        if (grid.Rows.Count == count) return;
        grid.Rows.Clear();
        if (count > 0) grid.Rows.Add(count);
    }

    /// <summary>Вмикає подвійну буферизацію (властивість захищена, тому через рефлексію).</summary>
    public static void EnableDoubleBuffering(Control control) =>
        typeof(Control)
            .GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic)?
            .SetValue(control, true, null);
}
