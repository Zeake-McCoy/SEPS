namespace SEPS.Editor;

public sealed class MapViewportState
{
    /// <summary>
    /// 1.0 = fit-to-view baseline.
    /// </summary>
    public double Zoom { get; private set; } = 1.0;

    /// <summary>
    /// Pan offset in normalized viewport space.
    /// </summary>
    public double PanX { get; private set; }

    public double PanY { get; private set; }

    public void SetZoom(double zoom)
    {
        Zoom =
            Math.Clamp(
                zoom,
                0.05,
                32.0);
    }

    public void AdjustZoom(double factor)
    {
        SetZoom(
            Zoom * factor);
    }

    public void Pan(
        double deltaX,
        double deltaY)
    {
        PanX += deltaX;
        PanY += deltaY;
    }

    public void Reset()
    {
        Zoom = 1.0;
        PanX = 0.0;
        PanY = 0.0;
    }
}