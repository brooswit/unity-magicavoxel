# Unity MagicaVoxel

Dynamically load MagicaVoxel .vox models in real-time within Unity3D.

## Architecture Overview

### Voxel File Loader
Responsible for holding the raw voxel data from .vox files.

### VoxelDefinition (MonoBehaviour)
A MonoBehaviour component that manages voxel model data and palette caching.

**Inspector Variables:**
- Public variable referencing a voxel file
- Array of images (extra palettes)

**Functionality:**
- Generates and caches meshes for all model frames using the default palette from voxel data
- For each image palette in the array, caches meshes for all model frames using that palette (named `default`)

**Methods:**

#### `registerPalette(Image palette) → string`
- Takes an image palette as input
- Caches meshes for all model frames using the provided palette
- Returns the palette name (filename of the palette)

#### `customPalette(Dictionary<int, Color> overrides, string name = null) → string`
- Takes a dictionary of palette index/color pairs as overrides to the default palette
- Optional name parameter (defaults to UUID if not provided)
- Caches meshes for all model frames using the custom palette
- Returns the custom palette name

#### `clearPalette(string paletteName) → void`
- Takes a palette name as input
- Clears the cached model frames for the specified palette

#### `getMesh(int frame, string paletteName = null) → Mesh`
- Takes a frame index and optional palette name
- Returns the cached mesh for the specified frame and palette

### VoxelMeshSelector (MonoBehaviour)
A component that selects and displays specific frames and palettes from a VoxelDefinition.

**Inspector Variables:**
- Reference to a VoxelDefinition
- Frame index (int)
- Palette name (string)

**Methods:**

#### `updateMesh()` (private)
- Calls `getMesh()` on the referenced VoxelDefinition
- Assigns the returned mesh to the GameObject's mesh renderer and collider

#### `selectFrame(int frame) → void`
- Updates the frame variable
- Calls `updateMesh()`

#### `selectPalette(string paletteName) → void`
- Updates the palette variable
- Calls `updateMesh()`

#### `customPalette(Dictionary<int, Color> overrides, string name = null) → string`
- Calls the VoxelDefinition's `customPalette()` method
- Tracks created custom palettes for cleanup
- When the VoxelMeshSelector is destroyed, automatically calls `clearPalette()` for all custom palettes created by this component

**Automatic Updates:**
- If frame or palette values are changed outside of the `selectFrame()` or `selectPalette()` methods, the component automatically calls the appropriate method with the new values to maintain synchronization.