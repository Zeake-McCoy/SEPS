using System.Xml.Linq;

namespace SEPS.Core;

public sealed class PlanetGeneratorDefinition
{
    public string SubtypeId { get; init; } = string.Empty;

    public string SourceFilePath { get; init; } = string.Empty;

    public bool MaterialMaps { get; init; }
    public bool OreMaps { get; init; }
    public bool BiomeMaps { get; init; }
    public bool OcclusionMaps { get; init; }

    public HillParameters? HillParams { get; init; }

    public SurfaceDetailSettings? SurfaceDetail { get; init; }

    public AtmosphereDefinitionSettings? Atmosphere { get; init; }

    public double? SurfaceGravity { get; init; }

    /// <summary>
    /// Complete XML for this PlanetGeneratorDefinition.
    /// This allows SEPS to retain settings that have not yet
    /// received strongly typed support.
    /// </summary>
    public XElement RawXml { get; init; } = new("PlanetGeneratorDefinition");
}

public sealed class HillParameters
{
    public double Min { get; init; }
    public double Max { get; init; }
}

public sealed class SurfaceDetailSettings
{
    public string? Texture { get; init; }

    public int? Size { get; init; }

    public double? Scale { get; init; }

    public double? SlopeMin { get; init; }

    public double? SlopeMax { get; init; }

    public double? Transition { get; init; }
}

public sealed class AtmosphereDefinitionSettings
{
    public bool? Breathable { get; init; }

    public double? OxygenDensity { get; init; }

    public double? Density { get; init; }

    public double? LimitAltitude { get; init; }

    public double? MaxWindSpeed { get; init; }
}