namespace F1RaceSimulator.Core.Simulation;

/// <summary>Допоміжні методи для українських текстів інтерфейсу.</summary>
public static class UkrainianText
{
    /// <summary>
    /// Правильна форма слова «коло» після числа: 1 коло, 2 кола, 5 кіл, 21 коло, 53 кола.
    /// </summary>
    public static string LapsWord(int n)
    {
        int lastTwo = Math.Abs(n) % 100;
        int last = lastTwo % 10;
        if (last == 1 && lastTwo != 11) return "коло";
        if (last is >= 2 and <= 4 && (lastTwo < 12 || lastTwo > 14)) return "кола";
        return "кіл";
    }

    /// <summary>Число разом зі словом «коло», наприклад «53 кола».</summary>
    public static string Laps(int n) => $"{n} {LapsWord(n)}";

    /// <summary>Правильна форма слова «очко»: 1 очко, 2 очки, 25 очок.</summary>
    public static string PointsWord(int n)
    {
        int lastTwo = Math.Abs(n) % 100;
        int last = lastTwo % 10;
        if (last == 1 && lastTwo != 11) return "очко";
        if (last is >= 2 and <= 4 && (lastTwo < 12 || lastTwo > 14)) return "очки";
        return "очок";
    }

    /// <summary>Число разом зі словом «очко», наприклад «18 очок».</summary>
    public static string Points(int n) => $"{n} {PointsWord(n)}";

    /// <summary>Правильна форма слова «хвилина»: 1 хвилина, 2 хвилини, 18 хвилин.</summary>
    public static string MinutesWord(int n) => Form(n, "хвилина", "хвилини", "хвилин");

    /// <summary>Число разом зі словом «хвилина», наприклад «18 хвилин».</summary>
    public static string Minutes(int n) => $"{n} {MinutesWord(n)}";

    /// <summary>Вибір форми слова за числом: одна (1, 21), кілька (2–4, 22–24), багато (5–20).</summary>
    public static string Form(int n, string one, string few, string many)
    {
        int lastTwo = Math.Abs(n) % 100;
        int last = lastTwo % 10;
        if (last == 1 && lastTwo != 11) return one;
        if (last is >= 2 and <= 4 && (lastTwo < 12 || lastTwo > 14)) return few;
        return many;
    }
}
