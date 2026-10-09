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
}
