# CHANGELOG
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
- Add: ClearPalette method for selective cache cleanup
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
