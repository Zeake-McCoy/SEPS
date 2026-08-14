using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using SEPS.Core;
using SEPS.Editor;
using SEPS.Rendering;

namespace SEPS.UI.Controls;

public sealed class SEPSOpenGlViewport
    : OpenGlControlBase
{
    private readonly OpenGlRenderer _renderer =
        new();
    private readonly MapViewportState _viewportState =
    new();

    private bool _initialized;
    private HeightMap? _pendingHeightMap;

    public MapViewportState ViewportState =>
        _viewportState;
    public bool IsRendererInitialized =>
    _initialized;

    public int RendererTextureWidth =>
        _renderer.TextureWidth;
    public int RendererTextureHeight =>
        _renderer.TextureHeight;

    public SEPSOpenGlViewport()
    {
        Focusable = true;
        IsTabStop = true;
        IsHitTestVisible = true;
    }

    public void SetHeightMap(
        HeightMap? heightMap)
    {
        _pendingHeightMap =
            heightMap;

        Console.WriteLine(
            heightMap == null
                ? "Viewport: heightmap cleared."
                : $"Viewport: heightmap queued " +
                  $"{heightMap.Width}x{heightMap.Height}.");

        if (_initialized)
            RequestNextFrameRendering();
    }

    public void RequestViewportRedraw()
    {
        if (_initialized)
            RequestNextFrameRendering();
    }

    protected override void OnOpenGlInit(
        GlInterface gl)
    {
        Console.WriteLine(
            "OpenGL INIT callback reached.");

        try
        {
            Console.WriteLine(
                "Creating SEPS OpenGL renderer...");

            _renderer.Initialize(
                gl.GetProcAddress);

            Console.WriteLine(
                "SEPS OpenGL renderer initialized.");

            _initialized = true;

            RequestNextFrameRendering();

            Console.WriteLine(
                "Requested first OpenGL frame.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"OpenGL INIT FAILED: {ex}");

            throw;
        }
    }

    protected override void OnOpenGlRender(
        GlInterface gl,
        int fb)
    {

        if (_pendingHeightMap != null)
        {
            var map =
                _pendingHeightMap;

            _pendingHeightMap = null;

            _renderer.SetHeightMap(
                map);
        }

        _renderer.Render(
            Math.Max(
                1,
                (int)Bounds.Width),
            Math.Max(
                1,
                (int)Bounds.Height),
            _viewportState.Zoom,
            _viewportState.PanX,
            _viewportState.PanY,
            _viewportState.Yaw);

        RequestNextFrameRendering();
    }

    protected override void OnOpenGlDeinit(
        GlInterface gl)
    {
        _initialized = false;

        _pendingHeightMap = null;

        _renderer.Dispose();
    }
}