using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using VoxelEngine.Core;
using VoxelEngine.Graphics;
using VoxelEngine.Structures;
using ImGuiNET;

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
    private const int GridSize = 32;

    private VoxelType selectedVoxelType = VoxelType.Grass;
    private bool mouseCaptured = false;
    private bool firstMove = true;
    private Vector2 lastMousePos;

    private string structureName = "NewStructure";
    private StructureCategory structureCategory = StructureCategory.Architecture;

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
        VSync = VSyncMode.On;
    }

    protected override void OnLoad()
    {
        base.OnLoad();

        GL.ClearColor(0.2f, 0.2f, 0.2f, 1.0f);
        GL.Enable(EnableCap.DepthTest);
        GL.Enable(EnableCap.CullFace);
        GL.CullFace(CullFaceMode.Back);

        // Initialize camera
        camera = new Camera(new Vector3(16, 16, 16), Size.X / (float)Size.Y);
        camera.Pitch = -30;

        // Load shaders
        voxelShader = new Shader("Shaders/voxel.vert", "Shaders/voxel.frag");
        CreateGridShader();

        // Initialize ImGui
        imguiController = new ImGuiController(this);

        // Create grid
        CreateGrid();

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

        gridShader = new Shader("Shaders/voxel.vert", "Shaders/voxel.frag");
        gridShader.Handle = program;
    }

    private void CreateGrid()
    {
        List<float> vertices = new();

        // Create grid lines
        for (int i = 0; i <= GridSize; i++)
        {
            // Lines along X axis
            vertices.Add(i); vertices.Add(0); vertices.Add(0);
            vertices.Add(i); vertices.Add(0); vertices.Add(GridSize);

            // Lines along Z axis
            vertices.Add(0); vertices.Add(0); vertices.Add(i);
            vertices.Add(GridSize); vertices.Add(0); vertices.Add(i);
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

            // Voxel editing
            if (MouseState.IsButtonPressed(MouseButton.Right))
            {
                PlaceVoxel();
            }
            if (MouseState.IsButtonPressed(MouseButton.Left))
            {
                RemoveVoxel();
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
            Vector4 gridColor = new Vector4(0.3f, 0.3f, 0.3f, 1.0f);
            GL.Uniform4(GL.GetUniformLocation(gridShader.Handle, "color"), gridColor);

            GL.BindVertexArray(gridVAO);
            GL.DrawArrays(PrimitiveType.Lines, 0, (GridSize + 1) * 4);
            GL.BindVertexArray(0);
        }

        // Render structure
        if (voxelShader != null && structureMesh != null)
        {
            voxelShader.Use();
            voxelShader.SetMatrix4("view", view);
            voxelShader.SetMatrix4("projection", projection);
            voxelShader.SetMatrix4("model", Matrix4.Identity);
            voxelShader.SetVector3("lightDir", new Vector3(-0.3f, -1.0f, -0.5f));
            voxelShader.SetVector3("viewPos", camera.Position);
            voxelShader.SetVector3("fogColor", new Vector3(0.2f, 0.2f, 0.2f));
            voxelShader.SetFloat("fogDensity", 0.001f);

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
        ImGui.Text("Controls:");
        ImGui.BulletText("Right Click: Capture mouse");
        ImGui.BulletText("Right Click (captured): Place voxel");
        ImGui.BulletText("Left Click (captured): Remove voxel");
        ImGui.BulletText("WASD: Move camera");
        ImGui.BulletText("Space/Ctrl: Up/Down");
        ImGui.BulletText("ESC: Release mouse");

        ImGui.End();
    }

    private void PlaceVoxel()
    {
        var hitPos = GetVoxelLookingAt(out bool hit, out var normal);
        if (hit)
        {
            Vector3Int placePos = hitPos + normal;
            currentStructure.AddVoxel(placePos, selectedVoxelType);
            RebuildMesh();
        }
    }

    private void RemoveVoxel()
    {
        var hitPos = GetVoxelLookingAt(out bool hit, out _);
        if (hit)
        {
            currentStructure.RemoveVoxel(hitPos);
            RebuildMesh();
        }
    }

    private Vector3Int GetVoxelLookingAt(out bool hit, out Vector3Int normal)
    {
        Vector3 rayStart = camera.Position;
        Vector3 rayDir = camera.Front;
        float maxDistance = 50.0f;
        float step = 0.1f;

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

        var manager = new StructureManager();
        manager.SaveStructure(currentStructure);

        Console.WriteLine($"Structure '{structureName}' saved successfully!");
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
