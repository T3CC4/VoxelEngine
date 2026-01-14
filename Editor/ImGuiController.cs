using ImGuiNET;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Runtime.CompilerServices;

namespace VoxelEngine.Editor;

public class ImGuiController : IDisposable
{
    private bool disposed = false;
    private int vertexArray;
    private int vertexBuffer;
    private int elementBuffer;
    private int vertexBufferSize;
    private int elementBufferSize;
    private int fontTexture;
    private int shader;
    private int shaderFontTextureLocation;
    private int shaderProjectionMatrixLocation;

    private GameWindow window;
    private Vector2 scaleFactor = Vector2.One;

    public ImGuiController(GameWindow window)
    {
        this.window = window;

        IntPtr context = ImGui.CreateContext();
        ImGui.SetCurrentContext(context);

        var io = ImGui.GetIO();
        io.Fonts.AddFontDefault();
        io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;

        CreateDeviceResources();
        SetKeyMappings();

        SetPerFrameImGuiData(1f / 60f);
        ImGui.NewFrame();
    }

    private void CreateDeviceResources()
    {
        vertexArray = GL.GenVertexArray();
        vertexBuffer = GL.GenBuffer();
        elementBuffer = GL.GenBuffer();

        RecreateFontDeviceTexture();

        string vertexSource = @"#version 330 core
uniform mat4 projection_matrix;
layout(location = 0) in vec2 in_position;
layout(location = 1) in vec2 in_texCoord;
layout(location = 2) in vec4 in_color;
out vec4 color;
out vec2 texCoord;
void main()
{
    gl_Position = projection_matrix * vec4(in_position, 0, 1);
    color = in_color;
    texCoord = in_texCoord;
}";

        string fragmentSource = @"#version 330 core
uniform sampler2D in_fontTexture;
in vec4 color;
in vec2 texCoord;
out vec4 outputColor;
void main()
{
    outputColor = color * texture(in_fontTexture, texCoord);
}";

        shader = CreateProgram("ImGui", vertexSource, fragmentSource);
        shaderProjectionMatrixLocation = GL.GetUniformLocation(shader, "projection_matrix");
        shaderFontTextureLocation = GL.GetUniformLocation(shader, "in_fontTexture");

        GL.BindVertexArray(vertexArray);
        GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBuffer);
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, elementBuffer);

        int stride = Unsafe.SizeOf<ImDrawVert>();
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, stride, 0);
        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, 8);
        GL.EnableVertexAttribArray(2);
        GL.VertexAttribPointer(2, 4, VertexAttribPointerType.UnsignedByte, true, stride, 16);

        GL.BindVertexArray(0);
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
    }

    private void RecreateFontDeviceTexture()
    {
        var io = ImGui.GetIO();
        io.Fonts.GetTexDataAsRGBA32(out IntPtr pixels, out int width, out int height, out int bytesPerPixel);

        fontTexture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, fontTexture);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0,
            PixelFormat.Rgba, PixelType.UnsignedByte, pixels);

        io.Fonts.SetTexID((IntPtr)fontTexture);
        io.Fonts.ClearTexData();
    }

    private int CreateProgram(string name, string vertexSource, string fragmentSource)
    {
        int program = GL.CreateProgram();
        int vertex = CompileShader(name, ShaderType.VertexShader, vertexSource);
        int fragment = CompileShader(name, ShaderType.FragmentShader, fragmentSource);

        GL.AttachShader(program, vertex);
        GL.AttachShader(program, fragment);
        GL.LinkProgram(program);

        GL.DetachShader(program, vertex);
        GL.DetachShader(program, fragment);
        GL.DeleteShader(vertex);
        GL.DeleteShader(fragment);

        return program;
    }

    private int CompileShader(string name, ShaderType type, string source)
    {
        int shader = GL.CreateShader(type);
        GL.ShaderSource(shader, source);
        GL.CompileShader(shader);

        GL.GetShader(shader, ShaderParameter.CompileStatus, out int success);
        if (success == 0)
        {
            string info = GL.GetShaderInfoLog(shader);
            Console.WriteLine($"GL.CompileShader for {name} [{type}] had info log: {info}");
        }

        return shader;
    }

    private void SetKeyMappings()
    {
        var io = ImGui.GetIO();
        io.KeyMap[(int)ImGuiKey.Tab] = (int)Keys.Tab;
        io.KeyMap[(int)ImGuiKey.LeftArrow] = (int)Keys.Left;
        io.KeyMap[(int)ImGuiKey.RightArrow] = (int)Keys.Right;
        io.KeyMap[(int)ImGuiKey.UpArrow] = (int)Keys.Up;
        io.KeyMap[(int)ImGuiKey.DownArrow] = (int)Keys.Down;
        io.KeyMap[(int)ImGuiKey.PageUp] = (int)Keys.PageUp;
        io.KeyMap[(int)ImGuiKey.PageDown] = (int)Keys.PageDown;
        io.KeyMap[(int)ImGuiKey.Home] = (int)Keys.Home;
        io.KeyMap[(int)ImGuiKey.End] = (int)Keys.End;
        io.KeyMap[(int)ImGuiKey.Delete] = (int)Keys.Delete;
        io.KeyMap[(int)ImGuiKey.Backspace] = (int)Keys.Backspace;
        io.KeyMap[(int)ImGuiKey.Enter] = (int)Keys.Enter;
        io.KeyMap[(int)ImGuiKey.Escape] = (int)Keys.Escape;
        io.KeyMap[(int)ImGuiKey.A] = (int)Keys.A;
        io.KeyMap[(int)ImGuiKey.C] = (int)Keys.C;
        io.KeyMap[(int)ImGuiKey.V] = (int)Keys.V;
        io.KeyMap[(int)ImGuiKey.X] = (int)Keys.X;
        io.KeyMap[(int)ImGuiKey.Y] = (int)Keys.Y;
        io.KeyMap[(int)ImGuiKey.Z] = (int)Keys.Z;
    }

    private void SetPerFrameImGuiData(float deltaSeconds)
    {
        var io = ImGui.GetIO();
        io.DisplaySize = new System.Numerics.Vector2(window.ClientSize.X, window.ClientSize.Y);
        io.DisplayFramebufferScale = System.Numerics.Vector2.One;
        io.DeltaTime = deltaSeconds;
    }

    public void Update(float deltaTime)
    {
        SetPerFrameImGuiData(deltaTime);
        UpdateImGuiInput();
        ImGui.NewFrame();
    }

    private void UpdateImGuiInput()
    {
        var io = ImGui.GetIO();
        var mouseState = window.MouseState;
        var keyboardState = window.KeyboardState;

        io.MouseDown[0] = mouseState.IsButtonDown(MouseButton.Left);
        io.MouseDown[1] = mouseState.IsButtonDown(MouseButton.Right);
        io.MouseDown[2] = mouseState.IsButtonDown(MouseButton.Middle);

        io.MousePos = new System.Numerics.Vector2(mouseState.X, mouseState.Y);

        foreach (Keys key in Enum.GetValues(typeof(Keys)))
        {
            if ((int)key >= 0 && (int)key < 512)
            {
                io.KeysDown[(int)key] = keyboardState.IsKeyDown(key);
            }
        }

        io.KeyCtrl = keyboardState.IsKeyDown(Keys.LeftControl) || keyboardState.IsKeyDown(Keys.RightControl);
        io.KeyAlt = keyboardState.IsKeyDown(Keys.LeftAlt) || keyboardState.IsKeyDown(Keys.RightAlt);
        io.KeyShift = keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift);
        io.KeySuper = keyboardState.IsKeyDown(Keys.LeftSuper) || keyboardState.IsKeyDown(Keys.RightSuper);
    }

    public void Render()
    {
        ImGui.Render();
        RenderImDrawData(ImGui.GetDrawData());
    }

    private void RenderImDrawData(ImDrawDataPtr drawData)
    {
        if (drawData.CmdListsCount == 0) return;

        int framebufferWidth = (int)(drawData.DisplaySize.X * drawData.FramebufferScale.X);
        int framebufferHeight = (int)(drawData.DisplaySize.Y * drawData.FramebufferScale.Y);
        if (framebufferWidth <= 0 || framebufferHeight <= 0) return;

        GL.Enable(EnableCap.Blend);
        GL.Enable(EnableCap.ScissorTest);
        GL.BlendEquation(BlendEquationMode.FuncAdd);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        GL.Disable(EnableCap.CullFace);
        GL.Disable(EnableCap.DepthTest);

        GL.UseProgram(shader);

        float L = drawData.DisplayPos.X;
        float R = drawData.DisplayPos.X + drawData.DisplaySize.X;
        float T = drawData.DisplayPos.Y;
        float B = drawData.DisplayPos.Y + drawData.DisplaySize.Y;

        Matrix4 mvp = new Matrix4(
            2.0f / (R - L), 0.0f, 0.0f, 0.0f,
            0.0f, 2.0f / (T - B), 0.0f, 0.0f,
            0.0f, 0.0f, -1.0f, 0.0f,
            (R + L) / (L - R), (T + B) / (B - T), 0.0f, 1.0f
        );

        GL.UniformMatrix4(shaderProjectionMatrixLocation, false, ref mvp);
        GL.Uniform1(shaderFontTextureLocation, 0);

        GL.BindVertexArray(vertexArray);

        drawData.ScaleClipRects(drawData.FramebufferScale);

        for (int n = 0; n < drawData.CmdListsCount; n++)
        {
            ImDrawListPtr cmdList = drawData.CmdListsRange[n];

            GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBuffer);
            GL.BufferData(BufferTarget.ArrayBuffer, cmdList.VtxBuffer.Size * Unsafe.SizeOf<ImDrawVert>(),
                cmdList.VtxBuffer.Data, BufferUsageHint.StreamDraw);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, elementBuffer);
            GL.BufferData(BufferTarget.ElementArrayBuffer, cmdList.IdxBuffer.Size * sizeof(ushort),
                cmdList.IdxBuffer.Data, BufferUsageHint.StreamDraw);

            for (int cmdIndex = 0; cmdIndex < cmdList.CmdBuffer.Size; cmdIndex++)
            {
                ImDrawCmdPtr cmd = cmdList.CmdBuffer[cmdIndex];

                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, (int)cmd.TextureId);

                var clip = cmd.ClipRect;
                GL.Scissor((int)clip.X, framebufferHeight - (int)clip.W,
                    (int)(clip.Z - clip.X), (int)(clip.W - clip.Y));

                GL.DrawElementsBaseVertex(PrimitiveType.Triangles, (int)cmd.ElemCount,
                    DrawElementsType.UnsignedShort, (IntPtr)(cmd.IdxOffset * sizeof(ushort)),
                    (int)cmd.VtxOffset);
            }
        }

        GL.Disable(EnableCap.Blend);
        GL.Disable(EnableCap.ScissorTest);
        GL.Enable(EnableCap.DepthTest);
        GL.Enable(EnableCap.CullFace);

        GL.BindVertexArray(0);
        GL.UseProgram(0);
    }

    public void Dispose()
    {
        if (!disposed)
        {
            GL.DeleteVertexArray(vertexArray);
            GL.DeleteBuffer(vertexBuffer);
            GL.DeleteBuffer(elementBuffer);
            GL.DeleteTexture(fontTexture);
            GL.DeleteProgram(shader);
            disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
