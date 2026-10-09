namespace F1RaceSimulator.Core.Geometry;

/// <summary>
/// Точка (або вектор) на площині схеми траси.
/// Незмінна структура: після створення координати не змінюються.
/// </summary>
public readonly record struct Vec2(double X, double Y)
{
    /// <summary>Відстань між двома точками.</summary>
    public static double Distance(Vec2 a, Vec2 b)
    {
        double dx = b.X - a.X;
        double dy = b.Y - a.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>Лінійна інтерполяція: t = 0 дає точку a, t = 1 дає точку b.</summary>
    public static Vec2 Lerp(Vec2 a, Vec2 b, double t) =>
        new(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);

    /// <summary>Довжина вектора.</summary>
    public double Length => Math.Sqrt(X * X + Y * Y);

    /// <summary>Вектор тієї ж спрямованості довжиною 1 (або нульовий вектор).</summary>
    public Vec2 Normalized()
    {
        double len = Length;
        return len < 1e-12 ? new Vec2(0, 0) : new Vec2(X / len, Y / len);
    }

    public static Vec2 operator +(Vec2 a, Vec2 b) => new(a.X + b.X, a.Y + b.Y);
    public static Vec2 operator -(Vec2 a, Vec2 b) => new(a.X - b.X, a.Y - b.Y);
    public static Vec2 operator *(Vec2 a, double k) => new(a.X * k, a.Y * k);
}
