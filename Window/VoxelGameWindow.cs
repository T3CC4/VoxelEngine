using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using VoxelEngine.Core;
using VoxelEngine.Editor;
using VoxelEngine.Game;
using VoxelEngine.Graphics;
using VoxelEngine.Structures;
using ImGuiNET;

namespace VoxelEngine.Window;

public class VoxelGameWindow : GameWindow
{
    private readonly bool isEditorMode;
    private VoxelWorld world;
    private Camera camera;
    private Shader? voxelShader;
    private Dictionary<Vector3Int, VoxelMesh> chunkMeshes = new();
    private StructureManager structureManager;
    private ImGuiController? imguiController;

    // Game mode
    private PlayerController? player;

    // Editor mode
    private Vector3Int editorCursorPos = Vector3Int.Zero;
    private VoxelType selectedVoxelType = VoxelType.Grass;
    private bool isPlaying = false;
    private Structure? currentStructure = null;
    private StructureCategory selectedCategory = StructureCategory.Architecture;
    private int selectedStructureIndex = 0;

    // Input
    private bool firstMove = true;
    private Vector2 lastMousePos;
    private bool mouseCaptured = false;

    public VoxelGameWindow(bool editorMode)
        : base(GameWindowSettings.Default, new NativeWindowSettings()
        {
            Size = new Vector2i(1600, 900),
            Title = editorMode ? "VoxelEngine - Editor Mode" : "VoxelEngine - Game Mode",
            StartVisible = true,
            WindowBorder = WindowBorder.Resizable,
            API = ContextAPI.OpenGL,
            APIVersion = new Version(3, 3),
            Profile = ContextProfile.Core
        })
    {
        isEditorMode = editorMode;
        VSync = VSyncMode.On;
    }

    protected override void OnLoad()
    {
        base.OnLoad();

        GL.ClearColor(0.53f, 0.81f, 0.92f, 1.0f); // Sky blue
        GL.Enable(EnableCap.DepthTest);
        GL.Enable(EnableCap.CullFace);
        GL.CullFace(CullFaceMode.Back);

        // Initialize world
        world = new VoxelWorld(new Vector3Int(4, 2, 4));
        world.GenerateTestTerrain();

        // Initialize structure manager
        structureManager = new StructureManager();

        // Initialize camera
        camera = new Camera(new Vector3(16, 20, 16), Size.X / (float)Size.Y);

        // Load shaders
        voxelShader = new Shader("Shaders/voxel.vert", "Shaders/voxel.frag");

        // Initialize mode-specific components
        if (isEditorMode)
        {
            imguiController = new ImGuiController(this);
            camera.Position = new Vector3(16, 25, 35);
            camera.Pitch = -30;
        }
        else
        {
            // Game mode - setup player
            Vector3 spawnPos = new Vector3(16, 10, 16);
            player = new PlayerController(spawnPos, camera, world);
            CaptureMouse();
        }

        // Build initial meshes
        RebuildAllMeshes();
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        float deltaTime = (float)args.Time;

        if (isEditorMode)
        {
            UpdateEditor(deltaTime);
        }
        else
        {
            UpdateGame(deltaTime);
        }
    }

    private void UpdateEditor(float deltaTime)
    {
        var keyboard = KeyboardState;
        var io = ImGui.GetIO();

        // Update ImGui
        imguiController?.Update(deltaTime);

        // Don't process game input if ImGui wants input
        if (io.WantCaptureMouse || io.WantCaptureKeyboard)
        {
            if (mouseCaptured) ReleaseMouse();
            return;
        }

        // Toggle mouse capture with right click
        if (MouseState.IsButtonPressed(MouseButton.Right) && !mouseCaptured)
        {
            CaptureMouse();
        }
        else if (keyboard.IsKeyPressed(Keys.Escape) && mouseCaptured)
        {
            ReleaseMouse();
        }

        // Camera movement (only when mouse captured or playing)
        if (mouseCaptured || isPlaying)
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

        // Voxel placement/removal (only in edit mode, not playing)
        if (!isPlaying && mouseCaptured)
        {
            if (MouseState.IsButtonPressed(MouseButton.Left))
            {
                PlaceVoxel();
            }
            if (MouseState.IsButtonPressed(MouseButton.Middle))
            {
                RemoveVoxel();
            }
        }
    }

    private void UpdateGame(float deltaTime)
    {
        var keyboard = KeyboardState;

        if (keyboard.IsKeyPressed(Keys.Escape))
        {
            Close();
            return;
        }

        // Player movement
        Vector3 moveDir = Vector3.Zero;

        if (keyboard.IsKeyDown(Keys.W)) moveDir.Z += 1;
        if (keyboard.IsKeyDown(Keys.S)) moveDir.Z -= 1;
        if (keyboard.IsKeyDown(Keys.A)) moveDir.X -= 1;
        if (keyboard.IsKeyDown(Keys.D)) moveDir.X += 1;

        if (moveDir.LengthSquared > 0)
        {
            bool sprint = keyboard.IsKeyDown(Keys.LeftShift);
            player?.ProcessMovement(moveDir, deltaTime, sprint);
        }

        if (keyboard.IsKeyPressed(Keys.Space))
        {
            player?.Jump();
        }

        player?.Update(deltaTime);
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

        // Render world
        RenderWorld();

        // Render UI
        if (isEditorMode)
        {
            RenderEditorUI();
            imguiController?.Render();
        }

        SwapBuffers();
    }

    private void RenderWorld()
    {
        if (voxelShader == null) return;

        voxelShader.Use();

        Matrix4 view = camera.GetViewMatrix();
        Matrix4 projection = camera.GetProjectionMatrix();

        voxelShader.SetMatrix4("view", view);
        voxelShader.SetMatrix4("projection", projection);
        voxelShader.SetVector3("lightDir", new Vector3(-0.3f, -1.0f, -0.5f));
        voxelShader.SetVector3("viewPos", camera.Position);
        voxelShader.SetVector3("fogColor", new Vector3(0.53f, 0.81f, 0.92f));
        voxelShader.SetFloat("fogDensity", 0.0015f);

        foreach (var chunk in world.GetAllChunks())
        {
            Matrix4 model = Matrix4.CreateTranslation(
                chunk.Position.X * Chunk.ChunkSize,
                chunk.Position.Y * Chunk.ChunkSize,
                chunk.Position.Z * Chunk.ChunkSize
            );

            voxelShader.SetMatrix4("model", model);

            if (chunkMeshes.TryGetValue(chunk.Position, out var mesh))
            {
                mesh.Render();
            }
        }
    }

    private void RenderEditorUI()
    {
        ImGui.Begin("VoxelEngine Editor", ImGuiWindowFlags.AlwaysAutoResize);

        ImGui.Text($"FPS: {1.0 / ImGui.GetIO().DeltaTime:F0}");
        ImGui.Text($"Camera: {camera.Position:F1}");
        ImGui.Separator();

        // Play/Edit mode toggle
        if (isPlaying)
        {
            if (ImGui.Button("Stop Playing", new System.Numerics.Vector2(200, 30)))
            {
                isPlaying = false;
            }
            ImGui.Text("Play Mode Active - Test your structures!");
        }
        else
        {
            if (ImGui.Button("Enter Play Mode", new System.Numerics.Vector2(200, 30)))
            {
                isPlaying = true;
                if (player == null)
                {
                    player = new PlayerController(camera.Position, camera, world);
                }
                CaptureMouse();
            }

            ImGui.Separator();
            ImGui.Text("Voxel Painting");

            // Voxel type selector
            string[] voxelNames = Enum.GetNames(typeof(VoxelType));
            int currentType = (int)selectedVoxelType;
            if (ImGui.Combo("Voxel Type", ref currentType, voxelNames, voxelNames.Length))
            {
                selectedVoxelType = (VoxelType)currentType;
            }

            ImGui.Separator();
            ImGui.Text("Structures");

            // Category selector
            string[] categories = { "Architecture", "Ambient" };
            int categoryIndex = (int)selectedCategory;
            if (ImGui.Combo("Category", ref categoryIndex, categories, categories.Length))
            {
                selectedCategory = (StructureCategory)categoryIndex;
                selectedStructureIndex = 0;
            }

            // Structure list
            var structures = selectedCategory == StructureCategory.Architecture
                ? structureManager.ArchitectureStructures
                : structureManager.AmbientStructures;

            if (structures.Count > 0)
            {
                string[] structureNames = structures.Select(s => s.Name).ToArray();
                ImGui.ListBox("Available Structures", ref selectedStructureIndex, structureNames, structureNames.Length, 5);

                if (ImGui.Button("Place Selected Structure"))
                {
                    if (selectedStructureIndex >= 0 && selectedStructureIndex < structures.Count)
                    {
                        var structure = structures[selectedStructureIndex];
                        structure.PlaceInWorld(world, editorCursorPos);
                        RebuildAllMeshes();
                    }
                }
            }
            else
            {
                ImGui.Text("No structures in this category");
            }

            // New structure
            ImGui.Separator();
            if (ImGui.Button("Create New Structure"))
            {
                currentStructure = new Structure($"New{selectedCategory}", selectedCategory);
            }

            if (currentStructure != null)
            {
                ImGui.Text($"Editing: {currentStructure.Name}");
                if (ImGui.Button("Save Structure"))
                {
                    structureManager.SaveStructure(currentStructure);
                    currentStructure = null;
                }
                ImGui.SameLine();
                if (ImGui.Button("Cancel"))
                {
                    currentStructure = null;
                }
            }
        }

        ImGui.Separator();
        ImGui.Text("Controls:");
        ImGui.BulletText("Right Click: Capture mouse for camera");
        ImGui.BulletText("WASD: Move camera");
        ImGui.BulletText("Space/Ctrl: Up/Down");
        ImGui.BulletText("Left Click: Place voxel");
        ImGui.BulletText("Middle Click: Remove voxel");
        ImGui.BulletText("ESC: Release mouse");

        ImGui.End();
    }

    private void PlaceVoxel()
    {
        // Raycast to find placement position
        var hitPos = GetVoxelLookingAt(out bool hit, out var normal);
        if (hit)
        {
            Vector3Int placePos = hitPos + normal;
            world.SetVoxelType(placePos, selectedVoxelType);

            if (currentStructure != null)
            {
                currentStructure.AddVoxel(placePos - editorCursorPos, selectedVoxelType);
            }

            RebuildChunkAt(placePos);
        }
    }

    private void RemoveVoxel()
    {
        var hitPos = GetVoxelLookingAt(out bool hit, out _);
        if (hit)
        {
            world.SetVoxelType(hitPos, VoxelType.Air);

            if (currentStructure != null)
            {
                currentStructure.RemoveVoxel(hitPos - editorCursorPos);
            }

            RebuildChunkAt(hitPos);
        }
    }

    private Vector3Int GetVoxelLookingAt(out bool hit, out Vector3Int normal)
    {
        // Simple raycast
        Vector3 rayStart = camera.Position;
        Vector3 rayDir = camera.Front;
        float maxDistance = 10.0f;
        float step = 0.1f;

        for (float dist = 0; dist < maxDistance; dist += step)
        {
            Vector3 checkPos = rayStart + rayDir * dist;
            Vector3Int voxelPos = new Vector3Int(
                (int)MathF.Floor(checkPos.X),
                (int)MathF.Floor(checkPos.Y),
                (int)MathF.Floor(checkPos.Z)
            );

            var voxel = world.GetVoxel(voxelPos);
            if (voxel.IsActive && voxel.Type != VoxelType.Air)
            {
                // Determine hit normal
                Vector3 prevPos = rayStart + rayDir * (dist - step);
                Vector3Int prevVoxel = new Vector3Int(
                    (int)MathF.Floor(prevPos.X),
                    (int)MathF.Floor(prevPos.Y),
                    (int)MathF.Floor(prevPos.Z)
                );

                normal = voxelPos - prevVoxel;
                hit = true;
                return voxelPos;
            }
        }

        hit = false;
        normal = Vector3Int.Zero;
        return Vector3Int.Zero;
    }

    private void RebuildAllMeshes()
    {
        foreach (var mesh in chunkMeshes.Values)
        {
            mesh.Dispose();
        }
        chunkMeshes.Clear();

        foreach (var chunk in world.GetAllChunks())
        {
            var mesh = new VoxelMesh();
            mesh.BuildMesh(chunk, world);
            chunkMeshes[chunk.Position] = mesh;
        }
    }

    private void RebuildChunkAt(Vector3Int worldPos)
    {
        Vector3Int chunkPos = new Vector3Int(
            worldPos.X / Chunk.ChunkSize,
            worldPos.Y / Chunk.ChunkSize,
            worldPos.Z / Chunk.ChunkSize
        );

        if (chunkMeshes.TryGetValue(chunkPos, out var mesh))
        {
            mesh.Dispose();
        }

        var chunk = world.GetChunk(chunkPos);
        if (chunk != null)
        {
            var newMesh = new VoxelMesh();
            newMesh.BuildMesh(chunk, world);
            chunkMeshes[chunkPos] = newMesh;
        }
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
        imguiController?.Dispose();

        foreach (var mesh in chunkMeshes.Values)
        {
            mesh.Dispose();
        }
    }
}
