using Silk.NET.OpenGL;

namespace SEPS.Rendering;

/// <summary>
/// First-stage SEPS OpenGL renderer.
///
/// V0.2.0 establishes the rendering context and pipeline.
/// Planet/map rendering is introduced in later V0.2.x milestones.
/// </summary>
public sealed unsafe class OpenGlRenderer : IDisposable
{
    private GL? _gl;

    private uint _vertexArray;
    private uint _vertexBuffer;
    private uint _shaderProgram;

    private static readonly float[] Vertices =
    {
        -0.65f, -0.55f,
         0.65f, -0.55f,
         0.00f,  0.65f
    };

    private const string VertexShader = """
        #version 300 es
        precision mediump float;

        layout (location = 0) in vec2 aPosition;

        void main()
        {
            gl_Position = vec4(aPosition, 0.0, 1.0);
        }
        """;

    private const string FragmentShader = """
        #version 300 es
        precision mediump float;

        out vec4 FragColor;

        void main()
        {
            FragColor = vec4(0.25, 0.65, 0.95, 1.0);
        }
        """;

    public void Initialize(Func<string, nint> getProcAddress)
    {
        if (_gl != null)
            return;

        _gl = GL.GetApi(getProcAddress);

        _shaderProgram =
            CreateShaderProgram();

        _vertexArray =
            _gl.GenVertexArray();

        _vertexBuffer =
            _gl.GenBuffer();

        _gl.BindVertexArray(
            _vertexArray);

        _gl.BindBuffer(
            BufferTargetARB.ArrayBuffer,
            _vertexBuffer);

        fixed (float* vertices = Vertices)
        {
            _gl.BufferData(
                BufferTargetARB.ArrayBuffer,
                (nuint)(Vertices.Length * sizeof(float)),
                vertices,
                BufferUsageARB.StaticDraw);
        }

        _gl.VertexAttribPointer(
            0,
            2,
            VertexAttribPointerType.Float,
            false,
            2 * sizeof(float),
            0);

        _gl.EnableVertexAttribArray(0);

        _gl.BindVertexArray(0);
        _gl.BindBuffer(
            BufferTargetARB.ArrayBuffer,
            0);
    }

    public void Render(
        int width,
        int height)
    {
        if (_gl == null)
            return;

        var gl = _gl;

        gl.Viewport(
            0,
            0,
            (uint)Math.Max(width, 1),
            (uint)Math.Max(height, 1));

        gl.ClearColor(
            0.055f,
            0.065f,
            0.08f,
            1.0f);

        gl.Clear(
            ClearBufferMask.ColorBufferBit);

        gl.UseProgram(
            _shaderProgram);

        gl.BindVertexArray(
            _vertexArray);

        gl.DrawArrays(
            PrimitiveType.Triangles,
            0,
            3);

        gl.BindVertexArray(0);
        gl.UseProgram(0);
    }

    private uint CreateShaderProgram()
    {
        var gl = _gl!;

        var vertexShader =
            CompileShader(
                ShaderType.VertexShader,
                VertexShader);

        var fragmentShader =
            CompileShader(
                ShaderType.FragmentShader,
                FragmentShader);

        var program =
            gl.CreateProgram();

        gl.AttachShader(
            program,
            vertexShader);

        gl.AttachShader(
            program,
            fragmentShader);

        gl.LinkProgram(program);

        gl.GetProgram(
            program,
            ProgramPropertyARB.LinkStatus,
            out var linked);

        if (linked == 0)
        {
            var log =
                gl.GetProgramInfoLog(program);

            gl.DeleteProgram(program);
            gl.DeleteShader(vertexShader);
            gl.DeleteShader(fragmentShader);

            throw new InvalidOperationException(
                $"OpenGL shader program link failed: {log}");
        }

        gl.DetachShader(
            program,
            vertexShader);

        gl.DetachShader(
            program,
            fragmentShader);

        gl.DeleteShader(vertexShader);
        gl.DeleteShader(fragmentShader);

        return program;
    }

    private uint CompileShader(
        ShaderType type,
        string source)
    {
        var gl = _gl!;

        var shader =
            gl.CreateShader(type);

        gl.ShaderSource(
            shader,
            source);

        gl.CompileShader(shader);

        gl.GetShader(
            shader,
            ShaderParameterName.CompileStatus,
            out var compiled);

        if (compiled == 0)
        {
            var log =
                gl.GetShaderInfoLog(shader);

            gl.DeleteShader(shader);

            throw new InvalidOperationException(
                $"OpenGL {type} shader compilation failed: {log}");
        }

        return shader;
    }

    public void Dispose()
    {
        if (_gl == null)
            return;

        var gl = _gl;

        if (_vertexBuffer != 0)
            gl.DeleteBuffer(_vertexBuffer);

        if (_vertexArray != 0)
            gl.DeleteVertexArray(_vertexArray);

        if (_shaderProgram != 0)
            gl.DeleteProgram(_shaderProgram);

        _vertexBuffer = 0;
        _vertexArray = 0;
        _shaderProgram = 0;

        gl.Dispose();
        _gl = null;
    }
}