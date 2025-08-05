using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// VoxelDefinition manages voxel model data and palette caching.
/// This is the core component that handles mesh generation and caching for different palettes.
/// </summary>
public class VoxelDefinition : MonoBehaviour
{
    [Header("Voxel Data")]
    [Tooltip("Reference to the imported .vox file")]
    public VoxAsset voxAsset;
    
    [Header("Extra Palettes")]
    [Tooltip("Array of additional palette textures")]
    public Texture2D[] extraPalettes = new Texture2D[0];
    
    // Internal cache for mesh data per palette and frame
    // Key: (paletteName, frameIndex), Value: Mesh
    private Dictionary<(string, int), Mesh> _meshCache = new Dictionary<(string, int), Mesh>();
    
    // Cache for parsed vox data to avoid repeated parsing
    private VoxData _cachedVoxData;
    private bool _dataCached = false;
    
    // Track custom palettes for cleanup
    private HashSet<string> _customPaletteNames = new HashSet<string>();
    
    void Awake()
    {
        InitializeCache();
    }
    
    void OnValidate()
    {
        // Clear cache when asset or palettes change
        ClearAllCaches();
        InitializeCache();
    }
    
    private void InitializeCache()
    {
        if (voxAsset?.rawData == null) return;
        
        var voxData = GetParsedData();
        if (voxData?.models == null) return;
        
        // Generate meshes for default palette
        GenerateDefaultPaletteMeshes(voxData);
        
        // Generate meshes for extra palettes
        GenerateExtraPaletteMeshes(voxData);
    }
    
    private VoxData GetParsedData()
    {
        if (voxAsset?.rawData == null) return null;
        
        if (!_dataCached || _cachedVoxData == null)
        {
            _cachedVoxData = new VoxData(voxAsset.rawData);
            _dataCached = true;
        }
        
        return _cachedVoxData;
    }
    
    private void GenerateDefaultPaletteMeshes(VoxData voxData)
    {
        string paletteName = "default";
        
        for (int frameIndex = 0; frameIndex < voxData.models.Length; frameIndex++)
        {
            var key = (paletteName, frameIndex);
            if (!_meshCache.ContainsKey(key))
            {
                var mesh = VoxTools.GenerateMesh(voxData.models[frameIndex], voxData.palette);
                mesh.name = $"VoxelMesh_{voxAsset.name}_{paletteName}_{frameIndex}";
                _meshCache[key] = mesh;
            }
        }
    }
    
    private void GenerateExtraPaletteMeshes(VoxData voxData)
    {
        for (int i = 0; i < extraPalettes.Length; i++)
        {
            var paletteTexture = extraPalettes[i];
            if (paletteTexture == null) continue;
            
            string paletteName = paletteTexture.name;
            var palette = CreatePaletteFromTexture(paletteTexture);
            
            for (int frameIndex = 0; frameIndex < voxData.models.Length; frameIndex++)
            {
                var key = (paletteName, frameIndex);
                if (!_meshCache.ContainsKey(key))
                {
                    var mesh = VoxTools.GenerateMesh(voxData.models[frameIndex], palette);
                    mesh.name = $"VoxelMesh_{voxAsset.name}_{paletteName}_{frameIndex}";
                    _meshCache[key] = mesh;
                }
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
        
        var voxData = GetParsedData();
        if (voxData?.models == null)
        {
            Debug.LogError("VoxelDefinition: No vox data available");
            return string.Empty;
        }
        
        string paletteName = palette.name;
        var voxPalette = CreatePaletteFromTexture(palette);
        
        // Generate and cache meshes for all frames
        for (int frameIndex = 0; frameIndex < voxData.models.Length; frameIndex++)
        {
            var key = (paletteName, frameIndex);
            var mesh = VoxTools.GenerateMesh(voxData.models[frameIndex], voxPalette);
            mesh.name = $"VoxelMesh_{voxAsset.name}_{paletteName}_{frameIndex}";
            _meshCache[key] = mesh;
        }
        
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
        
        var voxData = GetParsedData();
        if (voxData?.models == null)
        {
            Debug.LogError("VoxelDefinition: No vox data available");
            return string.Empty;
        }
        
        // Generate palette name
        string paletteName = string.IsNullOrEmpty(name) ? Guid.NewGuid().ToString() : name;
        
        // Track as custom palette
        _customPaletteNames.Add(paletteName);
        
        // Create custom palette by starting with base palette and applying overrides
        var customPalette = new VoxPalette(voxData.palette);
        foreach (var kvp in colorOverrides)
        {
            if (kvp.Key >= 0 && kvp.Key < 256)
            {
                customPalette[kvp.Key] = kvp.Value;
            }
        }
        
        // Generate and cache meshes for all frames
        for (int frameIndex = 0; frameIndex < voxData.models.Length; frameIndex++)
        {
            var key = (paletteName, frameIndex);
            var mesh = VoxTools.GenerateMesh(voxData.models[frameIndex], customPalette);
            mesh.name = $"VoxelMesh_{voxAsset.name}_{paletteName}_{frameIndex}";
            _meshCache[key] = mesh;
        }
        
        return paletteName;
    }
    
    /// <summary>
    /// Clears cached model frames for the specified palette.
    /// </summary>
    /// <param name="paletteName">Name of the palette to clear</param>
    public void ClearPalette(string paletteName)
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
        var voxData = GetParsedData();
        return voxData?.models?.Length ?? 0;
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
    
    private void ClearAllCaches()
    {
        foreach (var mesh in _meshCache.Values)
        {
            if (mesh != null)
                DestroyImmediate(mesh);
        }
        _meshCache.Clear();
        _customPaletteNames.Clear();
        _dataCached = false;
        _cachedVoxData = null;
    }
    
    private VoxPalette CreatePaletteFromTexture(Texture2D texture)
    {
        // This method should convert a Texture2D to VoxPalette
        // Implementation depends on how VoxPalette works
        var palette = new VoxPalette();
        
        // Assuming the texture is 16x16 with 256 colors arranged in a grid
        Color32[] pixels = texture.GetPixels32();
        for (int i = 0; i < Mathf.Min(pixels.Length, 256); i++)
        {
            palette[i] = pixels[i];
        }
        
        return palette;
    }
    
    void OnDestroy()
    {
        ClearAllCaches();
    }
} 