using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using VoxelEngine.Core;
using VoxelEngine.Decorations;
using VoxelEngine.World;
using ImGuiNET;
using VoxelEngine.Graphics;
using MathHelper = OpenTK.Mathematics.MathHelper;

namespace VoxelEngine.Editor;

/// <summary>
/// Editor window for creating decorations using mini-voxels
/// </summary>
public class DecorationEditorWindow : GameWindow
{
    private Decoration currentDecoration;
    private Camera camera;
    private Shader? shader;
    private Shader? gridShader;
    private Shader? blockShader; // For rendering spawn block
    private ImGuiController? imguiController;

    private int vao, vbo;
    private int gridVAO, gridVBO;
    private int blockVAO, blockVBO, blockEBO;
    private const int InfiniteGridSize = 20; // Large grid for "infinite" feel
    private const float GridFadeDistance = 15.0f;

    private Color selectedColor = Color.GrassGreen; // Default grass color
    private bool mouseCaptured = false;
    private bool firstMove = true;
    private Vector2 lastMousePos;

    // Blender-like controls
    private bool isRotating = false;
    private bool isPanning = false;
    private Vector3 focusPoint = new Vector3(0.5f, 0.5f, 0.5f); // Center of decoration block
    private float orbitDistance = 3.0f;

    private string decorationName = "NewDecoration";
    private int resolution = 4;
    private float density = 0.3f;
    private bool[] biomesEnabled = new bool[5];
    private bool[] groundBlocksEnabled = new bool[9]; // For each solid VoxelType

    private List<Decoration> availableDecorations = new();
    private int selectedLoadDecorationIndex = -1;

    public DecorationEditorWindow(Decoration? existingDecoration = null)
        : base(GameWindowSettings.Default, new NativeWindowSettings()
        {
            Size = new Vector2i(1280, 720),
            Title = "Decoration Editor (Mini-Voxels)",
            StartVisible = true,
            WindowBorder = WindowBorder.Resizable,
            API = ContextAPI.OpenGL,
            APIVersion = new Version(3, 3),
            Profile = ContextProfile.Core
        })
    {
        currentDecoration = existingDecoration ?? new Decoration("NewDecoration", 4);
        decorationName = currentDecoration.Name;
        resolution = currentDecoration.Resolution;
        density = currentDecoration.Density;

        // Initialize biome checkboxes
        for (int i = 0; i < biomesEnabled.Length; i++)
        {
            biomesEnabled[i] = currentDecoration.AllowedBiomes.Contains((BiomeType)i);
        }

        // Initialize ground block checkboxes
        for (int i = 0; i < groundBlocksEnabled.Length; i++)
        {
            var voxelType = (VoxelType)(i + 1); // Skip Air
            groundBlocksEnabled[i] = currentDecoration.RequiredGroundBlocks.Contains(voxelType);
        }

        VSync = VSyncMode.On;
    }

    protected override void OnLoad()
    {
        base.OnLoad();

        GL.ClearColor(0.2f, 0.2f, 0.2f, 1.0f);
        GL.Enable(EnableCap.DepthTest);
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        // Initialize camera in orbit around decoration center
        UpdateCameraOrbit();
        camera = new Camera(GetOrbitPosition(), Size.X / (float)Size.Y);
        camera.Pitch = -30;
        camera.Yaw = -45;

        // Load unlit shader from shader system
        shader = new Shader("Shaders/voxel_unlit.vert", "Shaders/voxel_unlit.frag");
        blockShader = new Shader("Shaders/voxel_unlit.vert", "Shaders/voxel_unlit.frag");
        CreateGridShader();

        // Initialize ImGui
        imguiController = new ImGuiController(this);

        // Create grid, voxel, and spawn block buffers
        CreateInfiniteGrid();
        CreateMiniVoxelBuffer();
        CreateSpawnBlock();

        // Load available decorations
        LoadAvailableDecorations();
    }

    private Vector3 GetOrbitPosition()
    {
        float radYaw = MathHelper.DegreesToRadians(camera?.Yaw ?? -45);
        float radPitch = MathHelper.DegreesToRadians(camera?.Pitch ?? -30);

        return focusPoint + new Vector3(
            MathF.Cos(radPitch) * MathF.Cos(radYaw),
            MathF.Sin(radPitch),
            MathF.Cos(radPitch) * MathF.Sin(radYaw)
        ) * orbitDistance;
    }

    private void UpdateCameraOrbit()
    {
        if (camera != null)
        {
            Vector3 direction = (focusPoint - camera.Position).Normalized();
            camera.Position = focusPoint - direction * orbitDistance;
        }
    }

    private void CreateGridShader()
    {
        string vertexSource = @"#version 330 core
layout(location = 0) in vec3 aPosition;
uniform mat4 view;
uniform mat4 projection;
void main()
{
    gl_Position = projection * view * vec4(aPosition, 1.0);
}";

        string fragmentSource = @"#version 330 core
out vec4 FragColor;
uniform vec4 color;
void main()
{
    FragColor = color;
}";

        int vertexShader = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(vertexShader, vertexSource);
        GL.CompileShader(vertexShader);

        int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(fragmentShader, fragmentSource);
        GL.CompileShader(fragmentShader);

        int program = GL.CreateProgram();
        GL.AttachShader(program, vertexShader);
        GL.AttachShader(program, fragmentShader);
        GL.LinkProgram(program);

        GL.DetachShader(program, vertexShader);
        GL.DetachShader(program, fragmentShader);
        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);

        gridShader = new Shader(program);
    }

    private void CreateInfiniteGrid()
    {
        List<float> vertices = new();

        // Create a large grid centered at origin but extending far in all directions
        // This creates an "infinite" feel like Blender
        int halfSize = InfiniteGridSize / 2;

        // Lines along X axis (parallel to X)
        for (int z = -halfSize; z <= halfSize; z++)
        {
            vertices.Add(-halfSize); vertices.Add(0); vertices.Add(z);
            vertices.Add(halfSize); vertices.Add(0); vertices.Add(z);
        }

        // Lines along Z axis (parallel to Z)
        for (int x = -halfSize; x <= halfSize; x++)
        {
            vertices.Add(x); vertices.Add(0); vertices.Add(-halfSize);
            vertices.Add(x); vertices.Add(0); vertices.Add(halfSize);
        }

        gridVAO = GL.GenVertexArray();
        gridVBO = GL.GenBuffer();

        GL.BindVertexArray(gridVAO);
        GL.BindBuffer(BufferTarget.ArrayBuffer, gridVBO);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Count * sizeof(float), vertices.ToArray(), BufferUsageHint.StaticDraw);

        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);

        GL.BindVertexArray(0);
    }

    private void CreateSpawnBlock()
    {
        // Create a simple cube to represent the spawn block below the decoration
        // This block is positioned at y = -1 (one block below the decoration grid at y=0)
        float[] vertices = {
            // Positions (3) + Colors (3) + Normals (3) + AO (1) + IsWater (1)
            // Front face (Z+)
            0, -1, 1,  0.4f, 0.7f, 0.3f,  0, 0, 1,  1, 0,
            1, -1, 1,  0.4f, 0.7f, 0.3f,  0, 0, 1,  1, 0,
            1, 0, 1,   0.4f, 0.7f, 0.3f,  0, 0, 1,  1, 0,
            0, 0, 1,   0.4f, 0.7f, 0.3f,  0, 0, 1,  1, 0,

            // Back face (Z-)
            1, -1, 0,  0.4f, 0.7f, 0.3f,  0, 0, -1,  1, 0,
            0, -1, 0,  0.4f, 0.7f, 0.3f,  0, 0, -1,  1, 0,
            0, 0, 0,   0.4f, 0.7f, 0.3f,  0, 0, -1,  1, 0,
            1, 0, 0,   0.4f, 0.7f, 0.3f,  0, 0, -1,  1, 0,

            // Left face (X-)
            0, -1, 0,  0.4f, 0.7f, 0.3f,  -1, 0, 0,  1, 0,
            0, -1, 1,  0.4f, 0.7f, 0.3f,  -1, 0, 0,  1, 0,
            0, 0, 1,   0.4f, 0.7f, 0.3f,  -1, 0, 0,  1, 0,
            0, 0, 0,   0.4f, 0.7f, 0.3f,  -1, 0, 0,  1, 0,

            // Right face (X+)
            1, -1, 1,  0.4f, 0.7f, 0.3f,  1, 0, 0,  1, 0,
            1, -1, 0,  0.4f, 0.7f, 0.3f,  1, 0, 0,  1, 0,
            1, 0, 0,   0.4f, 0.7f, 0.3f,  1, 0, 0,  1, 0,
            1, 0, 1,   0.4f, 0.7f, 0.3f,  1, 0, 0,  1, 0,

            // Top face (Y+)
            0, 0, 1,   0.4f, 0.7f, 0.3f,  0, 1, 0,  1, 0,
            1, 0, 1,   0.4f, 0.7f, 0.3f,  0, 1, 0,  1, 0,
            1, 0, 0,   0.4f, 0.7f, 0.3f,  0, 1, 0,  1, 0,
            0, 0, 0,   0.4f, 0.7f, 0.3f,  0, 1, 0,  1, 0,

            // Bottom face (Y-)
            0, -1, 0,  0.4f, 0.7f, 0.3f,  0, -1, 0,  1, 0,
            1, -1, 0,  0.4f, 0.7f, 0.3f,  0, -1, 0,  1, 0,
            1, -1, 1,  0.4f, 0.7f, 0.3f,  0, -1, 0,  1, 0,
            0, -1, 1,  0.4f, 0.7f, 0.3f,  0, -1, 0,  1, 0,
        };

        uint[] indices = {
            0, 1, 2, 2, 3, 0,       // Front
            4, 5, 6, 6, 7, 4,       // Back
            8, 9, 10, 10, 11, 8,    // Left
            12, 13, 14, 14, 15, 12, // Right
            16, 17, 18, 18, 19, 16, // Top
            20, 21, 22, 22, 23, 20  // Bottom
        };

        blockVAO = GL.GenVertexArray();
        blockVBO = GL.GenBuffer();
        blockEBO = GL.GenBuffer();

        GL.BindVertexArray(blockVAO);

        GL.BindBuffer(BufferTarget.ArrayBuffer, blockVBO);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

        GL.BindBuffer(BufferTarget.ElementArrayBuffer, blockEBO);
        GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);

        // Position attribute
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 11 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);

        // Color attribute
        GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 11 * sizeof(float), 3 * sizeof(float));
        GL.EnableVertexAttribArray(1);

        // Normal attribute
        GL.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, 11 * sizeof(float), 6 * sizeof(float));
        GL.EnableVertexAttribArray(2);

        // AO attribute
        GL.VertexAttribPointer(3, 1, VertexAttribPointerType.Float, false, 11 * sizeof(float), 9 * sizeof(float));
        GL.EnableVertexAttribArray(3);

        // IsWater attribute
        GL.VertexAttribIPointer(4, 1, (VertexAttribIntegerType)VertexAttribIPointerType.Int, 11 * sizeof(float), (IntPtr)(10 * sizeof(float)));
        GL.EnableVertexAttribArray(4);

        GL.BindVertexArray(0);
    }

    private void CreateMiniVoxelBuffer()
    {
        vao = GL.GenVertexArray();
        vbo = GL.GenBuffer();

        GL.BindVertexArray(vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);

        // Unlit shader format: Position (3) + Color (3) + Normal (3) + AO (1) + IsWater (1) = 11 floats
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 11 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);

        GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 11 * sizeof(float), 3 * sizeof(float));
        GL.EnableVertexAttribArray(1);

        GL.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, 11 * sizeof(float), 6 * sizeof(float));
        GL.EnableVertexAttribArray(2);

        GL.VertexAttribPointer(3, 1, VertexAttribPointerType.Float, false, 11 * sizeof(float), 9 * sizeof(float));
        GL.EnableVertexAttribArray(3);

        GL.VertexAttribIPointer(4, 1, (VertexAttribIntegerType)VertexAttribIPointerType.Int, 11 * sizeof(float), (IntPtr)(10 * sizeof(float)));
        GL.EnableVertexAttribArray(4);

        GL.BindVertexArray(0);
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        float deltaTime = (float)args.Time;
        var keyboard = KeyboardState;
        var io = ImGui.GetIO();

        imguiController?.Update(deltaTime);

        if (io.WantCaptureMouse || io.WantCaptureKeyboard)
        {
            if (mouseCaptured) ReleaseMouse();
            return;
        }

        // Blender-like controls
        // Middle mouse button for rotation
        if (MouseState.IsButtonDown(MouseButton.Middle) && !keyboard.IsKeyDown(Keys.LeftShift))
        {
            if (!isRotating)
            {
                isRotating = true;
                CursorState = CursorState.Grabbed;
            }
        }
        else if (isRotating && !MouseState.IsButtonDown(MouseButton.Middle))
        {
            isRotating = false;
            CursorState = CursorState.Normal;
        }

        // Shift + Middle mouse for panning
        if (MouseState.IsButtonDown(MouseButton.Middle) && keyboard.IsKeyDown(Keys.LeftShift))
        {
            if (!isPanning)
            {
                isPanning = true;
                isRotating = false;
                CursorState = CursorState.Grabbed;
            }
        }
        else if (isPanning && (!MouseState.IsButtonDown(MouseButton.Middle) || !keyboard.IsKeyDown(Keys.LeftShift)))
        {
            isPanning = false;
            CursorState = CursorState.Normal;
        }

        // Right click for voxel placement (traditional mode)
        if (MouseState.IsButtonPressed(MouseButton.Right) && !mouseCaptured)
        {
            CaptureMouse();
        }
        else if (keyboard.IsKeyPressed(Keys.Escape) && mouseCaptured)
        {
            ReleaseMouse();
        }

        // Traditional camera movement with WASD (when mouse captured)
        if (mouseCaptured)
        {
            float speed = keyboard.IsKeyDown(Keys.LeftShift) ? 5.0f : 2.0f;

            if (keyboard.IsKeyDown(Keys.W))
                camera.ProcessKeyboard(CameraMovement.Forward, deltaTime, speed);
            if (keyboard.IsKeyDown(Keys.S))
                camera.ProcessKeyboard(CameraMovement.Backward, deltaTime, speed);
            if (keyboard.IsKeyDown(Keys.A))
                camera.ProcessKeyboard(CameraMovement.Left, deltaTime, speed);
            if (keyboard.IsKeyDown(Keys.D))
                camera.ProcessKeyboard(CameraMovement.Right, deltaTime, speed);
            if (keyboard.IsKeyDown(Keys.Space))
                camera.ProcessKeyboard(CameraMovement.Up, deltaTime, speed);
            if (keyboard.IsKeyDown(Keys.LeftControl))
                camera.ProcessKeyboard(CameraMovement.Down, deltaTime, speed);

            // Mini-voxel editing
            if (MouseState.IsButtonPressed(MouseButton.Left))
            {
                PlaceMiniVoxel();
            }
            if (MouseState.IsButtonPressed(MouseButton.Right))
            {
                RemoveMiniVoxel();
            }
        }
    }

    protected override void OnMouseMove(MouseMoveEventArgs e)
    {
        base.OnMouseMove(e);

        if (firstMove)
        {
            lastMousePos = new Vector2(e.X, e.Y);
            firstMove = false;
            return;
        }

        float deltaX = e.X - lastMousePos.X;
        float deltaY = e.Y - lastMousePos.Y;

        // Blender-like orbit rotation with middle mouse
        if (isRotating)
        {
            camera.Yaw += deltaX * 0.3f;
            camera.Pitch -= deltaY * 0.3f;
            camera.Pitch = Math.Clamp(camera.Pitch, -89.0f, 89.0f);
            camera.Position = GetOrbitPosition();
            camera.UpdateCameraVectors();
        }
        // Blender-like panning with Shift + middle mouse
        else if (isPanning)
        {
            float panSpeed = 0.003f * orbitDistance;
            Vector3 right = camera.Right;
            Vector3 up = camera.Up;
            focusPoint -= right * deltaX * panSpeed;
            focusPoint += up * deltaY * panSpeed;
            camera.Position = GetOrbitPosition();
        }
        // Traditional FPS camera movement when captured
        else if (mouseCaptured)
        {
            camera.ProcessMouseMovement(deltaX, deltaY);
        }

        lastMousePos = new Vector2(e.X, e.Y);
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);

        // Blender-like zoom with scroll wheel
        float zoomSpeed = 0.3f;
        orbitDistance -= e.OffsetY * zoomSpeed;
        orbitDistance = Math.Clamp(orbitDistance, 1.0f, 20.0f);
        camera.Position = GetOrbitPosition();
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        Matrix4 view = camera.GetViewMatrix();
        Matrix4 projection = camera.GetProjectionMatrix();

        // Render spawn block (grass block below grid)
        if (blockShader != null)
        {
            blockShader.Use();
            blockShader.SetMatrix4("view", view);
            blockShader.SetMatrix4("projection", projection);
            blockShader.SetMatrix4("model", Matrix4.Identity);

            GL.BindVertexArray(blockVAO);
            GL.DrawElements(PrimitiveType.Triangles, 36, DrawElementsType.UnsignedInt, 0);
            GL.BindVertexArray(0);
        }

        // Render infinite grid
        if (gridShader != null)
        {
            GL.UseProgram(gridShader.Handle);
            GL.UniformMatrix4(GL.GetUniformLocation(gridShader.Handle, "view"), false, ref view);
            GL.UniformMatrix4(GL.GetUniformLocation(gridShader.Handle, "projection"), false, ref projection);
            Vector4 gridColor = new Vector4(0.3f, 0.3f, 0.3f, 0.6f);
            GL.Uniform4(GL.GetUniformLocation(gridShader.Handle, "color"), gridColor);

            GL.BindVertexArray(gridVAO);
            int lineCount = (InfiniteGridSize + 1) * 2; // Lines in both X and Z directions
            GL.DrawArrays(PrimitiveType.Lines, 0, lineCount * 2);
            GL.BindVertexArray(0);
        }

        // Render mini-voxels
        RenderMiniVoxels(view, projection);

        // Render UI
        RenderUI();
        imguiController?.Render();

        SwapBuffers();
    }

    private void RenderMiniVoxels(Matrix4 view, Matrix4 projection)
    {
        if (shader == null || currentDecoration.MiniVoxels.Count == 0)
            return;

        // Build vertex data for all mini-voxels with unlit shader format
        List<float> vertices = new();
        float cellSize = 1.0f / resolution;

        foreach (var miniVoxel in currentDecoration.MiniVoxels)
        {
            float x = miniVoxel.Position.X * cellSize;
            float y = miniVoxel.Position.Y * cellSize;
            float z = miniVoxel.Position.Z * cellSize;

            // Create a small cube for each mini-voxel using unlit shader format
            AddCubeUnlit(vertices, x, y, z, cellSize, miniVoxel.Color.ToVector3());
        }

        if (vertices.Count == 0)
            return;

        // Upload to GPU
        GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Count * sizeof(float), vertices.ToArray(), BufferUsageHint.DynamicDraw);

        shader.Use();
        shader.SetMatrix4("view", view);
        shader.SetMatrix4("projection", projection);
        shader.SetMatrix4("model", Matrix4.Identity);

        GL.BindVertexArray(vao);
        GL.DrawArrays(PrimitiveType.Triangles, 0, vertices.Count / 11);
        GL.BindVertexArray(0);
    }

    private void AddCubeUnlit(List<float> vertices, float x, float y, float z, float size, Vector3 color)
    {
        // Cube with 6 faces using unlit shader format (Position + Color + Normal + AO + IsWater)
        // Front face (Z+)
        AddQuadUnlit(vertices,
            new Vector3(x, y, z + size), new Vector3(x + size, y, z + size),
            new Vector3(x + size, y + size, z + size), new Vector3(x, y + size, z + size),
            color, new Vector3(0, 0, 1));

        // Back face (Z-)
        AddQuadUnlit(vertices,
            new Vector3(x + size, y, z), new Vector3(x, y, z),
            new Vector3(x, y + size, z), new Vector3(x + size, y + size, z),
            color, new Vector3(0, 0, -1));

        // Left face (X-)
        AddQuadUnlit(vertices,
            new Vector3(x, y, z), new Vector3(x, y, z + size),
            new Vector3(x, y + size, z + size), new Vector3(x, y + size, z),
            color, new Vector3(-1, 0, 0));

        // Right face (X+)
        AddQuadUnlit(vertices,
            new Vector3(x + size, y, z + size), new Vector3(x + size, y, z),
            new Vector3(x + size, y + size, z), new Vector3(x + size, y + size, z + size),
            color, new Vector3(1, 0, 0));

        // Top face (Y+)
        AddQuadUnlit(vertices,
            new Vector3(x, y + size, z + size), new Vector3(x + size, y + size, z + size),
            new Vector3(x + size, y + size, z), new Vector3(x, y + size, z),
            color, new Vector3(0, 1, 0));

        // Bottom face (Y-)
        AddQuadUnlit(vertices,
            new Vector3(x, y, z), new Vector3(x + size, y, z),
            new Vector3(x + size, y, z + size), new Vector3(x, y, z + size),
            color, new Vector3(0, -1, 0));
    }

    private void AddQuadUnlit(List<float> vertices, Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3, Vector3 color, Vector3 normal)
    {
        // Format: Position (3) + Color (3) + Normal (3) + AO (1) + IsWater (1) = 11 floats per vertex
        void AddVertex(Vector3 pos)
        {
            vertices.Add(pos.X); vertices.Add(pos.Y); vertices.Add(pos.Z); // Position
            vertices.Add(color.X); vertices.Add(color.Y); vertices.Add(color.Z); // Color
            vertices.Add(normal.X); vertices.Add(normal.Y); vertices.Add(normal.Z); // Normal
            vertices.Add(1.0f); // AO
            vertices.Add(0.0f); // IsWater
        }

        // Triangle 1
        AddVertex(v0);
        AddVertex(v1);
        AddVertex(v2);

        // Triangle 2
        AddVertex(v0);
        AddVertex(v2);
        AddVertex(v3);
    }

    private void RenderUI()
    {
        ImGui.Begin("Decoration Editor", ImGuiWindowFlags.AlwaysAutoResize);

        ImGui.Text($"Camera: {camera.Position:F1}");
        ImGui.Text($"Mini-Voxels: {currentDecoration.MiniVoxels.Count}");
        ImGui.Text($"Resolution: {resolution}x{resolution}x{resolution}");
        ImGui.Separator();

        // Load existing decoration
        ImGui.Text("Load Existing Decoration:");
        if (availableDecorations.Count > 0)
        {
            string[] decorationNames = availableDecorations.Select(d => d.Name).ToArray();
            int currentIndex = selectedLoadDecorationIndex;
            if (ImGui.Combo("##LoadDecoration", ref currentIndex, decorationNames, decorationNames.Length))
            {
                selectedLoadDecorationIndex = currentIndex;
            }
            ImGui.SameLine();
            if (ImGui.Button("Load"))
            {
                LoadDecoration(selectedLoadDecorationIndex);
            }
        }
        else
        {
            ImGui.Text("No decorations found");
        }
        ImGui.Separator();

        // Decoration info
        byte[] nameBuffer = System.Text.Encoding.UTF8.GetBytes(decorationName);
        Array.Resize(ref nameBuffer, 256);
        if (ImGui.InputText("Name", nameBuffer, 256))
        {
            decorationName = System.Text.Encoding.UTF8.GetString(nameBuffer).TrimEnd('\0');
        }

        ImGui.Separator();

        // Color picker
        System.Numerics.Vector3 colorPicker = new System.Numerics.Vector3(selectedColor.R, selectedColor.G, selectedColor.B);
        if (ImGui.ColorEdit3("Mini-Voxel Color", ref colorPicker))
        {
            selectedColor = new Color(colorPicker.X, colorPicker.Y, colorPicker.Z);
        }

        ImGui.Separator();

        // Biome settings
        ImGui.Text("Allowed Biomes (none = all):");
        string[] biomeNames = Enum.GetNames(typeof(BiomeType));
        for (int i = 0; i < biomesEnabled.Length; i++)
        {
            ImGui.Checkbox(biomeNames[i], ref biomesEnabled[i]);
        }

        ImGui.Separator();

        // Ground block requirements
        ImGui.Text("Required Ground Blocks (none = any solid):");
        string[] voxelNames = new[] { "Grass", "Dirt", "Stone", "Wood", "Leaves", "Sand", "Brick", "Glass" };
        for (int i = 0; i < groundBlocksEnabled.Length; i++)
        {
            if (i < voxelNames.Length)
            {
                ImGui.Checkbox(voxelNames[i], ref groundBlocksEnabled[i]);
            }
        }

        ImGui.Separator();

        // Density
        ImGui.Text("Density:");
        ImGui.SliderFloat("##Density", ref density, 0.0f, 1.0f, $"{density:F3}");
        ImGui.Text($"({density * 100:F1}% spawn chance)");

        ImGui.Separator();

        // Save button
        if (ImGui.Button("Save Decoration", new System.Numerics.Vector2(200, 30)))
        {
            SaveDecoration();
        }

        if (ImGui.Button("Clear All", new System.Numerics.Vector2(200, 30)))
        {
            currentDecoration.MiniVoxels.Clear();
        }

        ImGui.Separator();
        ImGui.Text("Blender-like Controls:");
        ImGui.BulletText("Middle Mouse: Orbit camera");
        ImGui.BulletText("Scroll Wheel: Zoom in/out");
        ImGui.BulletText("Shift + Middle Mouse: Pan camera");
        ImGui.Separator();
        ImGui.Text("Traditional Controls:");
        ImGui.BulletText("Right Click: Capture mouse (FPS mode)");
        ImGui.BulletText("Left Click (FPS): Place mini-voxel");
        ImGui.BulletText("Right Click (FPS): Remove mini-voxel");
        ImGui.BulletText("WASD (FPS): Move camera");
        ImGui.BulletText("ESC: Release mouse");

        ImGui.End();
    }

    private void PlaceMiniVoxel()
    {
        var miniVoxelPos = GetMiniVoxelLookingAt(out bool hit);
        if (!hit) return;

        currentDecoration.AddMiniVoxel(miniVoxelPos, selectedColor);
    }

    private void RemoveMiniVoxel()
    {
        var miniVoxelPos = GetMiniVoxelLookingAt(out bool hit);
        if (!hit) return;

        currentDecoration.RemoveMiniVoxel(miniVoxelPos);
    }

    private Vector3Int GetMiniVoxelLookingAt(out bool hit)
    {
        Vector3 rayStart = camera.Position;
        Vector3 rayDir = camera.Front.Normalized();

        float maxDistance = 10.0f;
        float cellSize = 1.0f / resolution;
        float step = cellSize * 0.5f;

        for (float dist = 0; dist < maxDistance; dist += step)
        {
            Vector3 checkPos = rayStart + rayDir * dist;

            // Convert to mini-voxel coordinates
            int mx = (int)MathF.Floor(checkPos.X / cellSize);
            int my = (int)MathF.Floor(checkPos.Y / cellSize);
            int mz = (int)MathF.Floor(checkPos.Z / cellSize);

            // Check if within bounds
            if (mx >= 0 && mx < resolution && my >= 0 && my < resolution && mz >= 0 && mz < resolution)
            {
                hit = true;
                return new Vector3Int(mx, my, mz);
            }
        }

        hit = false;
        return Vector3Int.Zero;
    }

    private void SaveDecoration()
    {
        currentDecoration.Name = decorationName;
        currentDecoration.Resolution = resolution;
        currentDecoration.Density = density;

        // Update biomes
        currentDecoration.AllowedBiomes.Clear();
        for (int i = 0; i < biomesEnabled.Length; i++)
        {
            if (biomesEnabled[i])
            {
                currentDecoration.AllowedBiomes.Add((BiomeType)i);
            }
        }

        // Update ground blocks
        currentDecoration.RequiredGroundBlocks.Clear();
        for (int i = 0; i < groundBlocksEnabled.Length; i++)
        {
            if (groundBlocksEnabled[i])
            {
                var voxelType = (VoxelType)(i + 1); // Skip Air
                currentDecoration.RequiredGroundBlocks.Add(voxelType);
            }
        }

        var manager = new DecorationManager();
        manager.SaveDecoration(currentDecoration);

        Console.WriteLine($"Decoration '{decorationName}' saved successfully!");

        LoadAvailableDecorations();
    }

    private void LoadAvailableDecorations()
    {
        availableDecorations.Clear();
        var manager = new DecorationManager();
        availableDecorations.AddRange(manager.Decorations);
    }

    private void LoadDecoration(int index)
    {
        if (index < 0 || index >= availableDecorations.Count)
            return;

        currentDecoration = availableDecorations[index];
        decorationName = currentDecoration.Name;
        resolution = currentDecoration.Resolution;
        density = currentDecoration.Density;

        // Update biome checkboxes
        for (int i = 0; i < biomesEnabled.Length; i++)
        {
            biomesEnabled[i] = currentDecoration.AllowedBiomes.Contains((BiomeType)i);
        }

        // Update ground block checkboxes
        for (int i = 0; i < groundBlocksEnabled.Length; i++)
        {
            var voxelType = (VoxelType)(i + 1);
            groundBlocksEnabled[i] = currentDecoration.RequiredGroundBlocks.Contains(voxelType);
        }

        // Recreate infinite grid (resolution doesn't affect infinite grid size)
        CreateInfiniteGrid();

        Console.WriteLine($"Loaded decoration '{currentDecoration.Name}'");
    }

    private void CaptureMouse()
    {
        CursorState = CursorState.Grabbed;
        mouseCaptured = true;
    }

    private void ReleaseMouse()
    {
        CursorState = CursorState.Normal;
        mouseCaptured = false;
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        GL.Viewport(0, 0, e.Width, e.Height);
        camera.AspectRatio = e.Width / (float)e.Height;
    }

    protected override void OnUnload()
    {
        base.OnUnload();
        shader?.Dispose();
        gridShader?.Dispose();
        blockShader?.Dispose();
        imguiController?.Dispose();
        GL.DeleteVertexArray(vao);
        GL.DeleteBuffer(vbo);
        GL.DeleteVertexArray(gridVAO);
        GL.DeleteBuffer(gridVBO);
        GL.DeleteVertexArray(blockVAO);
        GL.DeleteBuffer(blockVBO);
        GL.DeleteBuffer(blockEBO);
    }
}
