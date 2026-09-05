using SEPS.Core;

namespace SEPS.Editor;

public sealed class PlanetEditingSession
{
    private readonly Dictionary<
        (CubeFace Face, MapKind Kind),
        MapAssetState> _assets =
        new();

    public Planet Planet { get; }

    public bool HasUnsavedChanges =>
        _assets.Values.Any(
            asset => asset.IsDirty);

    public PlanetEditingSession(
        Planet planet)
    {
        Planet = planet;

        foreach (var face in
                 Enum.GetValues<CubeFace>())
        {
            _assets[(face, MapKind.Height)] =
                new MapAssetState(
                    face,
                    MapKind.Height);

            _assets[(face, MapKind.Material)] =
                new MapAssetState(
                    face,
                    MapKind.Material);
        }
    }

    public MapAssetState GetAsset(
        CubeFace face,
        MapKind kind)
    {
        return _assets[
            (face, kind)];
    }

    public void MarkDirty(
        CubeFace face,
        MapKind kind)
    {
        GetAsset(face, kind)
            .MarkDirty();
    }

    public void MarkClean(
        CubeFace face,
        MapKind kind)
    {
        GetAsset(face, kind)
            .MarkClean();
    }

    public bool TryUnloadCleanAsset(
        CubeFace face,
        MapKind kind)
    {
        return GetAsset(face, kind)
            .TryUnload();
    }

    public void DiscardAll()
    {
        foreach (var asset in
                 _assets.Values)
        {
            asset.Discard();
        }
    }
}