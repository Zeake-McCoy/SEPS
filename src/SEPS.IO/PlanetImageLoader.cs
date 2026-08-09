using SEPS.Core;
using StbImageSharp;

namespace SEPS.IO;

/// <summary>
/// Loads Space Engineers planet image maps.
///
/// Material maps are currently decoded as RGBA8.
/// Heightmaps are decoded as native 16-bit luminance data.
/// </summary>
public static class PlanetImageLoader
{
    public static HeightMap LoadHeightMap(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException(
                "Heightmap was not found.",
                path);

        var image = Png16Reader.Read(path);

        return new HeightMap(
            image.Width,
            image.Height,
            image.Samples);
    }

    public static MaterialMap LoadMaterialMap(string path)
    {
        using var stream =
            File.OpenRead(path);

        var image =
            ImageResult.FromStream(
                stream,
                ColorComponents.RedGreenBlueAlpha);

        return new MaterialMap(
            image.Width,
            image.Height,
            image.Data);
    }

    public static (int Width, int Height) ReadDimensions(
        string path)
    {
        using var stream =
            File.OpenRead(path);

        var image =
            ImageResult.FromStream(
                stream,
                ColorComponents.Default);

        return (
            image.Width,
            image.Height);
    }
}