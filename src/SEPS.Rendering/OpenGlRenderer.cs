using SEPS.Core;
using Silk.NET.OpenGL;

namespace SEPS.Rendering;

public sealed unsafe class OpenGlRenderer : IDisposable
{
    private GL? _gl;

    private uint _vertexArray;
    private uint _vertexBuffer;
    private uint _shaderProgram;
    private uint _heightTexture;
    private uint _materialTexture;

    private int _heightMapLocation;
    private int _materialMapLocation;
    private int _showMaterialLocation;

    private bool _showMaterial;

    private int _zoomLocation;
    private int _panLocation;
    private int _yawLocation;

    private int _textureWidth;
    private int _textureHeight;

    public bool IsInitialized =>
    _gl != null;
    public int TextureWidth =>
        _textureWidth;
    public int TextureHeight =>
        _textureHeight;

    private static readonly float[] Vertices =
    {
        // Position      UV
        -1f, -1f,        0f, 1f,
         1f, -1f,        1f, 1f,
         1f,  1f,        1f, 0f,

        -1f, -1f,        0f, 1f,
         1f,  1f,        1f, 0f,
        -1f,  1f,        0f, 0f
    };

    private const string VertexShader = """
        #version 300 es
        precision mediump float;

        layout (location = 0) in vec2 aPosition;
        layout (location = 1) in vec2 aTexCoord;

        uniform float uZoom;
        uniform vec2 uPan;
        uniform float uYaw;

        out vec2 vTexCoord;

        void main()
        {
            float angle =
                radians(uYaw);

            float c =
                cos(angle);

            float s =
                sin(angle);

            mat2 rotation =
                mat2(
                    c, -s,
                    s,  c);

            vec2 position =
                rotation *
                (aPosition + uPan);

            position *= uZoom;

            gl_Position =
                vec4(
                    position,
                    0.0,
                    1.0);

            vTexCoord = aTexCoord;
        }
        """;

    
    private const string FragmentShader = """
        #version 300 es
        precision mediump float;

        in vec2 vTexCoord;

        uniform sampler2D uHeightMap;
        uniform sampler2D uMaterialMap;
        uniform bool uShowMaterial;

        out vec4 FragColor;

        void main()
        {
            if (uShowMaterial)
            {
                FragColor = texture(
                    uMaterialMap,
                    vTexCoord);
            }
            else
            {
                float height = texture(
                    uHeightMap,
                    vTexCoord).r;

                FragColor = vec4(
                    height,
                    height,
                    height,
                    1.0);
            }
        }
        """;

    public void Initialize(
        Func<string, nint> getProcAddress)
    {
        if (_gl != null)
            return;

        _gl =
            GL.GetApi(getProcAddress);

        _shaderProgram =
            CreateShaderProgram();

        _heightMapLocation =
            _gl.GetUniformLocation(
                _shaderProgram,
                "uHeightMap");

        _materialMapLocation =
            _gl.GetUniformLocation(
                _shaderProgram,
                "uMaterialMap");

        _showMaterialLocation =
            _gl.GetUniformLocation(
                _shaderProgram,
                "uShowMaterial");

        _zoomLocation =
            _gl.GetUniformLocation(
                _shaderProgram,
                "uZoom");

        _panLocation =
            _gl.GetUniformLocation(
                _shaderProgram,
                "uPan");

        _yawLocation =
            _gl.GetUniformLocation(
                _shaderProgram,
                "uYaw");

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
                (nuint)(
                    Vertices.Length *
                    sizeof(float)),
                vertices,
                BufferUsageARB.StaticDraw);
        }

        _gl.VertexAttribPointer(
            0,
            2,
            VertexAttribPointerType.Float,
            false,
            4 * sizeof(float),
            0);

        _gl.EnableVertexAttribArray(0);

        _gl.VertexAttribPointer(
            1,
            2,
            VertexAttribPointerType.Float,
            false,
            4 * sizeof(float),
            2 * sizeof(float));

        _gl.EnableVertexAttribArray(1);

        _gl.BindVertexArray(0);
        _gl.BindBuffer(
            BufferTargetARB.ArrayBuffer,
            0);
    }

    public void SetHeightMap(
        HeightMap heightMap)
    {
        if (_gl == null)
        {
            Console.WriteLine(
                "OpenGL renderer: SetHeightMap called before initialization.");

            return;
        }

        var gl = _gl;
        if (_heightTexture == 0)
        {
            _heightTexture =
                gl.GenTexture();
        }

        gl.BindTexture(
            TextureTarget.Texture2D,
            _heightTexture);

        _gl.TexParameter(
            TextureTarget.Texture2D,
            TextureParameterName.TextureMinFilter,
            (int)TextureMinFilter.Nearest);

        _gl.TexParameter(
            TextureTarget.Texture2D,
            TextureParameterName.TextureMagFilter,
            (int)TextureMagFilter.Nearest);

        gl.TexParameter(
            TextureTarget.Texture2D,
            TextureParameterName.TextureWrapS,
            (int)GLEnum.ClampToEdge);

        gl.TexParameter(
            TextureTarget.Texture2D,
            TextureParameterName.TextureWrapT,
            (int)GLEnum.ClampToEdge);

        fixed (ushort* samples =
            heightMap.Samples)
        {
            gl.TexImage2D(
                TextureTarget.Texture2D,
                0,
                InternalFormat.R16,
                (uint)heightMap.Width,
                (uint)heightMap.Height,
                0,
                PixelFormat.Red,
                PixelType.UnsignedShort,
                samples);
        }

        gl.BindTexture(
            TextureTarget.Texture2D,
            0);

        _textureWidth =
            heightMap.Width;

        _textureHeight =
            heightMap.Height;
    }

    public void ClearHeightMap()
    {
        if (_gl == null)
            return;

        if (_heightTexture != 0)
        {
            _gl.DeleteTexture(_heightTexture);
            _heightTexture = 0;
        }

        _textureWidth = 0;
        _textureHeight = 0;
    }

    public void SetMaterialChannelImage(
        int width,
        int height,
        byte[] rgba)
    {
        if (_gl == null)
        {
            Console.WriteLine(
                "OpenGL renderer: material image upload " +
                "requested before initialization.");

            return;
        }

        if (width <= 0 ||
            height <= 0 ||
            rgba.Length != checked(width * height * 4))
        {
            throw new ArgumentException(
                "Invalid material channel image dimensions or data.");
        }

        var gl = _gl;

        if (_materialTexture == 0)
            _materialTexture = gl.GenTexture();

        gl.BindTexture(
            TextureTarget.Texture2D,
            _materialTexture);

        gl.TexParameter(
            TextureTarget.Texture2D,
            TextureParameterName.TextureMinFilter,
            (int)GLEnum.Nearest);

        gl.TexParameter(
            TextureTarget.Texture2D,
            TextureParameterName.TextureMagFilter,
            (int)GLEnum.Nearest);

        gl.TexParameter(
            TextureTarget.Texture2D,
            TextureParameterName.TextureWrapS,
            (int)GLEnum.ClampToEdge);

        gl.TexParameter(
            TextureTarget.Texture2D,
            TextureParameterName.TextureWrapT,
            (int)GLEnum.ClampToEdge);

        fixed (byte* pixels = rgba)
        {
            gl.TexImage2D(
                TextureTarget.Texture2D,
                0,
                InternalFormat.Rgba8,
                (uint)width,
                (uint)height,
                0,
                PixelFormat.Rgba,
                PixelType.UnsignedByte,
                pixels);
        }

        gl.BindTexture(
            TextureTarget.Texture2D,
            0);
    }

    public void SetMaterialDisplay(bool enabled)
    {
        _showMaterial = enabled;
    }

        public void Render(
            int width,
            int height,
            double zoom,
            double panX,
            double panY,
            double yaw)
    {
        if (_gl == null)
            return;

        var gl = _gl;

        var viewportSize =
            Math.Min(
                width,
                height);

        var viewportX =
            (width - viewportSize) / 2;

        var viewportY =
            (height - viewportSize) / 2;

        gl.Viewport(
            viewportX,
            viewportY,
            (uint)Math.Max(viewportSize, 1),
            (uint)Math.Max(viewportSize, 1));

        gl.ClearColor(
            0.055f,
            0.065f,
            0.08f,
            1.0f);

        gl.Clear(ClearBufferMask.ColorBufferBit);

        gl.UseProgram(_shaderProgram);

        gl.Uniform1(
            _zoomLocation,
            (float)zoom);

        gl.Uniform2(
            _panLocation,
            (float)panX,
            (float)panY);

        gl.Uniform1(
            _yawLocation,
            (float)yaw);
        
        gl.ActiveTexture(TextureUnit.Texture0);
        gl.BindTexture(
            TextureTarget.Texture2D,
            _heightTexture);

        gl.Uniform1(_heightMapLocation, 0);

        gl.ActiveTexture(TextureUnit.Texture1);
        gl.BindTexture(
            TextureTarget.Texture2D,
            _materialTexture);

        gl.Uniform1(_materialMapLocation, 1);

        gl.Uniform1(
            _showMaterialLocation,
            _showMaterial ? 1 : 0);

        // Bind the geometry and draw the two triangles.
        gl.BindVertexArray(_vertexArray);
        gl.DrawArrays(PrimitiveType.Triangles, 0, 6);

        // Unbind textures after drawing.
        gl.BindVertexArray(0);

        gl.ActiveTexture(TextureUnit.Texture1);
        gl.BindTexture(TextureTarget.Texture2D, 0);

        gl.ActiveTexture(TextureUnit.Texture0);
        gl.BindTexture(TextureTarget.Texture2D, 0);

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

        gl.LinkProgram(
            program);

        gl.GetProgram(
            program,
            ProgramPropertyARB.LinkStatus,
            out var linked);

        if (linked == 0)
        {
            var log =
                gl.GetProgramInfoLog(
                    program);

            gl.DeleteProgram(
                program);

            gl.DeleteShader(
                vertexShader);

            gl.DeleteShader(
                fragmentShader);

            throw new InvalidOperationException(
                $"OpenGL shader program link failed: {log}");
        }

        gl.DetachShader(
            program,
            vertexShader);

        gl.DetachShader(
            program,
            fragmentShader);

        gl.DeleteShader(
            vertexShader);

        gl.DeleteShader(
            fragmentShader);

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

        gl.CompileShader(
            shader);

        gl.GetShader(
            shader,
            ShaderParameterName.CompileStatus,
            out var compiled);

        if (compiled == 0)
        {
            var log =
                gl.GetShaderInfoLog(
                    shader);

            gl.DeleteShader(
                shader);

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

        if (_heightTexture != 0)
            gl.DeleteTexture(
                _heightTexture);

        if (_materialTexture != 0)
            gl.DeleteTexture(_materialTexture);

        if (_vertexBuffer != 0)
            gl.DeleteBuffer(
                _vertexBuffer);

        if (_vertexArray != 0)
            gl.DeleteVertexArray(
                _vertexArray);

        if (_shaderProgram != 0)
            gl.DeleteProgram(
                _shaderProgram);

        _heightTexture = 0;
        _materialTexture = 0;
        _vertexBuffer = 0;
        _vertexArray = 0;
        _shaderProgram = 0;

        gl.Dispose();

        _gl = null;
    }
}