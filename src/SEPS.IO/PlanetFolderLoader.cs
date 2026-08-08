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

    public static Planet Scan(string directory)
    {
        if (!Directory.Exists(directory))
            throw new DirectoryNotFoundException(directory);

        var planet = new Planet
        {
            SourceDirectory = directory,
            Name = new DirectoryInfo(directory).Name
        };

        foreach (var face in Enum.GetValues<CubeFace>())
        {
            var pf = planet.Faces[face];
            pf.HeightMapPath = FindMap(directory, FaceAliases[face], false);
            pf.MaterialMapPath = FindMap(directory, FaceAliases[face], true);
        }

        return planet;
    }

    private static string? FindMap(string directory, IEnumerable<string> aliases, bool material)
    {
        foreach (var alias in aliases)
        {
            var suffix = material ? "_mat.png" : ".png";
            var exact = Path.Combine(directory, alias + suffix);
            if (File.Exists(exact)) return exact;
        }

        return Directory.EnumerateFiles(directory, "*.png", SearchOption.TopDirectoryOnly)
            .FirstOrDefault(p => aliases.Any(a =>
                Path.GetFileNameWithoutExtension(p).Equals(material ? a + "_mat" : a, StringComparison.OrdinalIgnoreCase)));
    }
}
