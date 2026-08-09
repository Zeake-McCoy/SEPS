using System.Globalization;
using System.Xml.Linq;
using SEPS.Core;

namespace SEPS.IO;

public static class PlanetDefinitionLoader
{
    public static IReadOnlyList<PlanetGeneratorDefinition> LoadFromFile(
        string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException(
                "Planet definition file was not found.",
                filePath);

        var document = XDocument.Load(filePath);

        return document
            .Descendants("PlanetGeneratorDefinition")
            .Select(element => ParseDefinition(element, filePath))
            .Where(definition => !string.IsNullOrWhiteSpace(definition.SubtypeId))
            .ToList();
    }

    public static IReadOnlyList<PlanetGeneratorDefinition> Discover(
        string rootDirectory)
    {
        if (!Directory.Exists(rootDirectory))
            throw new DirectoryNotFoundException(rootDirectory);

        var definitions = new List<PlanetGeneratorDefinition>();

        foreach (var file in Directory.EnumerateFiles(
                     rootDirectory,
                     "*.sbc",
                     SearchOption.AllDirectories))
        {
            try
            {
                definitions.AddRange(LoadFromFile(file));
            }
            catch (Exception)
            {
                // A mod may contain SBC/XML files that are not valid
                // standalone XML documents or are otherwise unrelated.
                //
                // Discovery should not fail the entire planet import
                // because of one unrelated definition file.
            }
        }

        return definitions;
    }

    private static PlanetGeneratorDefinition ParseDefinition(
        XElement element,
        string sourceFilePath)
    {
        var id = element.Element("Id");

        var subtypeId =
            id?.Element("SubtypeId")?.Value.Trim()
            ?? string.Empty;

        var planetMaps = element.Element("PlanetMaps");

        var hillElement = element.Element("HillParams");

        HillParameters? hillParams = null;

        if (hillElement != null &&
            TryDouble(hillElement.Attribute("Min")?.Value, out var hillMin) &&
            TryDouble(hillElement.Attribute("Max")?.Value, out var hillMax))
        {
            hillParams = new HillParameters
            {
                Min = hillMin,
                Max = hillMax
            };
        }

        var surfaceElement = element.Element("SurfaceDetail");

        SurfaceDetailSettings? surfaceDetail = null;

        if (surfaceElement != null)
        {
            surfaceDetail = new SurfaceDetailSettings
            {
                Texture = surfaceElement.Element("Texture")?.Value.Trim(),
                Size = TryInt(
                    surfaceElement.Element("Size")?.Value,
                    out var size)
                    ? size
                    : null,
                Scale = TryDouble(
                    surfaceElement.Element("Scale")?.Value,
                    out var scale)
                    ? scale
                    : null,
                SlopeMin = TryDouble(
                    surfaceElement.Element("Slope")?.Attribute("Min")?.Value,
                    out var slopeMin)
                    ? slopeMin
                    : null,
                SlopeMax = TryDouble(
                    surfaceElement.Element("Slope")?.Attribute("Max")?.Value,
                    out var slopeMax)
                    ? slopeMax
                    : null,
                Transition = TryDouble(
                    surfaceElement.Element("Transition")?.Value,
                    out var transition)
                    ? transition
                    : null
            };
        }

        var atmosphereElement = element.Element("Atmosphere");

        AtmosphereDefinitionSettings? atmosphere = null;

        if (atmosphereElement != null)
        {
            atmosphere = new AtmosphereDefinitionSettings
            {
                Breathable = TryBool(
                    atmosphereElement.Element("Breathable")?.Value,
                    out var breathable)
                    ? breathable
                    : null,

                OxygenDensity = TryDouble(
                    atmosphereElement.Element("OxygenDensity")?.Value,
                    out var oxygenDensity)
                    ? oxygenDensity
                    : null,

                Density = TryDouble(
                    atmosphereElement.Element("Density")?.Value,
                    out var density)
                    ? density
                    : null,

                LimitAltitude = TryDouble(
                    atmosphereElement.Element("LimitAltitude")?.Value,
                    out var limitAltitude)
                    ? limitAltitude
                    : null,

                MaxWindSpeed = TryDouble(
                    atmosphereElement.Element("MaxWindSpeed")?.Value,
                    out var maxWindSpeed)
                    ? maxWindSpeed
                    : null
            };
        }

        return new PlanetGeneratorDefinition
        {
            SubtypeId = subtypeId,
            SourceFilePath = sourceFilePath,

            MaterialMaps = ReadBoolAttribute(
                planetMaps,
                "Material"),

            OreMaps = ReadBoolAttribute(
                planetMaps,
                "Ores"),

            BiomeMaps = ReadBoolAttribute(
                planetMaps,
                "Biome"),

            OcclusionMaps = ReadBoolAttribute(
                planetMaps,
                "Occlusion"),

            HillParams = hillParams,

            SurfaceDetail = surfaceDetail,

            Atmosphere = atmosphere,

            SurfaceGravity =
                TryDouble(
                    element.Element("SurfaceGravity")?.Value,
                    out var gravity)
                    ? gravity
                    : null,

            RawXml = new XElement(element)
        };
    }

    private static bool ReadBoolAttribute(
        XElement? element,
        string attributeName)
    {
        return element?.Attribute(attributeName)?.Value
            .Equals("true", StringComparison.OrdinalIgnoreCase)
            == true;
    }

    private static bool TryDouble(
        string? value,
        out double result)
    {
        return double.TryParse(
            value,
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out result);
    }

    private static bool TryInt(
        string? value,
        out int result)
    {
        return int.TryParse(
            value,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out result);
    }

    private static bool TryBool(
        string? value,
        out bool result)
    {
        return bool.TryParse(value, out result);
    }
}