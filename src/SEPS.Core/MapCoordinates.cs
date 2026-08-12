namespace SEPS.Core;

/// <summary>
/// Identifies a pixel on one of the six planetary map faces.
///
/// Coordinates are face-local:
/// X increases from left to right.
/// Y increases from top to bottom.
///
/// No world-space orientation is assumed here.
/// </summary>
public readonly record struct MapPixel(
    CubeFace Face,
    int X,
    int Y);

/// <summary>
/// Normalized coordinates on one planetary face.
///
/// U and V are in the range 0..1.
/// U = 0 is the left edge.
/// V = 0 is the top edge.
///
/// This is deliberately independent of OpenGL UV orientation.
/// </summary>
public readonly record struct MapUv(
    CubeFace Face,
    double U,
    double V);

/// <summary>
/// Converts between pixel and normalized face-local coordinates.
/// </summary>
public static class MapCoordinates
{
    public static MapUv PixelToUv(
        MapPixel pixel,
        int width,
        int height)
    {
        if (width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width));

        if (height <= 0)
            throw new ArgumentOutOfRangeException(nameof(height));

        var u =
            width == 1
                ? 0.0
                : (double)pixel.X / (width - 1);

        var v =
            height == 1
                ? 0.0
                : (double)pixel.Y / (height - 1);

        return new MapUv(
            pixel.Face,
            u,
            v);
    }

    public static MapPixel UvToPixel(
        MapUv uv,
        int width,
        int height)
    {
        if (width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width));

        if (height <= 0)
            throw new ArgumentOutOfRangeException(nameof(height));

        var x =
            width == 1
                ? 0
                : (int)Math.Round(
                    Math.Clamp(uv.U, 0.0, 1.0) *
                    (width - 1));

        var y =
            height == 1
                ? 0
                : (int)Math.Round(
                    Math.Clamp(uv.V, 0.0, 1.0) *
                    (height - 1));

        return new MapPixel(
            uv.Face,
            x,
            y);
    }
}