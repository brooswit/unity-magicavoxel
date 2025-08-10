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

    // Internal cache for mesh data per palette, frame and voxelsPerUnit
    // Key: (paletteName, frameIndex, voxelsPerUnit), Value: Mesh
    private Dictionary<(string, int, float), Mesh> _meshCache = new Dictionary<(string, int, float), Mesh>();
    
    // Palettes are sourced from default data and serialized extraPalettes
    
    // Cache for parsed vox data
    private VoxData _cachedVoxData;
    
    // Custom palettes are no longer supported; use RegisterPalette(Texture2D)
    
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
    /// Stores the palette in the registry so GetPalette can retrieve it.
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
        
        string paletteName = string.IsNullOrEmpty(palette.name) ? Guid.NewGuid().ToString() : palette.name;

        // Ensure it's also visible/serializable via extraPalettes if not already present
        bool exists = false;
        if (extraPalettes != null)
        {
            for (int i = 0; i < extraPalettes.Length; i++)
            {
                if (extraPalettes[i] != null && extraPalettes[i].name == paletteName)
                {
                    exists = true;
                    break;
                }
            }
        }
        if (!exists)
        {
            var list = new List<Texture2D>(extraPalettes ?? Array.Empty<Texture2D>());
            list.Add(palette);
            extraPalettes = list.ToArray();
        }

        return paletteName;
    }
    
    // Custom palettes removed. Prefer creating a full Texture2D palette and calling RegisterPalette.
    
    /// <summary>
    /// Removes cached model frames for the specified palette.
    /// </summary>
    /// <param name="paletteName">Name of the palette to remove</param>
    public void RemovePalette(string paletteName)
    {
        if (string.IsNullOrEmpty(paletteName)) return;
        
        var keysToRemove = new List<(string, int, float)>();
        
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
        
        // Remove from registries
        // no registry to remove from

        // Remove from extraPalettes by name if present
        if (extraPalettes != null && extraPalettes.Length > 0)
        {
            var list = new List<Texture2D>(extraPalettes.Length);
            foreach (var tex in extraPalettes)
            {
                if (tex == null || tex.name != paletteName)
                    list.Add(tex);
            }
            extraPalettes = list.ToArray();
        }
    }

    //-------------------------------------------------------------------------
    // Information & Queries

    /// <summary>
    /// Gets a mesh for the specified frame and palette, generating it on-demand if not cached.
    /// </summary>
    /// <param name="frame">Frame index</param>
    /// <param name="paletteName">Optional palette name (defaults to "default")</param>
    /// <param name="voxelsPerUnit">Number of voxels that fit in one Unity unit (e.g., 16 => scale = 1/16)</param>
    /// <returns>Generated mesh or null if generation failed</returns>
    public Mesh GetMesh(int frame, string paletteName = null, float voxelsPerUnit = 1f)
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
        
        var key = (paletteName, frame, voxelsPerUnit);
        
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
            float effectiveScale = voxelsPerUnit > 0f ? 1f / voxelsPerUnit : 1f;
            mesh = VoxTools.GenerateMesh(_cachedVoxData.models[frame], palette, effectiveScale);
            if (mesh != null)
            {
                mesh.name = $"VoxelMesh_{voxAsset.name}_{paletteName}_{frame}_vpu{voxelsPerUnit}";
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
        
        // Not found
        Debug.LogWarning($"VoxelDefinition '{name}': Palette '{paletteName}' not found. Falling back to default.");
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
        _cachedVoxData = null;
    }
}
