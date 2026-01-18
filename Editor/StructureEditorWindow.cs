using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using VoxelEngine.Core;
using VoxelEngine.Graphics;
using VoxelEngine.Structures;
using VoxelEngine.World;
using ImGuiNET;
using MathHelper = OpenTK.Mathematics.MathHelper;

namespace VoxelEngine.Editor;

public class StructureEditorWindow : GameWindow
{
    private Structure currentStructure;
    private Camera camera;
    private Shader? voxelShader;
    private Shader? gridShader;
    private VoxelMesh? structureMesh;
    private ImGuiController? imguiController;

    private int gridVAO, gridVBO;
    private const int GridSize = 100; // Large grid for infinite feel

    private VoxelType selectedVoxelType = VoxelType.Grass;
    private bool mouseCaptured = false;
    private bool firstMove = true;
    private Vector2 lastMousePos;

    // Blender-like controls
    private bool isRotating = false;
    private bool isPanning = false;
    private Vector3 focusPoint = Vector3.Zero; // Center at origin
    private float orbitDistance = 30.0f;

    private string structureName = "NewStructure";
    private StructureCategory structureCategory = StructureCategory.Architecture;
    private float spawnFrequency = 0.05f;
    private bool[] biomesEnabled = new bool[5]; // One for each BiomeType
    private List<Structure> availableStructures = new();
    private int selectedLoadStructureIndex = -1;

    public StructureEditorWindow(Structure? existingStructure = null)
        : base(GameWindowSettings.Default, new NativeWindowSettings()
        {
            Size = new Vector2i(1280, 720),
            Title = "Structure Editor",
            StartVisible = true,
            WindowBorder = WindowBorder.Resizable,
            API = ContextAPI.OpenGL,
            APIVersion = new Version(3, 3),
            Profile = ContextProfile.Core
        })
    {
        currentStructure = existingStructure ?? new Structure("NewStructure", StructureCategory.Architecture);
        structureName = currentStructure.Name;
        structureCategory = currentStructure.Category;
        spawnFrequency = currentStructure.SpawnFrequency;

        // Initialize biome checkboxes based on structure's allowed biomes
        for (int i = 0; i < biomesEnabled.Length; i++)
        {
            biomesEnabled[i] = currentStructure.AllowedBiomes.Contains((BiomeType)i);
        }

        VSync = VSyncMode.On;
    }

    protected override void OnLoad()
    {
        base.OnLoad();

        GL.ClearColor(0.2f, 0.2f, 0.2f, 1.0f);
        GL.Enable(EnableCap.DepthTest);
        GL.Enable(EnableCap.CullFace);
        GL.CullFace(CullFaceMode.Back);

        // Initialize camera in orbit around origin
        camera = new Camera(GetOrbitPosition(), Size.X / (float)Size.Y);
        camera.Pitch = -30;
        camera.Yaw = -45;

        // Load unlit shaders for editor
        voxelShader = new Shader("Shaders/voxel_unlit.vert", "Shaders/voxel_unlit.frag");
        CreateGridShader();

        // Initialize ImGui
        imguiController = new ImGuiController(this);

        // Create grid
        CreateGrid();

        // Load available structures
        LoadAvailableStructures();

        // Build initial mesh
        RebuildMesh();
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

        // Compile vertex shader
        int vertexShader = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(vertexShader, vertexSource);
        GL.CompileShader(vertexShader);

        // Compile fragment shader
        int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(fragmentShader, fragmentSource);
        GL.CompileShader(fragmentShader);

        // Link program
        int program = GL.CreateProgram();
        GL.AttachShader(program, vertexShader);
        GL.AttachShader(program, fragmentShader);
        GL.LinkProgram(program);

        // Cleanup shader objects
        GL.DetachShader(program, vertexShader);
        GL.DetachShader(program, fragmentShader);
        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);

        // ✅ Correct usage: wrap program in Shader
        gridShader = new Shader(program);
    }


    private void CreateGrid()
    {
        List<float> vertices = new();

        // Create grid centered at origin for infinite feel
        int halfSize = GridSize / 2;

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

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        float deltaTime = (float)args.Time;
        var keyboard = KeyboardState;
        var io = ImGui.GetIO();

        imguiController?.Update(deltaTime);

        // Don't process input if ImGui wants it
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

        // Mouse-based voxel placement (Blender-like)
        if (!isRotating && !isPanning && !mouseCaptured)
        {
            if (MouseState.IsButtonPressed(MouseButton.Left))
            {
                PlaceVoxel();
            }
            if (MouseState.IsButtonPressed(MouseButton.Right))
            {
                RemoveVoxel();
            }
        }

        // Optional: Right click for traditional camera capture mode
        if (keyboard.IsKeyDown(Keys.LeftControl) && MouseState.IsButtonPressed(MouseButton.Right) && !mouseCaptured)
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
            float speed = keyboard.IsKeyDown(Keys.LeftShift) ? 15.0f : 7.0f;

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
            float panSpeed = 0.01f * orbitDistance;
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

        var io = ImGui.GetIO();
        if (io.WantCaptureMouse)
            return;

        // Blender-like zoom with scroll wheel
        float zoomSpeed = 2.0f;
        orbitDistance -= e.OffsetY * zoomSpeed;
        orbitDistance = Math.Clamp(orbitDistance, 5.0f, 100.0f);
        camera.Position = GetOrbitPosition();
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
            Vector4 gridColor = new Vector4(0.3f, 0.3f, 0.3f, 1.0f);
            GL.Uniform4(GL.GetUniformLocation(gridShader.Handle, "color"), gridColor);

            GL.BindVertexArray(gridVAO);
            GL.DrawArrays(PrimitiveType.Lines, 0, (GridSize + 1) * 4);
            GL.BindVertexArray(0);
        }

        // Render structure (using unlit shader, no lighting uniforms needed)
        if (voxelShader != null && structureMesh != null)
        {
            voxelShader.Use();
            voxelShader.SetMatrix4("view", view);
            voxelShader.SetMatrix4("projection", projection);
            voxelShader.SetMatrix4("model", Matrix4.Identity);

            structureMesh.Render();
        }

        // Render UI
        RenderUI();
        imguiController?.Render();

        SwapBuffers();
    }

    private void RenderUI()
    {
        ImGui.Begin("Structure Editor", ImGuiWindowFlags.AlwaysAutoResize);

        ImGui.Text($"Camera: {camera.Position:F1}");
        ImGui.Text($"Structure Size: {currentStructure.Size}");
        ImGui.Text($"Voxels: {currentStructure.Voxels.Count}");
        ImGui.Separator();

        // Load existing structure
        ImGui.Text("Load Existing Structure:");
        if (availableStructures.Count > 0)
        {
            string[] structureNames = availableStructures.Select(s => $"{s.Category}_{s.Name}").ToArray();
            int currentIndex = selectedLoadStructureIndex;
            if (ImGui.Combo("##LoadStructure", ref currentIndex, structureNames, structureNames.Length))
            {
                selectedLoadStructureIndex = currentIndex;
            }
            ImGui.SameLine();
            if (ImGui.Button("Load"))
            {
                LoadStructure(selectedLoadStructureIndex);
            }
        }
        else
        {
            ImGui.Text("No structures found");
        }
        ImGui.Separator();

        // Structure info
        byte[] nameBuffer = System.Text.Encoding.UTF8.GetBytes(structureName);
        Array.Resize(ref nameBuffer, 256);
        if (ImGui.InputText("Name", nameBuffer, 256))
        {
            structureName = System.Text.Encoding.UTF8.GetString(nameBuffer).TrimEnd('\0');
        }

        string[] categories = { "Architecture", "Ambient" };
        int categoryIndex = (int)structureCategory;
        if (ImGui.Combo("Category", ref categoryIndex, categories, categories.Length))
        {
            structureCategory = (StructureCategory)categoryIndex;
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

        // Spawn frequency
        ImGui.Text("Spawn Frequency:");
        ImGui.SliderFloat("##SpawnFrequency", ref spawnFrequency, 0.0f, 1.0f, $"{spawnFrequency:F3}");
        ImGui.Text($"({spawnFrequency * 100:F1}% chance per location)");

        ImGui.Separator();

        // Voxel type selector
        string[] voxelNames = Enum.GetNames(typeof(VoxelType));
        int currentType = (int)selectedVoxelType;
        if (ImGui.Combo("Voxel Type", ref currentType, voxelNames, voxelNames.Length))
        {
            selectedVoxelType = (VoxelType)currentType;
        }

        ImGui.Separator();

        // Save button
        if (ImGui.Button("Save Structure", new System.Numerics.Vector2(200, 30)))
        {
            SaveStructure();
        }

        if (ImGui.Button("Clear All", new System.Numerics.Vector2(200, 30)))
        {
            currentStructure.Voxels.Clear();
            RebuildMesh();
        }

        ImGui.Separator();
        ImGui.Text("Blender-like Controls:");
        ImGui.BulletText("Middle Mouse: Orbit camera");
        ImGui.BulletText("Scroll Wheel: Zoom in/out");
        ImGui.BulletText("Shift + Middle Mouse: Pan camera");
        ImGui.BulletText("Left Click: Place voxel");
        ImGui.BulletText("Right Click: Remove voxel");
        ImGui.Separator();
        ImGui.Text("Traditional Controls:");
        ImGui.BulletText("Ctrl + Right Click: FPS mode");
        ImGui.BulletText("WASD (FPS): Move camera");
        ImGui.BulletText("Space/Ctrl (FPS): Up/Down");
        ImGui.BulletText("ESC: Release mouse");

        ImGui.End();
    }

    private void PlaceVoxel()
    {
        var hitVoxel = GetVoxelLookingAt(out bool hit, out Vector3Int placePos);
        if (!hit) return;

        currentStructure.AddVoxel(placePos, selectedVoxelType);
        RebuildMesh();
    }

    private void RemoveVoxel()
    {
        var hitVoxel = GetVoxelLookingAt(out bool hit, out _);
        if (!hit) return;

        currentStructure.RemoveVoxel(hitVoxel);
        RebuildMesh();
    }

    private Vector3Int GetVoxelLookingAt(out bool hit, out Vector3Int placePos)
    {
        Vector3 rayStart = camera.Position;
        Vector3 rayDir = camera.Front.Normalized();

        float maxDistance = 50.0f;
        float step = 0.1f;

        Vector3Int lastEmpty = Vector3Int.Zero;

        for (float dist = 0; dist < maxDistance; dist += step)
        {
            Vector3 checkPos = rayStart + rayDir * dist;

            Vector3Int voxelPos = new Vector3Int(
                (int)MathF.Floor(checkPos.X),
                (int)MathF.Floor(checkPos.Y),
                (int)MathF.Floor(checkPos.Z)
            );

            var voxelType = currentStructure.GetVoxel(voxelPos);

            if (voxelType != VoxelType.Air)
            {
                hit = true;
                placePos = lastEmpty;
                return voxelPos;
            }

            lastEmpty = voxelPos;
        }

        // ---- grid placement (Y = 0) ----
        if (MathF.Abs(rayDir.Y) > 0.0001f)
        {
            float t = -rayStart.Y / rayDir.Y;
            if (t > 0 && t < maxDistance)
            {
                Vector3 hitPos = rayStart + rayDir * t;

                hit = true;
                placePos = new Vector3Int(
                    (int)MathF.Floor(hitPos.X),
                    0,
                    (int)MathF.Floor(hitPos.Z)
                );

                return placePos;
            }
        }

        hit = false;
        placePos = Vector3Int.Zero;
        return Vector3Int.Zero;
    }

    private void RebuildMesh()
    {
        structureMesh?.Dispose();

        // Create temporary chunk for mesh building
        var tempChunk = new Chunk(Vector3Int.Zero);

        foreach (var voxelData in currentStructure.Voxels)
        {
            int x = voxelData.Position.X;
            int y = voxelData.Position.Y;
            int z = voxelData.Position.Z;

            if (x >= 0 && x < Chunk.ChunkSize && y >= 0 && y < Chunk.ChunkSize && z >= 0 && z < Chunk.ChunkSize)
            {
                tempChunk.SetVoxel(x, y, z, new Voxel(voxelData.Type, true));
            }
        }

        // Create temporary world for neighbor checking
        var tempWorld = new VoxelWorld(new Vector3Int(1, 1, 1));
        var worldChunk = tempWorld.GetChunk(Vector3Int.Zero);
        if (worldChunk != null)
        {
            foreach (var voxelData in currentStructure.Voxels)
            {
                int x = voxelData.Position.X;
                int y = voxelData.Position.Y;
                int z = voxelData.Position.Z;
                if (x >= 0 && x < Chunk.ChunkSize && y >= 0 && y < Chunk.ChunkSize && z >= 0 && z < Chunk.ChunkSize)
                {
                    worldChunk.SetVoxel(x, y, z, new Voxel(voxelData.Type, true));
                }
            }
        }

        structureMesh = new VoxelMesh();
        structureMesh.BuildMesh(tempChunk, tempWorld);
    }

    private void SaveStructure()
    {
        currentStructure.Name = structureName;
        currentStructure.Category = structureCategory;
        currentStructure.SpawnFrequency = spawnFrequency;

        // Update allowed biomes based on checkboxes
        currentStructure.AllowedBiomes.Clear();
        for (int i = 0; i < biomesEnabled.Length; i++)
        {
            if (biomesEnabled[i])
            {
                currentStructure.AllowedBiomes.Add((BiomeType)i);
            }
        }

        var manager = new StructureManager();
        manager.SaveStructure(currentStructure);

        Console.WriteLine($"Structure '{structureName}' saved successfully!");

        // Reload available structures
        LoadAvailableStructures();
    }

    private void LoadAvailableStructures()
    {
        availableStructures.Clear();
        var manager = new StructureManager();

        // Load all structures from both categories
        availableStructures.AddRange(manager.ArchitectureStructures);
        availableStructures.AddRange(manager.AmbientStructures);
    }

    private void LoadStructure(int index)
    {
        if (index < 0 || index >= availableStructures.Count)
            return;

        currentStructure = availableStructures[index];
        structureName = currentStructure.Name;
        structureCategory = currentStructure.Category;
        spawnFrequency = currentStructure.SpawnFrequency;

        // Update biome checkboxes
        for (int i = 0; i < biomesEnabled.Length; i++)
        {
            biomesEnabled[i] = currentStructure.AllowedBiomes.Contains((BiomeType)i);
        }

        RebuildMesh();
        Console.WriteLine($"Loaded structure '{currentStructure.Name}'");
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
        voxelShader?.Dispose();
        structureMesh?.Dispose();
        imguiController?.Dispose();
        GL.DeleteVertexArray(gridVAO);
        GL.DeleteBuffer(gridVBO);
    }
}
