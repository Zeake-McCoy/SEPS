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
    public HeightMap? Height { get; set; }
    public MaterialMap? Material { get; set; }
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

public sealed class MaterialMap
{
    public int Width { get; }
    public int Height { get; }
    public byte[] Rgba { get; }

    public MaterialMap(int width, int height, byte[] rgba)
    {
        Width = width;
        Height = height;
        Rgba = rgba;
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
