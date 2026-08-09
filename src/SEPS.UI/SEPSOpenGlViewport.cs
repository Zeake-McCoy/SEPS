using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using SEPS.Rendering;

namespace SEPS.UI.Controls;

public sealed class SEPSOpenGlViewport
    : OpenGlControlBase
{
    private readonly OpenGlRenderer _renderer =
        new();

    private bool _initialized;

    protected override void OnOpenGlInit(
        GlInterface gl)
    {
        Console.WriteLine("OpenGL INIT callback reached.");

        try
        {
            Console.WriteLine("Creating SEPS OpenGL renderer...");

            _renderer.Initialize(
                gl.GetProcAddress);

            Console.WriteLine("SEPS OpenGL renderer initialized.");

            _initialized = true;

            RequestNextFrameRendering();

            Console.WriteLine("Requested first OpenGL frame.");
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
        Console.WriteLine(
            $"OpenGL RENDER callback reached: {Bounds.Width}x{Bounds.Height}, FB={fb}");

        if (!_initialized)
        {
            Console.WriteLine(
                "OpenGL render skipped: renderer not initialized.");

            return;
        }

        _renderer.Render(
            Math.Max(1, (int)Bounds.Width),
            Math.Max(1, (int)Bounds.Height));

        RequestNextFrameRendering();
    }

    protected override void OnOpenGlDeinit(
        GlInterface gl)
    {
        _initialized = false;

        _renderer.Dispose();
    }
}