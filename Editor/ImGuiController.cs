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
        // Modern ImGui uses ImGuiKey enums directly, no mapping needed
        var io = ImGui.GetIO();
        io.BackendFlags |= ImGuiBackendFlags.HasMouseCursors;
        io.BackendFlags |= ImGuiBackendFlags.HasSetMousePos;
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

    private Dictionary<Keys, ImGuiKey> keyMap = new()
    {
        { Keys.Tab, ImGuiKey.Tab },
        { Keys.Left, ImGuiKey.LeftArrow },
        { Keys.Right, ImGuiKey.RightArrow },
        { Keys.Up, ImGuiKey.UpArrow },
        { Keys.Down, ImGuiKey.DownArrow },
        { Keys.PageUp, ImGuiKey.PageUp },
        { Keys.PageDown, ImGuiKey.PageDown },
        { Keys.Home, ImGuiKey.Home },
        { Keys.End, ImGuiKey.End },
        { Keys.Insert, ImGuiKey.Insert },
        { Keys.Delete, ImGuiKey.Delete },
        { Keys.Backspace, ImGuiKey.Backspace },
        { Keys.Space, ImGuiKey.Space },
        { Keys.Enter, ImGuiKey.Enter },
        { Keys.Escape, ImGuiKey.Escape },
        { Keys.Apostrophe, ImGuiKey.Apostrophe },
        { Keys.Comma, ImGuiKey.Comma },
        { Keys.Minus, ImGuiKey.Minus },
        { Keys.Period, ImGuiKey.Period },
        { Keys.Slash, ImGuiKey.Slash },
        { Keys.Semicolon, ImGuiKey.Semicolon },
        { Keys.Equal, ImGuiKey.Equal },
        { Keys.LeftBracket, ImGuiKey.LeftBracket },
        { Keys.Backslash, ImGuiKey.Backslash },
        { Keys.RightBracket, ImGuiKey.RightBracket },
        { Keys.GraveAccent, ImGuiKey.GraveAccent },
        { Keys.CapsLock, ImGuiKey.CapsLock },
        { Keys.ScrollLock, ImGuiKey.ScrollLock },
        { Keys.NumLock, ImGuiKey.NumLock },
        { Keys.PrintScreen, ImGuiKey.PrintScreen },
        { Keys.Pause, ImGuiKey.Pause },
        { Keys.KeyPad0, ImGuiKey.Keypad0 },
        { Keys.KeyPad1, ImGuiKey.Keypad1 },
        { Keys.KeyPad2, ImGuiKey.Keypad2 },
        { Keys.KeyPad3, ImGuiKey.Keypad3 },
        { Keys.KeyPad4, ImGuiKey.Keypad4 },
        { Keys.KeyPad5, ImGuiKey.Keypad5 },
        { Keys.KeyPad6, ImGuiKey.Keypad6 },
        { Keys.KeyPad7, ImGuiKey.Keypad7 },
        { Keys.KeyPad8, ImGuiKey.Keypad8 },
        { Keys.KeyPad9, ImGuiKey.Keypad9 },
        { Keys.KeyPadDecimal, ImGuiKey.KeypadDecimal },
        { Keys.KeyPadDivide, ImGuiKey.KeypadDivide },
        { Keys.KeyPadMultiply, ImGuiKey.KeypadMultiply },
        { Keys.KeyPadSubtract, ImGuiKey.KeypadSubtract },
        { Keys.KeyPadAdd, ImGuiKey.KeypadAdd },
        { Keys.KeyPadEnter, ImGuiKey.KeypadEnter },
        { Keys.KeyPadEqual, ImGuiKey.KeypadEqual },
        { Keys.LeftShift, ImGuiKey.LeftShift },
        { Keys.LeftControl, ImGuiKey.LeftCtrl },
        { Keys.LeftAlt, ImGuiKey.LeftAlt },
        { Keys.LeftSuper, ImGuiKey.LeftSuper },
        { Keys.RightShift, ImGuiKey.RightShift },
        { Keys.RightControl, ImGuiKey.RightCtrl },
        { Keys.RightAlt, ImGuiKey.RightAlt },
        { Keys.RightSuper, ImGuiKey.RightSuper },
        { Keys.Menu, ImGuiKey.Menu },
        { Keys.D0, ImGuiKey._0 },
        { Keys.D1, ImGuiKey._1 },
        { Keys.D2, ImGuiKey._2 },
        { Keys.D3, ImGuiKey._3 },
        { Keys.D4, ImGuiKey._4 },
        { Keys.D5, ImGuiKey._5 },
        { Keys.D6, ImGuiKey._6 },
        { Keys.D7, ImGuiKey._7 },
        { Keys.D8, ImGuiKey._8 },
        { Keys.D9, ImGuiKey._9 },
        { Keys.A, ImGuiKey.A },
        { Keys.B, ImGuiKey.B },
        { Keys.C, ImGuiKey.C },
        { Keys.D, ImGuiKey.D },
        { Keys.E, ImGuiKey.E },
        { Keys.F, ImGuiKey.F },
        { Keys.G, ImGuiKey.G },
        { Keys.H, ImGuiKey.H },
        { Keys.I, ImGuiKey.I },
        { Keys.J, ImGuiKey.J },
        { Keys.K, ImGuiKey.K },
        { Keys.L, ImGuiKey.L },
        { Keys.M, ImGuiKey.M },
        { Keys.N, ImGuiKey.N },
        { Keys.O, ImGuiKey.O },
        { Keys.P, ImGuiKey.P },
        { Keys.Q, ImGuiKey.Q },
        { Keys.R, ImGuiKey.R },
        { Keys.S, ImGuiKey.S },
        { Keys.T, ImGuiKey.T },
        { Keys.U, ImGuiKey.U },
        { Keys.V, ImGuiKey.V },
        { Keys.W, ImGuiKey.W },
        { Keys.X, ImGuiKey.X },
        { Keys.Y, ImGuiKey.Y },
        { Keys.Z, ImGuiKey.Z },
        { Keys.F1, ImGuiKey.F1 },
        { Keys.F2, ImGuiKey.F2 },
        { Keys.F3, ImGuiKey.F3 },
        { Keys.F4, ImGuiKey.F4 },
        { Keys.F5, ImGuiKey.F5 },
        { Keys.F6, ImGuiKey.F6 },
        { Keys.F7, ImGuiKey.F7 },
        { Keys.F8, ImGuiKey.F8 },
        { Keys.F9, ImGuiKey.F9 },
        { Keys.F10, ImGuiKey.F10 },
        { Keys.F11, ImGuiKey.F11 },
        { Keys.F12, ImGuiKey.F12 },
    };

    private HashSet<Keys> pressedKeys = new();

    private void UpdateImGuiInput()
    {
        var io = ImGui.GetIO();
        var mouseState = window.MouseState;
        var keyboardState = window.KeyboardState;

        // Mouse input
        io.AddMouseButtonEvent(0, mouseState.IsButtonDown(MouseButton.Left));
        io.AddMouseButtonEvent(1, mouseState.IsButtonDown(MouseButton.Right));
        io.AddMouseButtonEvent(2, mouseState.IsButtonDown(MouseButton.Middle));

        io.AddMousePosEvent(mouseState.X, mouseState.Y);

        // Mouse wheel
        io.AddMouseWheelEvent(mouseState.ScrollDelta.X, mouseState.ScrollDelta.Y);

        // Keyboard input using modern API
        var currentPressedKeys = new HashSet<Keys>();

        foreach (var kvp in keyMap)
        {
            bool isDown = keyboardState.IsKeyDown(kvp.Key);
            if (isDown)
            {
                currentPressedKeys.Add(kvp.Key);
                if (!pressedKeys.Contains(kvp.Key))
                {
                    // Key just pressed
                    io.AddKeyEvent(kvp.Value, true);
                }
            }
            else if (pressedKeys.Contains(kvp.Key))
            {
                // Key just released
                io.AddKeyEvent(kvp.Value, false);
            }
        }

        pressedKeys = currentPressedKeys;

        // Modifier keys
        io.AddKeyEvent(ImGuiKey.ModCtrl, keyboardState.IsKeyDown(Keys.LeftControl) || keyboardState.IsKeyDown(Keys.RightControl));
        io.AddKeyEvent(ImGuiKey.ModShift, keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift));
        io.AddKeyEvent(ImGuiKey.ModAlt, keyboardState.IsKeyDown(Keys.LeftAlt) || keyboardState.IsKeyDown(Keys.RightAlt));
        io.AddKeyEvent(ImGuiKey.ModSuper, keyboardState.IsKeyDown(Keys.LeftSuper) || keyboardState.IsKeyDown(Keys.RightSuper));
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
