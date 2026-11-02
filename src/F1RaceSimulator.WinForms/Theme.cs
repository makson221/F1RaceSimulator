namespace F1RaceSimulator.WinForms;

/// <summary>
/// Палітра кольорів інтерфейсу. Об'єкт незмінний: тема перемикається заміною
/// <see cref="ThemeManager.Current"/> на інший готовий екземпляр (<see cref="Light"/> або <see cref="Dark"/>).
/// Кольори секторів траси (червоний, синій, жовтий) і кольори команд від теми не залежать.
/// </summary>
public sealed class Theme
{
    /// <summary>Назва теми для меню.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Темна тема (для заголовка вікна Windows і кнопок).</summary>
    public bool IsDark { get; init; }

    // ---- Вікно і таблиці ----
    /// <summary>Фон вікна і рамок груп.</summary>
    public Color Window { get; init; }
    /// <summary>Фон таблиць, схеми траси, телеметрії.</summary>
    public Color Panel { get; init; }
    /// <summary>Меню, панель інструментів, рядок стану, заголовки таблиць.</summary>
    public Color Chrome { get; init; }
    /// <summary>Лінії сітки й рамки.</summary>
    public Color Line { get; init; }
    public Color Text { get; init; }
    public Color Muted { get; init; }
    public Color Accent { get; init; }
    /// <summary>Рядок обраного пілота.</summary>
    public Color Selection { get; init; }
    /// <summary>Рядок пілота, що виграв позицію.</summary>
    public Color Gained { get; init; }
    /// <summary>Рядок пілота, що втратив позицію.</summary>
    public Color Lost { get; init; }
    /// <summary>Зона вибування в кваліфікації.</summary>
    public Color DropZone { get; init; }

    // ---- Значення в таблицях ----
    public Color Good { get; init; }
    public Color Warning { get; init; }
    public Color Danger { get; init; }
    /// <summary>Найкращий час (як на табло F1).</summary>
    public Color Purple { get; init; }
    public Color SoftTyre { get; init; }
    public Color MediumTyre { get; init; }

    // ---- Шкали телеметрії ----
    public Color GaugeNormal { get; init; }
    public Color GaugeWarning { get; init; }
    public Color GaugeCritical { get; init; }
    public Color GaugeCold { get; init; }
    public Color GaugeBack { get; init; }
    public Color GaugeRpm { get; init; }
    public Color ZoneWindow { get; init; }
    public Color ZoneHot { get; init; }
    public Color ZoneDanger { get; init; }

    // ---- Схема траси ----
    public Color Track { get; init; }
    public Color PitLane { get; init; }
    public Color StartLine { get; init; }
    public Color TurnFill { get; init; }
    public Color TurnBorder { get; init; }
    /// <summary>Підкладка під підписи болідів і обвідка болідів.</summary>
    public Color Halo { get; init; }

    /// <summary>Світла тема (за замовчуванням).</summary>
    public static readonly Theme Light = new()
    {
        Name = "Світла",
        IsDark = false,
        Window = Color.FromArgb(240, 243, 247),
        Panel = Color.White,
        Chrome = Color.FromArgb(237, 241, 246),
        Line = Color.FromArgb(210, 217, 226),
        Text = Color.FromArgb(26, 35, 48),
        Muted = Color.FromArgb(91, 104, 121),
        Accent = Color.FromArgb(10, 98, 191),
        Selection = Color.FromArgb(217, 231, 248),
        Gained = Color.FromArgb(219, 241, 226),
        Lost = Color.FromArgb(251, 226, 226),
        DropZone = Color.FromArgb(253, 240, 240),
        Good = Color.FromArgb(22, 112, 59),
        Warning = Color.FromArgb(154, 88, 0),
        Danger = Color.FromArgb(179, 34, 34),
        Purple = Color.FromArgb(128, 0, 160),
        SoftTyre = Color.FromArgb(196, 38, 38),
        MediumTyre = Color.FromArgb(168, 115, 0),
        GaugeNormal = Color.FromArgb(46, 139, 87),
        GaugeWarning = Color.FromArgb(214, 137, 16),
        GaugeCritical = Color.FromArgb(192, 57, 43),
        GaugeCold = Color.FromArgb(52, 120, 200),
        GaugeBack = Color.FromArgb(232, 236, 241),
        GaugeRpm = Color.FromArgb(120, 82, 200),
        ZoneWindow = Color.FromArgb(198, 232, 206),
        ZoneHot = Color.FromArgb(250, 222, 186),
        ZoneDanger = Color.FromArgb(245, 196, 190),
        Track = Color.FromArgb(52, 60, 72),
        PitLane = Color.FromArgb(192, 57, 43),
        StartLine = Color.Black,
        TurnFill = Color.White,
        TurnBorder = Color.FromArgb(70, 80, 92),
        Halo = Color.White
    };

    /// <summary>Темна тема.</summary>
    public static readonly Theme Dark = new()
    {
        Name = "Темна",
        IsDark = true,
        Window = Color.FromArgb(21, 26, 33),
        Panel = Color.FromArgb(27, 33, 41),
        Chrome = Color.FromArgb(33, 41, 51),
        Line = Color.FromArgb(48, 58, 71),
        Text = Color.FromArgb(227, 232, 238),
        Muted = Color.FromArgb(147, 160, 177),
        Accent = Color.FromArgb(94, 164, 240),
        Selection = Color.FromArgb(31, 56, 88),
        Gained = Color.FromArgb(22, 54, 34),
        Lost = Color.FromArgb(64, 28, 28),
        DropZone = Color.FromArgb(52, 30, 32),
        Good = Color.FromArgb(114, 212, 154),
        Warning = Color.FromArgb(240, 177, 76),
        Danger = Color.FromArgb(255, 141, 141),
        Purple = Color.FromArgb(205, 150, 255),
        SoftTyre = Color.FromArgb(255, 118, 118),
        MediumTyre = Color.FromArgb(240, 194, 74),
        GaugeNormal = Color.FromArgb(88, 196, 130),
        GaugeWarning = Color.FromArgb(240, 177, 76),
        GaugeCritical = Color.FromArgb(255, 120, 110),
        GaugeCold = Color.FromArgb(110, 170, 240),
        GaugeBack = Color.FromArgb(44, 54, 67),
        GaugeRpm = Color.FromArgb(170, 140, 240),
        ZoneWindow = Color.FromArgb(36, 84, 55),
        ZoneHot = Color.FromArgb(96, 70, 32),
        ZoneDanger = Color.FromArgb(100, 42, 42),
        Track = Color.FromArgb(118, 130, 146),
        PitLane = Color.FromArgb(255, 122, 107),
        StartLine = Color.White,
        TurnFill = Color.FromArgb(27, 33, 41),
        TurnBorder = Color.FromArgb(147, 160, 177),
        Halo = Color.FromArgb(27, 33, 41)
    };
}
