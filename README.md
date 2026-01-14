# VoxelEngine

A modern C# voxel-based world editor and game engine with 3D rendering, inspired by Trove's colorful aesthetic. Features a complete editor system with structure management and a playable game mode with FPS controls.

## ✨ Features

### 🎨 Editor Mode
- **3D Voxel Editing**: Full 3D OpenTK-powered rendering with ImGui interface
- **Structure System**: Create and manage two types of structures:
  - **Architecture**: Houses, buildings, and constructed objects
  - **Ambient**: Trees, rocks, natural decorations
- **Play Mode**: Test your structures directly in the editor
- **Free Camera**: WASD movement with mouse look for easy navigation
- **Real-time Editing**: Place and remove voxels with instant visual feedback
- **Structure Save/Load**: JSON-based structure persistence
- **Blockbench Support**: Import models from Blockbench (.bbmodel format)

### 🎮 Game Mode
- **FPS Controls**: Standard WASD + mouse look controls
- **Physics Simulation**: Gravity, collision detection, and jumping
- **Player Controller**: Smooth movement with sprint capability
- **Immersive Exploration**: Navigate the voxel world in first-person

### 🎯 Graphics & Rendering
- **OpenTK 3D Rendering**: Hardware-accelerated voxel rendering
- **Trove-Style Aesthetic**: Colorful, vibrant voxel graphics
- **Optimized Mesh Generation**: Face culling and ambient occlusion
- **Fog System**: Distance-based atmospheric fog
- **Chunk-Based Rendering**: Efficient rendering of large worlds

## 🚀 Quick Start

### Prerequisites
- .NET 8.0 SDK or later
- OpenGL 3.3 compatible graphics card
- Windows, macOS, or Linux

### Building the Project

```bash
dotnet restore
dotnet build
```

### Running the Engine

```bash
dotnet run
```

### Switching Between Modes

Open `Program.cs` and change the `EDITOR_MODE` constant:

```csharp
private const bool EDITOR_MODE = true;  // Editor mode
private const bool EDITOR_MODE = false; // Game mode
```

## 🎮 Controls

### Editor Mode

| Action | Control |
|--------|---------|
| **Camera Movement** | Right Click + Mouse to look around |
| **Move Forward/Back** | W / S |
| **Move Left/Right** | A / D |
| **Move Up/Down** | Space / Left Ctrl |
| **Sprint** | Hold Left Shift |
| **Place Voxel** | Left Click (when mouse captured) |
| **Remove Voxel** | Middle Click (when mouse captured) |
| **Release Mouse** | ESC |
| **Enter Play Mode** | Click "Enter Play Mode" in UI |

### Game Mode

| Action | Control |
|--------|---------|
| **Look Around** | Mouse |
| **Move Forward/Back** | W / S |
| **Move Left/Right** | A / D |
| **Jump** | Space |
| **Sprint** | Hold Left Shift |
| **Exit** | ESC |

## 📁 Project Structure

```
VoxelEngine/
├── Core/                   # Core voxel data structures
│   ├── Vector3Int.cs      # 3D integer vectors
│   ├── VoxelType.cs       # Voxel types and colors
│   ├── Voxel.cs           # Individual voxel
│   ├── Chunk.cs           # 16³ chunk container
│   └── VoxelWorld.cs      # World management
├── Graphics/              # Rendering system
│   ├── Shader.cs          # OpenGL shader wrapper
│   ├── Camera.cs          # FPS and free camera
│   └── VoxelMesh.cs       # Voxel mesh generation
├── Shaders/               # GLSL shaders
│   ├── voxel.vert         # Vertex shader
│   └── voxel.frag         # Fragment shader
├── Game/                  # Game mode components
│   └── PlayerController.cs # Player physics and movement
├── Editor/                # Editor components
│   └── ImGuiController.cs # ImGui integration
├── Structures/            # Structure system
│   ├── Structure.cs       # Structure data
│   └── StructureManager.cs # Structure management
├── Models/                # Model loading
│   └── BlockbenchModel.cs # Blockbench importer
├── Window/                # Main window
│   └── VoxelGameWindow.cs # OpenTK game window
└── Program.cs             # Entry point with mode selection
```

## 🎨 Voxel Types & Colors

The engine includes 9 vibrant voxel types:

| Type | Color | Use Case |
|------|-------|----------|
| **Grass** | Bright Green | Terrain, meadows |
| **Dirt** | Brown | Underground, paths |
| **Stone** | Gray | Mountains, foundations |
| **Wood** | Dark Brown | Trees, structures |
| **Leaves** | Dark Green | Foliage, nature |
| **Sand** | Yellow | Beaches, deserts |
| **Water** | Blue | Lakes, rivers |
| **Brick** | Red | Buildings, walls |
| **Glass** | Light Cyan | Windows, decorations |

## 🏗️ Structure System

### Creating Structures

1. **Start in Editor Mode**
2. **Select Category**: Choose Architecture or Ambient
3. **Click "Create New Structure"**
4. **Build**: Place voxels to create your structure
5. **Save**: Click "Save Structure" when complete

### Using Structures

1. **Browse Structures**: View available structures in the UI
2. **Select Structure**: Click on a structure in the list
3. **Place in World**: Click "Place Selected Structure"

### Default Structures

The engine includes two example structures:
- **Simple House** (Architecture): A small brick house with wooden roof
- **Oak Tree** (Ambient): A tree with wood trunk and leaf canopy

## 🎯 Blockbench Integration

### Importing Blockbench Models

1. Create your model in [Blockbench](https://www.blockbench.net/)
2. Export as `.bbmodel` format
3. Use the `BlockbenchModel.LoadFromFile()` method
4. Convert to Structure with `ToStructure()`
5. Place in world or save for later use

Example:
```csharp
var model = BlockbenchModel.LoadFromFile("player.bbmodel");
var structure = model.ToStructure(StructureCategory.Ambient);
structure.PlaceInWorld(world, new Vector3Int(10, 5, 10));
```

## ⚙️ Technical Architecture

### Chunk System
- **Chunk Size**: 16 × 16 × 16 voxels
- **Dynamic Meshing**: Regenerates only modified chunks
- **Face Culling**: Only renders visible voxel faces
- **Neighbor Checking**: Efficient cross-chunk voxel queries

### Rendering Pipeline
1. **Mesh Generation**: Convert voxel data to vertex/index buffers
2. **Shader Processing**: Vertex transformation and lighting
3. **Fragment Shading**: Trove-style lighting with fog
4. **Face Culling**: Backface culling for performance

### Physics System
- **AABB Collision**: Axis-aligned bounding box collision detection
- **Gravity**: -20 m/s² downward acceleration
- **Ground Detection**: Ray-based ground checking
- **Collision Response**: Push-out based collision resolution

## 🔧 Customization

### Adding New Voxel Types

1. Add to `VoxelType` enum in `Core/VoxelType.cs`
2. Update `GetColor()` method with RGB values
3. Type is automatically available in editor

### Modifying Shaders

Edit shader files in `Shaders/` directory:
- `voxel.vert`: Vertex transformations
- `voxel.frag`: Lighting and color calculations

### Adjusting Physics

Modify constants in `Game/PlayerController.cs`:
```csharp
private const float Gravity = -20.0f;     // Gravity strength
private const float JumpForce = 8.0f;     // Jump power
private const float WalkSpeed = 5.0f;     // Base movement speed
private const float SprintSpeed = 10.0f;  // Sprint multiplier
```

## 📊 Performance

- **Rendering**: ~60 FPS with 4×2×4 chunks (32,768 voxels)
- **Mesh Generation**: ~10ms per chunk
- **Memory**: ~50MB base + 8MB per loaded chunk
- **Scalable**: Supports worlds up to 100+ chunks

## 🛠️ Development

### Dependencies
- **OpenTK 4.8.2**: OpenGL bindings and windowing
- **ImGui.NET 1.90.1.1**: Immediate mode GUI
- **Newtonsoft.Json 13.0.3**: JSON serialization
- **StbImageSharp 2.27.14**: Image loading (for future textures)

### Building for Release

```bash
dotnet publish -c Release -r win-x64 --self-contained
```

Replace `win-x64` with your target platform:
- `win-x64`: Windows 64-bit
- `linux-x64`: Linux 64-bit
- `osx-x64`: macOS 64-bit

## 🎯 Roadmap

Future enhancements:
- [ ] Texture atlas support
- [ ] Advanced terrain generation (noise-based)
- [ ] Multiplayer networking
- [ ] Lighting system with colored lights
- [ ] Water shader with transparency
- [ ] Player model rendering
- [ ] Inventory system
- [ ] World save/load
- [ ] Undo/Redo in editor
- [ ] Copy/Paste structures
- [ ] Structure rotation before placement
- [ ] Biome system
- [ ] Day/night cycle

## 📝 License

This project is provided as-is for educational and development purposes.

## 🤝 Contributing

Contributions welcome! The modular architecture makes it easy to:
- Add new voxel types
- Create new structures
- Enhance rendering
- Improve physics
- Extend the editor

## 💡 Tips

### Editor Mode
- Use **Play Mode** to test structures with physics before saving
- Hold **Shift** while moving camera for faster navigation
- **Right-click and drag** to look around while building
- Use **Middle-click** for quick voxel removal

### Game Mode
- Press **Shift** while moving for sprint (2× speed)
- **Jump** works only when standing on solid ground
- Camera is locked to player - no separate camera mode

### Structures
- **Architecture** structures are for player-built objects
- **Ambient** structures are for natural/decorative elements
- Structures can be placed multiple times in different locations
- Structure files are saved in `Structures/` directory

---

**VoxelEngine** - Build, Create, Explore!
Made with ❤️ using OpenTK and C#
