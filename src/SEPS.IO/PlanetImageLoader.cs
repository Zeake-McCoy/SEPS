using SEPS.Core;
using StbImageSharp;

namespace SEPS.IO;

/// <summary>
/// First-pass PNG loader. Material maps are loaded as RGBA8.
/// Heightmap decoding is intentionally kept behind this boundary because SE heightmaps
/// need their original 16-bit precision preserved.
/// </summary>
public static class PlanetImageLoader
{
    public static MaterialMap LoadMaterialMap(string path)
    {
        using var stream = File.OpenRead(path);
        var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
        return new MaterialMap(image.Width, image.Height, image.Data);
    }

    public static (int Width, int Height) ReadDimensions(string path)
    {
        using var stream = File.OpenRead(path);
        var image = ImageResult.FromStream(stream, ColorComponents.Default);
        return (image.Width, image.Height);
    }
}
