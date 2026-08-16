using SEPS.Core;
using SEPS.Rendering;

namespace SEPS.Editor;

public sealed class EditorState
{
    public Planet? Planet { get; private set; }
    public PlanetEditingSession? Session { get; private set; }
    public CubeFace SelectedFace { get; set; } = CubeFace.Front;
        public List<IRenderLayer> Layers { get; } =
    [
        new RenderLayer("terrain", "Terrain"),
        new RenderLayer("materials", "Materials / Biomes"),
        new RenderLayer("resources", "Resources"),
        new RenderLayer("ecosystem", "Ecosystem"),
        new RenderLayer("water", "Water"),
        new RenderLayer("contours", "Topographic Lines"),
        new RenderLayer("grid", "Geographic Grid"),
        new RenderLayer("wireframe", "Wireframe")
    ];

    public MapViewportState Viewport { get; } =
        new();

    public void SetPlanet(Planet planet)
    {
        Session?
            .DiscardAll();
        Planet =
            planet;
        Session =
            new PlanetEditingSession(
                planet);
    }
}
