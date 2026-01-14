# VoxelEngine

A powerful C# voxel-based world editor and game engine with dual-mode functionality.

## Features

### 🎨 Editor Mode
- **Interactive 3D Voxel Editing**: Create and modify voxel structures in real-time
- **Layer-based Editing**: Navigate through different Y-layers to build complex 3D structures
- **Multiple Voxel Types**: Choose from 9 different voxel types including grass, dirt, stone, wood, leaves, sand, water, brick, and glass
- **Visual Cursor**: Clear indication of current editing position
- **Save/Load Structures**: Persist your creations to disk

### 🎮 Game Mode
- **First-Person Exploration**: Navigate through your created worlds
- **Physics Simulation**: Experience gravity and collision detection
- **Player Movement**: Full 3D movement with jumping capabilities
- **Real-time World Interaction**: Interact with the voxel world you've built

### 🔧 Core Architecture
- **Chunk-Based World Management**: Efficient rendering and memory management
- **Modular Design**: Easy to extend with new modes and features
- **Clean Separation of Concerns**: Core, Rendering, Modes, and Engine namespaces

## Project Structure

```
VoxelEngine/
├── Core/
│   ├── Vector3Int.cs       # 3D integer vector implementation
│   ├── VoxelType.cs        # Voxel type enumeration and extensions
│   ├── Voxel.cs            # Individual voxel structure
│   ├── Chunk.cs            # 16x16x16 chunk management
│   └── VoxelWorld.cs       # World management and terrain generation
├── Rendering/
│   └── ConsoleRenderer.cs  # Console-based visualization
├── Modes/
│   ├── IGameMode.cs        # Mode interface
│   ├── EditorMode.cs       # Structure editing mode
│   └── GameMode.cs         # Gameplay mode
├── Engine/
│   ├── ModeManager.cs      # Mode switching and management
│   └── VoxelEngineApp.cs   # Main application logic
└── Program.cs              # Entry point

```

## Getting Started

### Prerequisites
- .NET 8.0 SDK or later
- A terminal with color support

### Building the Project

```bash
dotnet build
```

### Running the Engine

```bash
dotnet run
```

## Controls

### Editor Mode

| Key | Action |
|-----|--------|
| **Arrow Keys** | Move cursor horizontally (X/Z axes) |
| **W** | Move layer up (increase Y) |
| **S** | Move layer down (decrease Y) |
| **Space** | Place selected voxel at cursor |
| **Delete/Backspace** | Remove voxel at cursor |
| **1-9** | Select voxel type (1=Grass, 2=Dirt, 3=Stone, etc.) |
| **M** | Switch to Game Mode |
| **Esc** | Exit application |

### Game Mode

| Key | Action |
|-----|--------|
| **Arrow Keys** | Move player horizontally (X/Z axes) |
| **W** | Move up (Y+) |
| **S** | Move down (Y-) |
| **Space** | Jump (when on ground) |
| **M** | Switch to Editor Mode |
| **Esc** | Exit application |

## Voxel Types

The engine supports the following voxel types:

1. **Grass** (▓) - Green terrain surface
2. **Dirt** (▒) - Brown underground material
3. **Stone** (█) - Gray solid rock
4. **Wood** (║) - Dark red wooden material
5. **Leaves** (♣) - Green foliage
6. **Sand** (░) - Yellow sandy terrain
7. **Water** (≈) - Blue liquid (non-solid)
8. **Brick** (■) - Red building material
9. **Glass** (□) - Cyan transparent material

## Architecture Details

### Core Components

- **Vector3Int**: Efficient 3D integer coordinate system
- **Voxel**: Individual voxel with type and active state
- **Chunk**: 16³ voxel container for efficient world management
- **VoxelWorld**: High-level world management with chunk-based organization

### Rendering System

- **ConsoleRenderer**: Terminal-based visualization using colored ASCII characters
- Supports layer-based 2D views of 3D space
- Distinct rendering modes for Editor and Game views

### Mode System

- **IGameMode**: Interface defining mode behavior
- **ModeManager**: Handles mode switching and input routing
- **EditorMode**: Provides structure creation and editing tools
- **GameMode**: Implements player physics and world exploration

## Extending the Engine

### Adding New Voxel Types

1. Add new type to `VoxelType` enum in `Core/VoxelType.cs`
2. Update `GetDisplayChar()` method with visual representation
3. Update `GetDisplayColor()` method with color assignment

### Creating New Modes

1. Implement `IGameMode` interface
2. Register mode in `VoxelEngineApp.InitializeModes()`
3. Mode will automatically integrate with mode switching system

### Improving Rendering

The current console-based renderer can be replaced with:
- OpenGL/DirectX 3D rendering
- MonoGame integration
- Unity or Godot backend

## Performance Characteristics

- **World Size**: Configurable via `VoxelWorld` constructor
- **Chunk Size**: 16³ voxels (4,096 voxels per chunk)
- **Default World**: 2³ chunks (32,768 total voxels)
- **Memory Efficient**: Only active voxels are tracked
- **Scalable**: Chunk-based system allows for large worlds

## Future Enhancements

Potential areas for expansion:
- Serialization system for world persistence
- Multiplayer networking support
- Advanced terrain generation algorithms
- Lighting and shadow systems
- Texture mapping for voxel faces
- Tool system for batch editing
- Undo/Redo functionality
- 3D camera system with rotation

## License

See LICENSE file for details.

## Contributing

Contributions are welcome! The modular architecture makes it easy to add new features:
- New voxel types
- Additional game modes
- Enhanced rendering systems
- Physics improvements
- World generation algorithms

## Technical Notes

- Built with .NET 8.0 and C# 12
- Uses modern C# features (records, pattern matching, null-safety)
- Console-based for maximum compatibility
- No external dependencies required
- Cross-platform (Windows, macOS, Linux)

---

**VoxelEngine** - Create, Build, Explore!
