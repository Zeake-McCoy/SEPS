using SEPS.Core;

namespace SEPS.IO;

public static class PlanetFolderLoader
{
    private static readonly Dictionary<CubeFace, string[]> FaceAliases = new()
    {
        [CubeFace.Front] = ["front", "forward"],
        [CubeFace.Back] = ["back", "rear"],
        [CubeFace.Left] = ["left"],
        [CubeFace.Right] = ["right"],
        [CubeFace.Top] = ["up", "top"],
        [CubeFace.Bottom] = ["down", "bottom"]
    };

    /// <summary>
    /// Scans a directory that is already known to contain a planet.
    /// </summary>
    public static Planet Scan(
        string directory,
        IReadOnlyList<PlanetGeneratorDefinition>? definitions = null)
    {
        if (!Directory.Exists(directory))
            throw new DirectoryNotFoundException(directory);

        var directoryInfo = new DirectoryInfo(directory);

        var planet = new Planet
        {
            SourceDirectory = directory,
            Name = directoryInfo.Name,
            DataSubtypeId = directoryInfo.Name
        };

        foreach (var face in Enum.GetValues<CubeFace>())
        {
            var pf = planet.Faces[face];

            pf.HeightMapPath = FindMap(
                directory,
                FaceAliases[face],
                false);

            pf.MaterialMapPath = FindMap(
                directory,
                FaceAliases[face],
                true);
        }

        // Associate the planet's map dataset with its
        // PlanetGeneratorDefinition using the directory name
        // as the candidate SubtypeId.
        if (definitions != null)
        {
            planet.GeneratorDefinition =
                definitions.FirstOrDefault(
                    definition =>
                        definition.SubtypeId.Equals(
                            planet.DataSubtypeId,
                            StringComparison.OrdinalIgnoreCase));
        }

        return planet;
    }

    /// <summary>
    /// Recursively searches a Space Engineers mod/root directory
    /// for directories containing a complete set of six planet
    /// heightmaps.
    /// </summary>
    public static IReadOnlyList<Planet> Discover(
        string rootDirectory)
    {
        if (!Directory.Exists(rootDirectory))
            throw new DirectoryNotFoundException(rootDirectory);

        // Discover all PlanetGeneratorDefinitions once.
        var definitions =
            PlanetDefinitionLoader.Discover(rootDirectory);

        var planets = new List<Planet>();

        // Check the selected directory itself first.
        if (LooksLikePlanetDirectory(rootDirectory))
        {
            planets.Add(
                Scan(rootDirectory, definitions));
        }

        foreach (var directory in Directory.EnumerateDirectories(
                     rootDirectory,
                     "*",
                     SearchOption.AllDirectories))
        {
            if (LooksLikePlanetDirectory(directory))
            {
                planets.Add(
                    Scan(directory, definitions));
            }
        }

        return planets
            .OrderBy(
                p => p.Name,
                StringComparer.OrdinalIgnoreCase)
            .ThenBy(
                p => p.SourceDirectory,
                StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Determines whether a directory contains the six required
    /// Space Engineers planet heightmap faces.
    /// </summary>
    private static bool LooksLikePlanetDirectory(string directory)
    {
        foreach (var face in Enum.GetValues<CubeFace>())
        {
            var found = FindMap(
                directory,
                FaceAliases[face],
                false);

            if (found == null)
                return false;
        }

        return true;
    }

    private static string? FindMap(
        string directory,
        IEnumerable<string> aliases,
        bool material)
    {
        foreach (var alias in aliases)
        {
            var suffix =
                material
                    ? "_mat.png"
                    : ".png";

            var exact =
                Path.Combine(
                    directory,
                    alias + suffix);

            if (File.Exists(exact))
                return exact;
        }

        return Directory.EnumerateFiles(
                directory,
                "*.png",
                SearchOption.TopDirectoryOnly)
            .FirstOrDefault(
                p =>
                    aliases.Any(
                        a =>
                            Path.GetFileNameWithoutExtension(p)
                                .Equals(
                                    material
                                        ? a + "_mat"
                                        : a,
                                    StringComparison.OrdinalIgnoreCase)));
    }
}