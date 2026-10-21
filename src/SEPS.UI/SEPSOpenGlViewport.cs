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
    private bool _hasPendingHeightMapUpdate;

    private byte[]? _pendingMaterialPixels;
    private int _pendingMaterialWidth;
    private int _pendingMaterialHeight;
    private bool _pendingMaterialDisplay;
    private bool _hasPendingMaterialUpdate;

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
        _pendingHeightMap = heightMap;
        _hasPendingHeightMapUpdate = true;

        Console.WriteLine(
            heightMap == null
                ? "Viewport: heightmap cleared."
                : $"Viewport: heightmap queued " +
                  $"{heightMap.Width}x{heightMap.Height}.");

        if (_initialized)
            RequestNextFrameRendering();
    }

    
    public void SetMaterialChannel(
    MaterialMap? materialMap,
    MaterialChannel channel,
    bool enabled,
    bool rawIntensity = false)
    {
        if (enabled && materialMap != null)
        {
            _pendingMaterialPixels =
                materialMap.ExtractChannel(channel, rawIntensity);

            _pendingMaterialWidth =
                materialMap.Width;

            _pendingMaterialHeight =
                materialMap.Height;

            _pendingMaterialDisplay = true;
        }
        else
        {
            _pendingMaterialPixels = null;
            _pendingMaterialWidth = 0;
            _pendingMaterialHeight = 0;
            _pendingMaterialDisplay = false;
        }

        _hasPendingMaterialUpdate = true;

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

        if (_hasPendingHeightMapUpdate)
        {
            var map = _pendingHeightMap;

            _pendingHeightMap = null;
            _hasPendingHeightMapUpdate = false;

            if (map != null)
            {
                _renderer.SetHeightMap(map);
            }
            else
            {
                _renderer.ClearHeightMap();
            }
        }

        if (_hasPendingMaterialUpdate)
        {
            var pixels = _pendingMaterialPixels;
            var width = _pendingMaterialWidth;
            var height = _pendingMaterialHeight;
            var showMaterial = _pendingMaterialDisplay;

            _hasPendingMaterialUpdate = false;
            _pendingMaterialPixels = null;

            if (pixels != null)
            {
                _renderer.SetMaterialChannelImage(
                    width,
                    height,
                    pixels);
            }

            _renderer.SetMaterialDisplay(
                showMaterial);
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
        _hasPendingHeightMapUpdate = false;

        _pendingMaterialPixels = null;
        _pendingMaterialWidth = 0;
        _pendingMaterialHeight = 0;
        _pendingMaterialDisplay = false;
        _hasPendingMaterialUpdate = false;

        _renderer.Dispose();
    }
}