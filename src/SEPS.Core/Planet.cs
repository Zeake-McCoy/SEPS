namespace SEPS.Core;

public enum CubeFace
{
    Front,
    Back,
    Left,
    Right,
    Top,
    Bottom
}

public enum MapKind
{
    Height,
    Material
}

public sealed class PlanetFace
{
    public CubeFace Face { get; init; }

    public string? HeightMapPath { get; set; }

    public string? MaterialMapPath { get; set; }
}

public sealed class HeightMap
{
    public int Width { get; }
    public int Height { get; }
    public ushort[] Samples { get; }

    public HeightMap(int width, int height, ushort[] samples)
    {
        Width = width;
        Height = height;
        Samples = samples;
    }

    public ushort Sample(int x, int y) => Samples[y * Width + x];
}


public enum MaterialChannel
{
    Red,
    Green,
    Blue
}

public sealed class MaterialMap
{
    public int Width { get; }
    public int Height { get; }
    public byte[] Rgba { get; }

    public MaterialMap(
        int width,
        int height,
        byte[] rgba)
    {
        if (width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width));

        if (height <= 0)
            throw new ArgumentOutOfRangeException(nameof(height));

        ArgumentNullException.ThrowIfNull(rgba);

        if (rgba.Length != checked(width * height * 4))
        {
            throw new ArgumentException(
                "RGBA data length does not match image dimensions.",
                nameof(rgba));
        }

        Width = width;
        Height = height;
        Rgba = rgba;
    }

    public byte[] ExtractChannel(
        MaterialChannel channel,
        bool rawIntensity = false)
    {
        int channelOffset = channel switch
        {
            MaterialChannel.Red => 0,
            MaterialChannel.Green => 1,
            MaterialChannel.Blue => 2,
            _ => throw new ArgumentOutOfRangeException(
                nameof(channel))
        };

        var output = new byte[Rgba.Length];

        for (int i = 0; i < Rgba.Length; i += 4)
        {
            byte value = Rgba[i + channelOffset];

            if (rawIntensity)
            {
                // Display the selected channel as grayscale.
                output[i] = value;
                output[i + 1] = value;
                output[i + 2] = value;
            }
            else
            {
                // Display the selected channel in its own colour.
                output[i] = channel == MaterialChannel.Red
                    ? value : (byte)0;

                output[i + 1] = channel == MaterialChannel.Green
                    ? value : (byte)0;

                output[i + 2] = channel == MaterialChannel.Blue
                    ? value : (byte)0;
            }

            // Keep the output fully opaque.
            output[i + 3] = 255;
        }

        return output;
    }
}


public sealed class WaterSettings
{
    public bool Enabled { get; set; }
    public double Modifier { get; set; } = 1.0;
}

public sealed class Planet
{
    public string SourceDirectory { get; init; } = string.Empty;

    public string Name { get; set; } = "Untitled Planet";

    public string DataSubtypeId { get; set; } = string.Empty;

    public PlanetGeneratorDefinition? GeneratorDefinition { get; set; }

    public bool HasGeneratorDefinition =>
        GeneratorDefinition != null;

    public double RadiusMeters { get; set; } = 60_000;

    public WaterSettings Water { get; } = new();

    public Dictionary<CubeFace, PlanetFace> Faces { get; } =
        Enum.GetValues<CubeFace>()
            .ToDictionary(
                f => f,
                f => new PlanetFace { Face = f });
}
