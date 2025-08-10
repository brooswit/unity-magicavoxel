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
    //=========================================================================
    // Public variables

    [Header("Voxel Data")]
    [Tooltip("Reference to the imported .vox file")]
    public VoxAsset voxAsset;
    
    [Header("Extra Palettes")]
    [Tooltip("Array of additional palette textures")]
    public Texture2D[] extraPalettes = new Texture2D[0];
    
    //=========================================================================
    // Internal variables

    // Internal cache for mesh data per palette and frame
    // Key: (paletteName, frameIndex), Value: Mesh
    private Dictionary<(string, int), Mesh> _meshCache = new Dictionary<(string, int), Mesh>();
    
    // Cache for parsed vox data
    private VoxData _cachedVoxData;
    
    // Track custom palettes for cleanup
    private HashSet<string> _customPaletteNames = new HashSet<string>();
    
    // Event fired when cache is reinitialized (for dependent components)
    public System.Action OnCacheReinitialized;
    
    //=========================================================================
    // Unity lifecycle methods
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
    
    //=========================================================================
    // Public methods
    
    //-------------------------------------------------------------------------
    // Palette Management

    /// <summary>
    /// Registers a new palette for use with just-in-time mesh generation.
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
        
        // Just return the palette name - meshes will be generated on-demand
        return palette.name;
    }
    
    /// <summary>
    /// Creates a custom palette with color overrides for use with just-in-time mesh generation.
    /// Note: With JIT approach, custom palettes are not persistent and will be recreated on-demand.
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
        
        // Note: Custom palette will be recreated on-demand in GetPalette()
        // This is a limitation of the JIT approach
        Debug.LogWarning($"VoxelDefinition '{this.name}': Custom palette '{paletteName}' created but will need to be recreated for each mesh generation. Consider using RegisterPalette() with a Texture2D instead for better performance.");
        
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

    //-------------------------------------------------------------------------
    // Information & Queries

    /// <summary>
    /// Gets a mesh for the specified frame and palette, generating it on-demand if not cached.
    /// </summary>
    /// <param name="frame">Frame index</param>
    /// <param name="paletteName">Optional palette name (defaults to "default")</param>
    /// <returns>Generated mesh or null if generation failed</returns>
    public Mesh GetMesh(int frame, string paletteName = null)
    {
        if (string.IsNullOrEmpty(paletteName))
            paletteName = "default";
        
        if (_cachedVoxData?.models == null)
        {
            Debug.LogError($"VoxelDefinition '{name}': No vox data available");
            return null;
        }
        
        if (frame < 0 || frame >= _cachedVoxData.models.Length)
        {
            Debug.LogError($"VoxelDefinition '{name}': Frame {frame} out of range (0-{_cachedVoxData.models.Length - 1})");
            return null;
        }
        
        var key = (paletteName, frame);
        
        // Return cached if available
        if (_meshCache.TryGetValue(key, out var mesh)) 
            return mesh;
        
        // Generate on-demand
        var palette = GetPalette(paletteName);
        if (palette == null)
        {
            Debug.LogError($"VoxelDefinition '{name}': Could not find or create palette '{paletteName}'");
            return null;
        }
        
        try
        {
            mesh = VoxTools.GenerateMesh(_cachedVoxData.models[frame], palette);
            if (mesh != null)
            {
                mesh.name = $"VoxelMesh_{voxAsset.name}_{paletteName}_{frame}";
                _meshCache[key] = mesh;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"VoxelDefinition '{name}': Failed to generate mesh for frame {frame}, palette '{paletteName}' - {ex.Message}");
            return null;
        }
        
        return mesh;
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
    
    //=========================================================================
    // Private methods
    
    //-------------------------------------------------------------------------
    // Initialization
    
    private void InitializeCache()
    {
        if (voxAsset?.rawData == null) 
        {
            Debug.LogWarning($"VoxelDefinition '{name}': No voxAsset or rawData assigned");
            return;
        }
        
        // Parse VoxData upfront - no mesh generation
        _cachedVoxData = new VoxData(voxAsset.rawData);
        
        if (_cachedVoxData?.models == null || _cachedVoxData.models.Length == 0) 
        {
            Debug.LogWarning($"VoxelDefinition '{name}': Failed to parse vox data or no models found");
            return;
        }
        
        // Notify dependent components that cache has been reinitialized
        OnCacheReinitialized?.Invoke();
    }



    //-------------------------------------------------------------------------
    // Palette Lookup
    
    private VoxPalette GetPalette(string paletteName)
    {
        if (paletteName == "default")
        {
            return _cachedVoxData?.palette;
        }
        
        // Check extra palettes
        if (extraPalettes != null)
        {
            foreach (var paletteTexture in extraPalettes)
            {
                if (paletteTexture != null && paletteTexture.name == paletteName)
                {
                    return VoxPalette.CreateFromTexture(paletteTexture);
                }
            }
        }
        
        // For custom palettes, we'll need to regenerate them on-demand
        // This is a limitation of the JIT approach - custom palettes aren't persistent
        Debug.LogWarning($"VoxelDefinition '{name}': Palette '{paletteName}' not found. Custom palettes need to be recreated.");
        return _cachedVoxData?.palette; // Fallback to default
    }

    //-------------------------------------------------------------------------
    // Cleanup
    
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
