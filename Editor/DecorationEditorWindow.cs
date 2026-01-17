using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using VoxelEngine.Core;
using VoxelEngine.Decorations;
using VoxelEngine.World;
using ImGuiNET;

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
    private ImGuiController? imguiController;

    private int vao, vbo;
    private int gridVAO, gridVBO;
    private const int GridSize = 1; // Only 1 block

    private Vector3 selectedColor = new Vector3(0.3f, 0.7f, 0.2f); // Default grass color
    private bool mouseCaptured = false;
    private bool firstMove = true;
    private Vector2 lastMousePos;

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

        // Initialize camera closer to the mini-voxel grid
        camera = new Camera(new Vector3(2, 2, 3), Size.X / (float)Size.Y);
        camera.Pitch = -20;

        // Load simple shader for mini-voxels
        CreateMiniVoxelShader();
        CreateGridShader();

        // Initialize ImGui
        imguiController = new ImGuiController(this);

        // Create grid and voxel buffers
        CreateGrid();
        CreateMiniVoxelBuffer();

        // Load available decorations
        LoadAvailableDecorations();
    }

    private void CreateMiniVoxelShader()
    {
        string vertexSource = @"#version 330 core
layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec3 aColor;
out vec3 fragColor;
uniform mat4 view;
uniform mat4 projection;
uniform mat4 model;
void main()
{
    fragColor = aColor;
    gl_Position = projection * view * model * vec4(aPosition, 1.0);
}";

        string fragmentSource = @"#version 330 core
in vec3 fragColor;
out vec4 FragColor;
void main()
{
    FragColor = vec4(fragColor, 1.0);
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

        shader = new Shader(program);
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

    private void CreateGrid()
    {
        List<float> vertices = new();
        float cellSize = 1.0f / resolution;

        // Create grid lines for the mini-voxel grid
        for (int i = 0; i <= resolution; i++)
        {
            float pos = i * cellSize;

            // Lines along X axis
            vertices.Add(pos); vertices.Add(0); vertices.Add(0);
            vertices.Add(pos); vertices.Add(0); vertices.Add(1);

            vertices.Add(pos); vertices.Add(0); vertices.Add(0);
            vertices.Add(pos); vertices.Add(1); vertices.Add(0);

            // Lines along Z axis
            vertices.Add(0); vertices.Add(0); vertices.Add(pos);
            vertices.Add(1); vertices.Add(0); vertices.Add(pos);

            vertices.Add(0); vertices.Add(0); vertices.Add(pos);
            vertices.Add(0); vertices.Add(1); vertices.Add(pos);

            // Lines along Y axis
            vertices.Add(0); vertices.Add(pos); vertices.Add(0);
            vertices.Add(1); vertices.Add(pos); vertices.Add(0);

            vertices.Add(0); vertices.Add(pos); vertices.Add(0);
            vertices.Add(0); vertices.Add(pos); vertices.Add(1);
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

    private void CreateMiniVoxelBuffer()
    {
        vao = GL.GenVertexArray();
        vbo = GL.GenBuffer();

        GL.BindVertexArray(vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);

        // Position and color attributes
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 3 * sizeof(float));
        GL.EnableVertexAttribArray(1);

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

        // Toggle mouse capture
        if (MouseState.IsButtonPressed(MouseButton.Right) && !mouseCaptured)
        {
            CaptureMouse();
        }
        else if (keyboard.IsKeyPressed(Keys.Escape) && mouseCaptured)
        {
            ReleaseMouse();
        }

        // Camera movement
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
            if (MouseState.IsButtonPressed(MouseButton.Right))
            {
                PlaceMiniVoxel();
            }
            if (MouseState.IsButtonPressed(MouseButton.Left))
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

        if (mouseCaptured)
        {
            float deltaX = e.X - lastMousePos.X;
            float deltaY = e.Y - lastMousePos.Y;
            camera.ProcessMouseMovement(deltaX, deltaY);
        }

        lastMousePos = new Vector2(e.X, e.Y);
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        Matrix4 view = camera.GetViewMatrix();
        Matrix4 projection = camera.GetProjectionMatrix();

        // Render grid
        if (gridShader != null)
        {
            GL.UseProgram(gridShader.Handle);
            GL.UniformMatrix4(GL.GetUniformLocation(gridShader.Handle, "view"), false, ref view);
            GL.UniformMatrix4(GL.GetUniformLocation(gridShader.Handle, "projection"), false, ref projection);
            Vector4 gridColor = new Vector4(0.4f, 0.4f, 0.4f, 1.0f);
            GL.Uniform4(GL.GetUniformLocation(gridShader.Handle, "color"), gridColor);

            GL.BindVertexArray(gridVAO);
            GL.DrawArrays(PrimitiveType.Lines, 0, (resolution + 1) * 12);
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

        // Build vertex data for all mini-voxels
        List<float> vertices = new();
        float cellSize = 1.0f / resolution;

        foreach (var miniVoxel in currentDecoration.MiniVoxels)
        {
            float x = miniVoxel.Position.X * cellSize;
            float y = miniVoxel.Position.Y * cellSize;
            float z = miniVoxel.Position.Z * cellSize;

            // Create a small cube for each mini-voxel
            AddCube(vertices, x, y, z, cellSize, miniVoxel.Color);
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
        GL.DrawArrays(PrimitiveType.Triangles, 0, vertices.Count / 6);
        GL.BindVertexArray(0);
    }

    private void AddCube(List<float> vertices, float x, float y, float z, float size, Vector3 color)
    {
        // Simple cube with 6 faces
        // Front face
        AddQuad(vertices,
            new Vector3(x, y, z + size), new Vector3(x + size, y, z + size),
            new Vector3(x + size, y + size, z + size), new Vector3(x, y + size, z + size), color);

        // Back face
        AddQuad(vertices,
            new Vector3(x + size, y, z), new Vector3(x, y, z),
            new Vector3(x, y + size, z), new Vector3(x + size, y + size, z), color);

        // Left face
        AddQuad(vertices,
            new Vector3(x, y, z), new Vector3(x, y, z + size),
            new Vector3(x, y + size, z + size), new Vector3(x, y + size, z), color);

        // Right face
        AddQuad(vertices,
            new Vector3(x + size, y, z + size), new Vector3(x + size, y, z),
            new Vector3(x + size, y + size, z), new Vector3(x + size, y + size, z + size), color);

        // Top face
        AddQuad(vertices,
            new Vector3(x, y + size, z + size), new Vector3(x + size, y + size, z + size),
            new Vector3(x + size, y + size, z), new Vector3(x, y + size, z), color);

        // Bottom face
        AddQuad(vertices,
            new Vector3(x, y, z), new Vector3(x + size, y, z),
            new Vector3(x + size, y, z + size), new Vector3(x, y, z + size), color);
    }

    private void AddQuad(List<float> vertices, Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3, Vector3 color)
    {
        // Triangle 1
        vertices.Add(v0.X); vertices.Add(v0.Y); vertices.Add(v0.Z);
        vertices.Add(color.X); vertices.Add(color.Y); vertices.Add(color.Z);

        vertices.Add(v1.X); vertices.Add(v1.Y); vertices.Add(v1.Z);
        vertices.Add(color.X); vertices.Add(color.Y); vertices.Add(color.Z);

        vertices.Add(v2.X); vertices.Add(v2.Y); vertices.Add(v2.Z);
        vertices.Add(color.X); vertices.Add(color.Y); vertices.Add(color.Z);

        // Triangle 2
        vertices.Add(v0.X); vertices.Add(v0.Y); vertices.Add(v0.Z);
        vertices.Add(color.X); vertices.Add(color.Y); vertices.Add(color.Z);

        vertices.Add(v2.X); vertices.Add(v2.Y); vertices.Add(v2.Z);
        vertices.Add(color.X); vertices.Add(color.Y); vertices.Add(color.Z);

        vertices.Add(v3.X); vertices.Add(v3.Y); vertices.Add(v3.Z);
        vertices.Add(color.X); vertices.Add(color.Y); vertices.Add(color.Z);
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
        System.Numerics.Vector3 colorPicker = new System.Numerics.Vector3(selectedColor.X, selectedColor.Y, selectedColor.Z);
        if (ImGui.ColorEdit3("Mini-Voxel Color", ref colorPicker))
        {
            selectedColor = new Vector3(colorPicker.X, colorPicker.Y, colorPicker.Z);
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
        ImGui.Text("Controls:");
        ImGui.BulletText("Right Click: Capture mouse");
        ImGui.BulletText("Right Click (captured): Place mini-voxel");
        ImGui.BulletText("Left Click (captured): Remove mini-voxel");
        ImGui.BulletText("WASD: Move camera");
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

        // Recreate grid with new resolution
        CreateGrid();

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
        imguiController?.Dispose();
        GL.DeleteVertexArray(vao);
        GL.DeleteBuffer(vbo);
        GL.DeleteVertexArray(gridVAO);
        GL.DeleteBuffer(gridVBO);
    }
}
