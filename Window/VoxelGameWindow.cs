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
using VoxelEngine.World;
using ImGuiNET;

namespace VoxelEngine.Window;

public class VoxelGameWindow : GameWindow
{
    private readonly bool isEditorMode;
    private InfiniteVoxelWorld world;
    private ThirdPersonCamera? thirdPersonCamera;
    private Camera? editorCamera;
    private Shader? voxelShader;
    private SkyboxRenderer? skyboxRenderer;
    private Dictionary<Vector3Int, VoxelMesh> chunkMeshes = new();
    private StructureManager structureManager;
    private InfiniteWorldGenerator worldGenerator;
    private WorldGenConfig worldGenConfig;
    private ChunkLoadingSystem chunkLoadingSystem;
    private ImGuiController? imguiController;
    private TickSystem tickSystem;
    private DayNightCycle dayNightCycle;
    private WaterSimulation? waterSimulation;
    private FrustumCulling frustumCulling;
    private OcclusionCulling occlusionCulling;
    private float gameTime = 0.0f;
    private bool initialLoadComplete = false;

    // Render stats
    private int lastRenderedChunks = 0;
    private int lastTotalChunks = 0;
    private int lastFrustumCulled = 0;
    private int lastOcclusionCulled = 0;

    // Game mode
    private PlayerController? player;
    private Vector3 playerPosition = Vector3.Zero;

    // Editor mode
    private Vector3Int editorCursorPos = Vector3Int.Zero;
    private VoxelType selectedVoxelType = VoxelType.Grass;
    private bool isPlaying = false;
    private Structure? currentStructure = null;
    private StructureCategory selectedCategory = StructureCategory.Architecture;
    private int selectedStructureIndex = 0;
    private bool showWorldGenUI = false;

    // Input
    private bool firstMove = true;
    private Vector2 lastMousePos;
    private bool mouseCaptured = false;

    public VoxelGameWindow(bool editorMode)
        : base(GameWindowSettings.Default, new NativeWindowSettings()
        {
            Size = new Vector2i(1600, 900),
            Title = editorMode ? "VoxelEngine - Editor Mode" : "VoxelEngine - RPG Mode",
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

        GL.ClearColor(0.53f, 0.81f, 0.92f, 1.0f);
        GL.Enable(EnableCap.DepthTest);
        GL.Enable(EnableCap.CullFace);
        GL.CullFace(CullFaceMode.Back);

        // Enable blending for transparent water
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        // Initialize tick system
        tickSystem = new TickSystem();

        // Initialize day/night cycle
        dayNightCycle = new DayNightCycle();
        dayNightCycle.SetToDawn(); // Start at dawn

        // Initialize frustum culling
        frustumCulling = new FrustumCulling();

        // Initialize occlusion culling (disabled during initial load for better performance)
        occlusionCulling = new OcclusionCulling();
        occlusionCulling.OcclusionEnabled = false;

        // Load world generation config
        worldGenConfig = WorldGenConfig.LoadFromFile();

        // Initialize infinite world with initial render distance
        world = new InfiniteVoxelWorld(renderDistance: 12, verticalChunks: 10);

        // Initialize infinite world generator
        worldGenerator = new InfiniteWorldGenerator(worldGenConfig);

        // Initialize async chunk loading system
        chunkLoadingSystem = new ChunkLoadingSystem(worldGenerator, world);
        chunkLoadingSystem.Start();

        // Initialize water simulation
        waterSimulation = new WaterSimulation(world, tickSystem, worldGenConfig.WaterLevel);

        // Initialize structure manager
        structureManager = new StructureManager();

        // Load shaders
        voxelShader = new Shader("Shaders/voxel.vert", "Shaders/voxel.frag");
        skyboxRenderer = new SkyboxRenderer();

        // Initialize mode-specific components
        if (isEditorMode)
        {
            imguiController = new ImGuiController(this);
            editorCamera = new Camera(new Vector3(0, 200, 0), Size.X / (float)Size.Y);
            editorCamera.Pitch = -30;
            editorCamera.ViewDistanceChunks = 12;

            // Generate initial chunks around editor spawn
            GenerateChunksAroundPosition(editorCamera.Position);
        }
        else
        {
            // Game mode - setup third person camera and player
            playerPosition = new Vector3(0, 200, 0);
            thirdPersonCamera = new ThirdPersonCamera(playerPosition, Size.X / (float)Size.Y);
            thirdPersonCamera.ViewDistanceChunks = 12;

            // Generate initial chunks
            GenerateChunksAroundPosition(playerPosition);

            // Find suitable spawn position
            FindSpawnPosition();

            if (editorCamera == null)
            {
                editorCamera = new Camera(playerPosition, Size.X / (float)Size.Y);
            }
            player = new PlayerController(playerPosition, editorCamera, world);
            CaptureMouse();
        }

        // Meshes will be built asynchronously as chunks are generated
    }

    private void FindSpawnPosition()
    {
        // Find a solid ground position (search downward from spawn height)
        for (int y = 250; y >= 0; y--)
        {
            var voxel = world.GetVoxel(new Vector3Int((int)playerPosition.X, y, (int)playerPosition.Z));
            if (voxel.IsActive && voxel.Type.IsSolid())
            {
                playerPosition = new Vector3(playerPosition.X, y + 2, playerPosition.Z);
                if (thirdPersonCamera != null)
                    thirdPersonCamera.TargetPosition = playerPosition;
                break;
            }
        }
    }

    private void GenerateChunksAroundPosition(Vector3 position)
    {
        // Update chunks in the infinite world
        world.UpdateChunksAroundPosition(position);

        // Queue all new chunks for async generation
        foreach (var chunkPos in world.GetAllChunkPositions())
        {
            chunkLoadingSystem.QueueChunkForGeneration(chunkPos);
        }
    }

    private void UpdateChunksAroundCamera(Vector3 position, int viewDistance)
    {
        // Get existing chunk positions before update
        var existingChunks = new HashSet<Vector3Int>(world.GetAllChunkPositions());

        // Update the infinite world based on new position
        world.UpdateChunksAroundPosition(position);

        // Get new chunk positions after update
        var currentChunks = new HashSet<Vector3Int>(world.GetAllChunkPositions());

        // Find chunks that were added - queue them for async generation
        var addedChunks = currentChunks.Except(existingChunks).ToList();
        foreach (var chunkPos in addedChunks)
        {
            chunkLoadingSystem.QueueChunkForGeneration(chunkPos);
        }

        // Find chunks that were removed
        var removedChunks = existingChunks.Except(currentChunks).ToList();

        // Clean up meshes for removed chunks
        foreach (var chunkPos in removedChunks)
        {
            if (chunkMeshes.TryGetValue(chunkPos, out var mesh))
            {
                mesh.Dispose();
                chunkMeshes.Remove(chunkPos);
            }
        }
    }

    private void ProcessChunkMeshing()
    {
        // Build meshes for chunks that have finished terrain generation
        // Limit to 1 chunk per frame with 5ms time budget to maintain 60+ FPS
        var chunksReady = chunkLoadingSystem.GetChunksReadyForMeshing(maxPerFrame: 1, maxTimeMs: 5.0f);

        foreach (var chunk in chunksReady)
        {
            // Build mesh on main thread (OpenGL requirement)
            var mesh = new VoxelMesh();
            mesh.BuildMesh(chunk, world);
            chunkMeshes[chunk.Position] = mesh;
        }

        // Mark initial load as complete once all chunks are loaded
        if (!initialLoadComplete && !chunkLoadingSystem.IsLoading)
        {
            initialLoadComplete = true;
            occlusionCulling.OcclusionEnabled = true; // Re-enable after initial load
            Console.WriteLine("Initial chunk loading complete!");
        }
    }


    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        float deltaTime = (float)args.Time;

        // Update game time
        gameTime += deltaTime;

        // Process chunks ready for meshing (limit to avoid frame drops)
        ProcessChunkMeshing();

        // Update tick system
        tickSystem.Update(deltaTime);

        // Update day/night cycle
        dayNightCycle.Update(deltaTime);

        // Check for water changes and rebuild affected chunks
        if (waterSimulation != null)
        {
            var changedPositions = waterSimulation.GetAndClearChangedPositions();
            if (changedPositions.Count > 0)
            {
                HashSet<Vector3Int> affectedChunks = new();
                foreach (var pos in changedPositions)
                {
                    var chunks = world.GetAffectedChunks(pos);
                    foreach (var chunk in chunks)
                    {
                        affectedChunks.Add(chunk);
                    }
                }

                foreach (var chunkPos in affectedChunks)
                {
                    RebuildChunk(chunkPos);
                }
            }
        }

        if (isEditorMode)
        {
            UpdateEditor(deltaTime);

            // Update chunks around editor camera
            if (editorCamera != null)
            {
                chunkLoadingSystem.UpdateCameraPosition(editorCamera.Position);
                UpdateChunksAroundCamera(editorCamera.Position, editorCamera.ViewDistanceChunks);
            }
        }
        else
        {
            UpdateGame(deltaTime);

            // Update chunks around player
            if (player != null && thirdPersonCamera != null)
            {
                chunkLoadingSystem.UpdateCameraPosition(player.Position);
                UpdateChunksAroundCamera(player.Position, thirdPersonCamera.ViewDistanceChunks);
            }
        }

        // Hot reload worldgen in editor
        if (isEditorMode && KeyboardState.IsKeyPressed(Keys.F5))
        {
            Console.WriteLine("Hot reloading worldgen...");
            worldGenConfig = WorldGenConfig.LoadFromFile();
            worldGenerator = new InfiniteWorldGenerator(worldGenConfig);

            // Stop current loading system
            chunkLoadingSystem.Stop();

            // Clear loading queues
            chunkLoadingSystem.Clear();

            // Clear and regenerate current chunks
            foreach (var mesh in chunkMeshes.Values)
            {
                mesh.Dispose();
            }
            chunkMeshes.Clear();

            // Restart loading system with new generator
            chunkLoadingSystem = new ChunkLoadingSystem(worldGenerator, world);
            chunkLoadingSystem.Start();

            if (editorCamera != null)
            {
                GenerateChunksAroundPosition(editorCamera.Position);
            }

            waterSimulation = new WaterSimulation(world, tickSystem, worldGenConfig.WaterLevel);
            initialLoadComplete = false;
        }
    }

    private void UpdateEditor(float deltaTime)
    {
        var keyboard = KeyboardState;
        var io = ImGui.GetIO();

        imguiController?.Update(deltaTime);

        if (io.WantCaptureMouse || io.WantCaptureKeyboard)
        {
            if (mouseCaptured) ReleaseMouse();
            return;
        }

        if (MouseState.IsButtonPressed(MouseButton.Right) && !mouseCaptured)
        {
            CaptureMouse();
        }
        else if (keyboard.IsKeyPressed(Keys.Escape) && mouseCaptured)
        {
            ReleaseMouse();
        }

        if (mouseCaptured || isPlaying)
        {
            float speed = keyboard.IsKeyDown(Keys.LeftShift) ? 15.0f : 7.0f;

            if (editorCamera != null)
            {
                if (keyboard.IsKeyDown(Keys.W))
                    editorCamera.ProcessKeyboard(CameraMovement.Forward, deltaTime, speed);
                if (keyboard.IsKeyDown(Keys.S))
                    editorCamera.ProcessKeyboard(CameraMovement.Backward, deltaTime, speed);
                if (keyboard.IsKeyDown(Keys.A))
                    editorCamera.ProcessKeyboard(CameraMovement.Left, deltaTime, speed);
                if (keyboard.IsKeyDown(Keys.D))
                    editorCamera.ProcessKeyboard(CameraMovement.Right, deltaTime, speed);
                if (keyboard.IsKeyDown(Keys.Space))
                    editorCamera.ProcessKeyboard(CameraMovement.Up, deltaTime, speed);
                if (keyboard.IsKeyDown(Keys.LeftControl))
                    editorCamera.ProcessKeyboard(CameraMovement.Down, deltaTime, speed);
            }
        }

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

        // Update camera to follow player
        if (player != null && thirdPersonCamera != null)
        {
            thirdPersonCamera.TargetPosition = player.Position + Vector3.UnitY * 0.9f;
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

            if (isEditorMode)
            {
                editorCamera?.ProcessMouseMovement(deltaX, deltaY);
            }
            else
            {
                thirdPersonCamera?.ProcessMouseMovement(deltaX, deltaY);
            }
        }

        lastMousePos = new Vector2(e.X, e.Y);
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);

        if (!isEditorMode && thirdPersonCamera != null)
        {
            thirdPersonCamera.ProcessMouseScroll(e.OffsetY);
        }
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        RenderWorld();

        if (isEditorMode)
        {
            RenderEditorUI();
            imguiController?.Render();
        }

        SwapBuffers();
    }

    private void RenderWorld()
    {
        if (voxelShader == null || skyboxRenderer == null) return;

        Matrix4 view, projection;
        Vector3 viewPos, viewFront;

        if (isEditorMode && editorCamera != null)
        {
            view = editorCamera.GetViewMatrix();
            projection = editorCamera.GetProjectionMatrix();
            viewPos = editorCamera.Position;
            viewFront = editorCamera.Front;
        }
        else if (thirdPersonCamera != null)
        {
            view = thirdPersonCamera.GetViewMatrix();
            projection = thirdPersonCamera.GetProjectionMatrix();
            viewPos = thirdPersonCamera.Position;
            viewFront = thirdPersonCamera.Front;
        }
        else
        {
            return;
        }

        // Update frustum for culling
        Matrix4 viewProjection = view * projection;
        frustumCulling.UpdateFrustum(viewProjection);

        // Update occlusion culling
        occlusionCulling.UpdateCameraPosition(viewPos);

        // Build loaded chunks dictionary for occlusion culling
        var loadedChunks = new Dictionary<Vector3Int, bool>();
        foreach (var chunk in world.GetAllChunks())
        {
            loadedChunks[chunk.Position] = true;
        }

        // Render skybox first
        skyboxRenderer.Render(view, projection, dayNightCycle.GetSunDirection(),
                             dayNightCycle.GetMoonDirection(), dayNightCycle.CurrentTime);

        // Render voxel world
        voxelShader.Use();
        voxelShader.SetMatrix4("view", view);
        voxelShader.SetMatrix4("projection", projection);
        voxelShader.SetVector3("sunDirection", dayNightCycle.GetSunDirection());
        voxelShader.SetVector3("moonDirection", dayNightCycle.GetMoonDirection());
        voxelShader.SetVector3("viewPos", viewPos);
        voxelShader.SetVector3("fogColor", dayNightCycle.GetSkyColor());
        voxelShader.SetFloat("fogDensity", 0.0008f);
        voxelShader.SetFloat("dayNightCycle", dayNightCycle.CurrentTime);
        voxelShader.SetFloat("time", gameTime);

        int renderedChunks = 0;
        int totalChunks = 0;
        int frustumCulled = 0;
        int occlusionCulled = 0;

        foreach (var chunk in world.GetAllChunks())
        {
            totalChunks++;

            Vector3 chunkWorldPos = new Vector3(
                chunk.Position.X * Chunk.ChunkSize,
                chunk.Position.Y * Chunk.ChunkSize,
                chunk.Position.Z * Chunk.ChunkSize
            );

            // Frustum culling - skip chunks outside view frustum
            if (!frustumCulling.IsChunkVisible(chunkWorldPos, Chunk.ChunkSize))
            {
                frustumCulled++;
                continue;
            }

            // Occlusion culling - skip chunks hidden by terrain (optimized for performance)
            if (!occlusionCulling.IsChunkVisibleWithOcclusion(chunk.Position, chunkWorldPos, viewPos,
                                                               viewFront, Chunk.ChunkSize, loadedChunks))
            {
                occlusionCulled++;
                continue;
            }

            renderedChunks++;

            Matrix4 model = Matrix4.CreateTranslation(chunkWorldPos);
            voxelShader.SetMatrix4("model", model);

            if (chunkMeshes.TryGetValue(chunk.Position, out var mesh))
            {
                mesh.Render();
            }
        }

        // Store stats for UI
        lastRenderedChunks = renderedChunks;
        lastTotalChunks = totalChunks;
        lastFrustumCulled = frustumCulled;
        lastOcclusionCulled = occlusionCulled;
    }

    private void RenderEditorUI()
    {
        if (editorCamera == null) return;

        ImGui.Begin("VoxelEngine Editor", ImGuiWindowFlags.AlwaysAutoResize);

        ImGui.Text($"FPS: {1.0 / ImGui.GetIO().DeltaTime:F0}");
        ImGui.Text($"Tick: {tickSystem.CurrentTick} ({tickSystem.TickRate} TPS)");
        ImGui.Text($"Camera: {editorCamera.Position:F1}");
        ImGui.Separator();

        // Chunk stats
        ImGui.Text($"Chunks Loaded: {lastTotalChunks}");
        ImGui.Text($"Chunks Rendered: {lastRenderedChunks}");
        ImGui.Text($"Frustum Culled: {lastFrustumCulled}");
        ImGui.Text($"Occlusion Culled: {lastOcclusionCulled}");
        float cullPercentage = lastTotalChunks > 0 ? ((lastFrustumCulled + lastOcclusionCulled) * 100.0f / lastTotalChunks) : 0;
        ImGui.Text($"Culling Efficiency: {cullPercentage:F1}%");

        bool occlusionEnabled = occlusionCulling.OcclusionEnabled;
        if (ImGui.Checkbox("Occlusion Culling", ref occlusionEnabled))
        {
            occlusionCulling.OcclusionEnabled = occlusionEnabled;
        }
        ImGui.Separator();

        // Loading status
        if (chunkLoadingSystem.IsLoading)
        {
            ImGui.TextColored(new System.Numerics.Vector4(1, 1, 0, 1), "LOADING CHUNKS...");
            ImGui.Text($"Generating: {chunkLoadingSystem.ChunksGenerating}");
            ImGui.Text($"Queue: {chunkLoadingSystem.ChunksInGenerationQueue}");
            ImGui.Text($"Meshing: {chunkLoadingSystem.ChunksInMeshQueue}");
        }
        else if (initialLoadComplete)
        {
            ImGui.TextColored(new System.Numerics.Vector4(0, 1, 0, 1), "Ready");
        }

        ImGui.Separator();

        // View Distance Control
        if (ImGui.CollapsingHeader("Render Settings"))
        {
            int viewDist = editorCamera.ViewDistanceChunks;
            if (ImGui.SliderInt("View Distance (chunks)", ref viewDist, 4, 24))
            {
                editorCamera.ViewDistanceChunks = viewDist;
            }
            ImGui.Text($"Render Distance: {viewDist * Chunk.ChunkSize} blocks");
        }

        ImGui.Separator();

        // World generation
        if (ImGui.CollapsingHeader("World Generation"))
        {
            ImGui.Text("Press F5 to hot reload worldgen");

            if (ImGui.Button("Edit Worldgen Config"))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "worldgen_config.json",
                    UseShellExecute = true
                });
            }

            ImGui.Text($"Seed: {worldGenConfig.Seed}");
            ImGui.Text($"Water Level: {worldGenConfig.WaterLevel}");
            ImGui.Text($"Max Height: {worldGenConfig.MaxHeight}");
        }

        // Day/Night Cycle
        if (ImGui.CollapsingHeader("Day/Night Cycle"))
        {
            ImGui.Text($"Time of Day: {dayNightCycle.GetTimeOfDay()}");
            ImGui.Text($"Cycle: {dayNightCycle.CurrentTime:F2}");

            if (ImGui.Button("Set to Dawn"))
                dayNightCycle.SetToDawn();
            ImGui.SameLine();
            if (ImGui.Button("Set to Noon"))
                dayNightCycle.SetToNoon();

            if (ImGui.Button("Set to Dusk"))
                dayNightCycle.SetToDusk();
            ImGui.SameLine();
            if (ImGui.Button("Set to Night"))
                dayNightCycle.SetToMidnight();

            float dayLength = dayNightCycle.DayLength;
            if (ImGui.SliderFloat("Day Length (s)", ref dayLength, 10.0f, 600.0f))
            {
                dayNightCycle.DayLength = dayLength;
            }
        }

        // Play/Edit mode toggle
        if (isPlaying)
        {
            if (ImGui.Button("Stop Playing", new System.Numerics.Vector2(200, 30)))
            {
                isPlaying = false;
            }
            ImGui.Text("Play Mode Active");
        }
        else
        {
            if (ImGui.Button("Enter Play Mode", new System.Numerics.Vector2(200, 30)))
            {
                isPlaying = true;
                if (player == null)
                {
                    player = new PlayerController(editorCamera.Position, editorCamera, world);
                }
                CaptureMouse();
            }

            ImGui.Separator();
            ImGui.Text("Voxel Painting");

            string[] voxelNames = Enum.GetNames(typeof(VoxelType));
            int currentType = (int)selectedVoxelType;
            if (ImGui.Combo("Voxel Type", ref currentType, voxelNames, voxelNames.Length))
            {
                selectedVoxelType = (VoxelType)currentType;
            }

            ImGui.Separator();
            ImGui.Text("Structures");

            string[] categories = { "Architecture", "Ambient" };
            int categoryIndex = (int)selectedCategory;
            if (ImGui.Combo("Category", ref categoryIndex, categories, categories.Length))
            {
                selectedCategory = (StructureCategory)categoryIndex;
                selectedStructureIndex = 0;
            }

            var structures = selectedCategory == StructureCategory.Architecture
                ? structureManager.ArchitectureStructures
                : structureManager.AmbientStructures;

            if (structures.Count > 0)
            {
                string[] structureNames = structures.Select(s => s.Name).ToArray();
                ImGui.ListBox("Available", ref selectedStructureIndex, structureNames, structureNames.Length, 5);

                if (ImGui.Button("Place Structure"))
                {
                    if (selectedStructureIndex >= 0 && selectedStructureIndex < structures.Count)
                    {
                        var structure = structures[selectedStructureIndex];
                        structure.PlaceInWorld(world, editorCursorPos);
                        RebuildAllMeshes();
                    }
                }
            }

            if (ImGui.Button("Open Structure Editor"))
            {
                // Launch structure editor in new process
                LaunchStructureEditor();
            }
        }

        ImGui.Separator();
        ImGui.Text("Controls:");
        ImGui.BulletText("Right Click: Capture mouse");
        ImGui.BulletText("WASD: Move");
        ImGui.BulletText("Space/Ctrl: Up/Down");
        ImGui.BulletText("Left Click: Place voxel");
        ImGui.BulletText("Middle Click: Remove voxel");
        ImGui.BulletText("F5: Reload worldgen");
        ImGui.BulletText("ESC: Release mouse");

        ImGui.End();
    }

    private void LaunchStructureEditor()
    {
        var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule!.FileName;
        var psi = new System.Diagnostics.ProcessStartInfo(exePath, "--structure-editor")
        {
            UseShellExecute = true
        };
        System.Diagnostics.Process.Start(psi);
        Console.WriteLine("Structure Editor launched as a new process");
    }

    private void PlaceVoxel()
    {
        var hitPos = GetVoxelLookingAt(out bool hit, out var normal);
        if (hit)
        {
            Vector3Int placePos = hitPos + normal;
            world.SetVoxelType(placePos, selectedVoxelType);

            if (currentStructure != null)
            {
                currentStructure.AddVoxel(placePos - editorCursorPos, selectedVoxelType);
            }

            // Notify water simulation of voxel change
            waterSimulation?.OnVoxelChanged(placePos, selectedVoxelType);

            RebuildChunkAt(placePos);
        }
    }

    private void RemoveVoxel()
    {
        var hitPos = GetVoxelLookingAt(out bool hit, out _);
        if (hit)
        {
            var voxel = world.GetVoxel(hitPos);

            // Don't allow breaking water blocks
            if (voxel.Type == VoxelType.Water)
            {
                return;
            }

            world.SetVoxelType(hitPos, VoxelType.Air);

            if (currentStructure != null)
            {
                currentStructure.RemoveVoxel(hitPos - editorCursorPos);
            }

            // Notify water simulation of voxel change
            waterSimulation?.OnVoxelChanged(hitPos, VoxelType.Air);

            RebuildChunkAt(hitPos);
        }
    }

    private Vector3Int GetVoxelLookingAt(out bool hit, out Vector3Int normal)
    {
        Vector3 rayStart;
        Vector3 rayDir;

        if (isEditorMode && editorCamera != null)
        {
            rayStart = editorCamera.Position;
            rayDir = editorCamera.Front;
        }
        else if (thirdPersonCamera != null)
        {
            rayStart = thirdPersonCamera.Position;
            rayDir = thirdPersonCamera.Front;
        }
        else
        {
            hit = false;
            normal = Vector3Int.Zero;
            return Vector3Int.Zero;
        }

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
        // Get all affected chunks (including neighbors if on edge)
        var affectedChunks = world.GetAffectedChunks(worldPos);

        foreach (var chunkPos in affectedChunks)
        {
            RebuildChunk(chunkPos);
        }
    }

    private void RebuildChunk(Vector3Int chunkPos)
    {
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

        if (editorCamera != null)
            editorCamera.AspectRatio = e.Width / (float)e.Height;
        if (thirdPersonCamera != null)
            thirdPersonCamera.AspectRatio = e.Width / (float)e.Height;
    }

    protected override void OnUnload()
    {
        base.OnUnload();

        // Stop chunk loading thread
        chunkLoadingSystem?.Stop();

        voxelShader?.Dispose();
        skyboxRenderer?.Dispose();
        imguiController?.Dispose();

        foreach (var mesh in chunkMeshes.Values)
        {
            mesh.Dispose();
        }
    }
}
