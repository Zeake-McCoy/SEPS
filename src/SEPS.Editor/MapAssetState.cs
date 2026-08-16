using SEPS.Core;

namespace SEPS.Editor;

public sealed class MapAssetState
{
    public CubeFace Face { get; }

    public MapKind Kind { get; }

    public bool IsLoaded =>
        Kind == MapKind.Height
            ? HeightMap != null
            : MaterialMap != null;

    public bool IsDirty { get; private set; }

    public HeightMap? HeightMap { get; private set; }

    public MaterialMap? MaterialMap { get; private set; }

    public MapAssetState(
        CubeFace face,
        MapKind kind)
    {
        Face = face;
        Kind = kind;
    }

    public void SetHeightMap(
        HeightMap? map)
    {
        if (Kind != MapKind.Height)
            throw new InvalidOperationException(
                "Asset state is not a heightmap.");

        HeightMap = map;
    }

    public void SetMaterialMap(
        MaterialMap? map)
    {
        if (Kind != MapKind.Material)
            throw new InvalidOperationException(
                "Asset state is not a material map.");

        MaterialMap = map;
    }

    public void MarkDirty()
    {
        IsDirty = true;
    }

    public void MarkClean()
    {
        IsDirty = false;
    }

    public void Unload()
    {
        if (IsDirty)
        {
            throw new InvalidOperationException(
                "Cannot unload a dirty map asset.");
        }

        HeightMap = null;
        MaterialMap = null;
    }

    public void Discard()
    {
        HeightMap = null;
        MaterialMap = null;
        IsDirty = false;
    }
}