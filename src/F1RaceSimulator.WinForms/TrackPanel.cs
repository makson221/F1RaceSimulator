using System.ComponentModel;
using F1RaceSimulator.Core.Models;
using F1RaceSimulator.Core.Simulation;

namespace F1RaceSimulator.WinForms;

/// <summary>
/// Панель зі схемою траси. Використовує подвійну буферизацію,
/// щоб анімація болідів не мерехтіла.
/// </summary>
public sealed class TrackPanel : Panel
{
    private readonly TrackRenderer _renderer = new();

    public TrackPanel()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        BackColor = Color.White;
    }

    /// <summary>Траса, яку потрібно намалювати.</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Track? Track { get; set; }

    /// <summary>Поточна сесія — кваліфікація або гонка (для малювання болідів).</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ITrackSession? Session { get; set; }

    /// <summary>Обраний пілот — його болід підсвічується кільцем.</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Car? SelectedCar { get; set; }

    /// <summary>Виникає, коли користувач клацнув мишею по боліду.</summary>
    public event EventHandler<CarEventArgs>? CarClicked;

    /// <summary>Колір команди (той самий, що й на схемі).</summary>
    public Color ColorOf(Team team) => _renderer.ColorOf(team);

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (Track == null)
        {
            TextRenderer.DrawText(e.Graphics, "Оберіть етап у списку «Етап».", Font, ClientRectangle,
                Color.Gray, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            return;
        }
        _renderer.Draw(e.Graphics, ClientRectangle, Track, Session, SelectedCar);
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        Car? car = _renderer.HitTest(e.Location);
        if (car != null)
            CarClicked?.Invoke(this, new CarEventArgs(car));
    }
}

/// <summary>Аргументи події з посиланням на болід.</summary>
public sealed class CarEventArgs : EventArgs
{
    public CarEventArgs(Car car) => Car = car;

    public Car Car { get; }
}
