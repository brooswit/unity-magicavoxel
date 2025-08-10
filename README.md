# Unity MagicaVoxel

Dynamically load MagicaVoxel .vox models in real-time within Unity3D.

## Architecture Overview

### Voxel File Loader
Responsible for holding the raw voxel data from .vox files.

### VoxelDefinition (MonoBehaviour)
A MonoBehaviour component that manages voxel model data and on-demand mesh generation/caching.

**Inspector Variables:**
- Reference to a `.vox` asset (`VoxAsset`)
- Array of palette textures (`extraPalettes`)

**Functionality:**
- Parses `.vox` data once and keeps `frames` and `palette` in memory
- Generates meshes on-demand per frame and palette, caches results by `(paletteName, frame, scale)`

**Key Methods:**

#### `RegisterPalette(Texture2D palette) → string`
- Registers a texture-based palette by name (texture name); also adds to `extraPalettes` if not present
- Returns the palette name

#### `RemovePalette(string paletteName) → void`
- Clears cached meshes for that palette and removes any matching texture from `extraPalettes`

#### `GetMesh(int frame, string paletteName = null) → Mesh`
- Returns a mesh for the frame/palette, generating and caching if needed (uses the component `scale` field)

### VoxelMeshSelector (MonoBehaviour)
A component that selects and displays specific frames and palettes from a VoxelDefinition.

**Inspector Variables:**
- Reference to a VoxelDefinition
- Frame index (int)
- Palette name (string)

**Methods:**

#### `SelectFrame(int frame) → void`
- Sets the frame index and updates the mesh

#### `SelectPalette(string paletteName) → void`
- Sets the palette name and updates the mesh

**Automatic Updates:**
- If frame or palette values change externally, the component detects the change and updates the mesh.