using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// VoxelDefinition manages voxel model data and palette caching.
/// This is the core component that handles mesh generation and caching for different palettes.
/// </summary>
[ExecuteInEditMode]
public class VoxelDefinition : MonoBehaviour
{
    [Header("Voxel Data")]
    [Tooltip("Reference to the imported .vox file")]
    public VoxAsset voxAsset;
    
    [Header("Extra Palettes")]
    [Tooltip("Array of additional palette textures")]
    public Texture2D[] extraPalettes = new Texture2D[0];
    
    //-------------------------------------------------------------------------
    // Internal variables
    //-------------------------------------------------------------------------
    // Internal cache for mesh data per palette and frame
    // Key: (paletteName, frameIndex), Value: Mesh
    private Dictionary<(string, int), Mesh> _meshCache = new Dictionary<(string, int), Mesh>();
    
    // Cache for parsed vox data
    private VoxData _cachedVoxData;
    
    // Track custom palettes for cleanup
    private HashSet<string> _customPaletteNames = new HashSet<string>();
    
    //-------------------------------------------------------------------------
    // Unity lifecycle methods
    //-------------------------------------------------------------------------
    void Awake()
    {
        InitializeCache();
    }
    
    void OnEnable()
    {
        InitializeCache();
    }
    
    void OnValidate()
    {
        // Clear cache when asset or palettes change
        ClearAllCaches();
        InitializeCache();
    }
    
    void OnDestroy()
    {
        ClearAllCaches();
    }
    
    private void InitializeCache()
    {
        if (voxAsset?.rawData == null) 
        {
            Debug.LogWarning($"VoxelDefinition '{name}': No voxAsset or rawData assigned");
            return;
        }
        
        // Parse VoxData upfront
        _cachedVoxData = new VoxData(voxAsset.rawData);
        
        if (_cachedVoxData?.models == null || _cachedVoxData.models.Length == 0) 
        {
            Debug.LogWarning($"VoxelDefinition '{name}': Failed to parse vox data or no models found");
            return;
        }
        
        try
        {
            GenerateDefaultPaletteMeshes();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"VoxelDefinition '{name}': Failed to generate default palette meshes - {ex.Message}");
        }
        
        try
        {
            GenerateExtraPaletteMeshes();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"VoxelDefinition '{name}': Failed to generate extra palette meshes - {ex.Message}");
        }
    }
    

    
    private void GenerateDefaultPaletteMeshes()
    {
        if (_cachedVoxData?.models == null || _cachedVoxData.palette == null) return;
        
        GenerateMeshesForAllFrames("default", _cachedVoxData.palette);
    }
    
    private void GenerateExtraPaletteMeshes()
    {
        if (_cachedVoxData?.models == null || extraPalettes == null) return;
        
        for (int i = 0; i < extraPalettes.Length; i++)
        {
            var paletteTexture = extraPalettes[i];
            if (paletteTexture == null) 
            {
                Debug.LogWarning($"VoxelDefinition '{name}': Extra palette at index {i} is null");
                continue;
            }
            
            string paletteName = string.IsNullOrEmpty(paletteTexture.name) ? $"palette_{i}" : paletteTexture.name;
            
            try
            {
                var palette = VoxPalette.CreateFromTexture(paletteTexture);
                if (palette == null) continue;
                
                GenerateMeshesForAllFrames(paletteName, palette);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"VoxelDefinition '{name}': Failed to generate meshes for palette '{paletteName}' - {ex.Message}");
            }
        }
    }
    
    /// <summary>
    /// Registers a new palette and caches meshes for all frames using that palette.
    /// </summary>
    /// <param name="palette">Image palette to register</param>
    /// <returns>Name of the registered palette (filename)</returns>
    public string RegisterPalette(Texture2D palette)
    {
        if (palette == null)
        {
            Debug.LogError("VoxelDefinition: Cannot register null palette");
            return string.Empty;
        }
        
        if (_cachedVoxData?.models == null)
        {
            Debug.LogError("VoxelDefinition: No vox data available");
            return string.Empty;
        }
        
        string paletteName = palette.name;
        var voxPalette = VoxPalette.CreateFromTexture(palette);
        
        // Generate and cache meshes for all frames
        GenerateMeshesForAllFrames(paletteName, voxPalette);
        
        return paletteName;
    }
    
    /// <summary>
    /// Creates a custom palette with color overrides and caches meshes for all frames.
    /// </summary>
    /// <param name="colorOverrides">Dictionary of palette index to color overrides</param>
    /// <param name="name">Optional name for the palette (defaults to UUID)</param>
    /// <returns>Name of the custom palette</returns>
    public string CustomPalette(Dictionary<int, Color> colorOverrides, string name = null)
    {
        if (colorOverrides == null || colorOverrides.Count == 0)
        {
            Debug.LogError("VoxelDefinition: Color overrides cannot be null or empty");
            return string.Empty;
        }
        
        if (_cachedVoxData?.models == null)
        {
            Debug.LogError("VoxelDefinition: No vox data available");
            return string.Empty;
        }
        
        // Generate palette name
        string paletteName = string.IsNullOrEmpty(name) ? Guid.NewGuid().ToString() : name;
        
        // Track as custom palette
        _customPaletteNames.Add(paletteName);
        
        // Create custom palette by starting with base palette and applying overrides
        var customPalette = new VoxPalette(_cachedVoxData.palette);
        foreach (var kvp in colorOverrides)
        {
            if (kvp.Key >= 0 && kvp.Key < 256)
            {
                customPalette[kvp.Key] = kvp.Value;
            }
        }
        
        // Generate and cache meshes for all frames
        GenerateMeshesForAllFrames(paletteName, customPalette);
        
        return paletteName;
    }
    
    /// <summary>
    /// Removes cached model frames for the specified palette.
    /// </summary>
    /// <param name="paletteName">Name of the palette to remove</param>
    public void RemovePalette(string paletteName)
    {
        if (string.IsNullOrEmpty(paletteName)) return;
        
        var keysToRemove = new List<(string, int)>();
        
        foreach (var key in _meshCache.Keys)
        {
            if (key.Item1 == paletteName)
            {
                keysToRemove.Add(key);
            }
        }
        
        foreach (var key in keysToRemove)
        {
            if (_meshCache.TryGetValue(key, out var mesh) && mesh != null)
            {
                DestroyImmediate(mesh);
            }
            _meshCache.Remove(key);
        }
        
        // Remove from custom palette tracking
        _customPaletteNames.Remove(paletteName);
    }
    
    /// <summary>
    /// Gets a cached mesh for the specified frame and palette.
    /// </summary>
    /// <param name="frame">Frame index</param>
    /// <param name="paletteName">Optional palette name (defaults to "default")</param>
    /// <returns>Cached mesh or null if not found</returns>
    public Mesh GetMesh(int frame, string paletteName = null)
    {
        if (string.IsNullOrEmpty(paletteName))
            paletteName = "default";
        
        var key = (paletteName, frame);
        return _meshCache.TryGetValue(key, out var mesh) ? mesh : null;
    }
    
    /// <summary>
    /// Gets the number of available frames in the voxel data.
    /// </summary>
    public int GetFrameCount()
    {
        return _cachedVoxData?.models?.Length ?? 0;
    }
    
    /// <summary>
    /// Gets all available palette names.
    /// </summary>
    public string[] GetAvailablePalettes()
    {
        var palettes = new HashSet<string>();
        
        foreach (var key in _meshCache.Keys)
        {
            palettes.Add(key.Item1);
        }
        
        return new List<string>(palettes).ToArray();
    }
    
    private void GenerateMeshesForAllFrames(string paletteName, VoxPalette palette)
    {
        for (int frameIndex = 0; frameIndex < _cachedVoxData.models.Length; frameIndex++)
        {
            var key = (paletteName, frameIndex);
            if (!_meshCache.ContainsKey(key))
            {
                try
                {
                    var mesh = VoxTools.GenerateMesh(_cachedVoxData.models[frameIndex], palette);
                    if (mesh != null)
                    {
                        mesh.name = $"VoxelMesh_{voxAsset.name}_{paletteName}_{frameIndex}";
                        _meshCache[key] = mesh;
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"VoxelDefinition '{name}': Failed to generate mesh for frame {frameIndex} - {ex.Message}");
                }
            }
        }
    }
    
    private void ClearAllCaches()
    {
        foreach (var mesh in _meshCache.Values)
        {
            if (mesh != null)
                DestroyImmediate(mesh);
        }
        _meshCache.Clear();
        _customPaletteNames.Clear();
        _cachedVoxData = null;
    }
} 