# CHANGELOG
## 1.1.1
### Stability and URP fixes
## 1.2.0
### Feature: Marching Cubes and Smoothness
- Add Marching Cubes meshing option with adjustable `smoothness` (0..1) on `VoxelDefinition`
- Extend mesh cache key to include meshing mode and smoothness
- Update README with new inspector options and behavior
- Fix compile error in `VoxelDefinition.RegisterPalette` (use `_cachedVoxData.frames`)
- Guard editor-only API in `VoxelMeshSelector.OnValidate` with `#if UNITY_EDITOR`
- Prevent event subscription leaks when switching `VoxelDefinition` references
- Correct mesh pivot centering in `VoxTools.GenerateMesh` (remove ad-hoc +1 and sign flip)
- Update shader macro to `#if defined(_CLUSTER_LIGHT_LOOP)` in `VoxelURPVertexColor`
- Improve `VoxAutoReload` to only reload when the changed `.vox` matches the component's asset
- Raise reimport event in `VoxImporter` (`VoxAsset.RaiseReimported`)
- Update `GetAvailablePalettes` to include `default` and names from `extraPalettes`
- Sync README with current on-demand mesh generation and palette API
## 1.1.0
### Simplify Shaders and Finalize URP Lighting
- Remove legacy shaders and tests; keep a single, clean shader: `Custom/VoxelURPVertexColor`
- Clean URP shader: vertex colors, ambient, main light, additional lights; no boosts
- Fix default material assignment to use `Custom/VoxelURPVertexColor`
- Update URP asset settings to ensure additional lights compile and render
  - `m_AdditionalLightsRenderingMode: 2` (Per Pixel)
  - `m_AdditionalLightsPerObjectLimit: 8`
  - `m_PrefilteringModeAdditionalLight: 0` (keep variants)
- Remove debug and experimental shaders to avoid pink shader confusion
- Scene material reference updated away from `Custom/VertexColorShader`
## 1.0.0
### 🎉 MAJOR RELEASE: Fully Functional Voxel Rendering System
- **CRITICAL FIX**: Resolved coordinate transformation bug causing single-layer voxel rendering
  - Fixed VoxFrame.GetArrayIndex to properly reverse coordinate transformation
  - Mesh generation now renders complete 3D models instead of thin slices
  - Coordinate system properly maps between .vox storage and Unity rendering
- **FIX**: Resolved CS0102 compiler error by renaming UpdateCollider method to UpdateMeshCollider
  - Eliminated naming conflict between UpdateCollider property and method
  - Clear semantic distinction between control setting and operation
- **FIX**: Resolved SendMessage restrictions in OnValidate by deferring mesh updates
  - Smart deferral using EditorApplication.delayCall in editor mode
  - Immediate updates during runtime for responsive gameplay
  - Includes safety checks to prevent errors on destroyed objects
- **FIX**: Updated deprecated FindObjectsOfType to modern FindObjectsByType API
  - Uses FindObjectsSortMode.None for optimal performance
  - Eliminates deprecation warnings in Unity 2023+
- **FIX**: Resolved VoxAutoReload compilation errors with proper Unity API usage
  - Uses reflection to safely call private cache management methods
  - Proper Object.FindObjectsByType usage with modern Unity versions
- **FIX**: Corrected VoxTools palette index range check (255 vs 256)
  - Removes compiler warning about useless integral constant comparison
- **IMPROVE**: Enhanced Rigidbody detection for automatic collider convex setting
  - Dynamic detection of Rigidbody component addition/removal
  - Automatic mesh collider convex property synchronization
- **IMPROVE**: Simplified VoxAutoReload by leveraging event system architecture
  - Removed redundant VoxelMeshSelector handling
  - Relies on OnCacheReinitialized events for automatic updates
- **IMPROVE**: Code organization following consistent patterns across components
  - Standardized section headers and method grouping
  - Clear separation between public/private methods and logical subsections

## 0.0.5
### Enhance New Architecture with Missing Features
- Add: [ExecuteInEditMode] support for real-time editor updates
- Add: Enhanced collider management with optional flags and Rigidbody detection
- Add: Automatic material assignment with shader fallback hierarchy
- Add: Comprehensive error handling and null safety throughout system
- Add: Texture-based palette generation for cross-system compatibility
- Add: Cache management tools (GetCacheStats, LogCacheStats, ClearFrame, RefreshCache)
- Add: Public properties for collider settings in VoxelMeshSelector
- Add: OnEnable lifecycle method for better component initialization
- Improve: Better material naming and shader detection
- Improve: Detailed logging for debugging and error tracking

## 0.0.4
### Implement New Architecture Based on README Specification
- Add: VoxelDefinition MonoBehaviour for centralized voxel data and palette management
- Add: VoxelMeshSelector MonoBehaviour for frame and palette selection
- Add: Comprehensive mesh caching system for different palettes and frames
- Add: RegisterPalette method for image-based palette registration
- Add: CustomPalette method with dictionary-based color overrides
- Add: RemovePalette method for selective cache cleanup
- Add: GetMesh method for cached mesh retrieval
- Add: Automatic frame/palette synchronization in VoxelMeshSelector
- Add: Custom palette cleanup on component destruction
- Add: Frame cycling and palette switching utilities
- Update: Cleaned up README.md with proper formatting and documentation

## 0.0.3
### Add support for alternate palettes
- Support for designating a PNG as the alternate palette
- Support for dynamicaly changing to different palette
## 0.0.2
### Add support for loading all model in vox file
- Support loading all models in the file
- Cache mesh and voxel data in asset 
## 0.0.1
### Initial Commit
- Add: Initial code
