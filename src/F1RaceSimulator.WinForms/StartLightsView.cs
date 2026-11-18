namespace F1RaceSimulator.WinForms;

/// <summary>
/// Що показати на табло стартових вогнів над схемою траси.
/// </summary>
/// <param name="LitCount">Скільки з п'яти червоних вогнів горить.</param>
/// <param name="Caption">Підпис під вогнями.</param>
/// <param name="Opacity">Прозорість від 0 до 1 (табло плавно зникає після старту).</param>
public sealed record StartLightsView(int LitCount, string Caption, float Opacity = 1f);
