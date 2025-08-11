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
    [Header("Smoothing")]
    [Range(0f, 1f)]
    [Tooltip("Strength of normal smoothing (0=hard edges, 1=fully smooth)")]
    public float smoothStrength = 1f;
    [Range(0f, 5f)]
    [Tooltip("Radius in voxel units for blurring/averaging normals across nearby voxels (0=exact match, 0.5=half voxel radius, etc)")]
    public float smoothGroupRadius = 0f;
    [Header("Generation Settings")]
    [Tooltip("Scale applied when generating meshes (1.0 = 1 unit per voxel)")]
    public float scale = 1f;

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

    // Internal cache for mesh data per palette, frame, scale, smoothing strength and epsilon
    // Key: (paletteName, frameIndex, scale, smoothStrength, smoothEpsilon), Value: Mesh
    private Dictionary<(string, int, float, float, float), Mesh> _meshCache = new Dictionary<(string, int, float, float, float), Mesh>();
    
    // Palettes are sourced from default data and serialized extraPalettes
    
    // Cache for parsed vox data
    private VoxData _cachedVoxData;
    
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
        // Snap values to their respective increments
        smoothStrength = Mathf.Round(smoothStrength * 10f) * 0.1f;
        smoothGroupRadius = Mathf.Round(smoothGroupRadius * 4f) * 0.25f;
        
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
        
        if (_cachedVoxData?.frames == null)
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
        
        var keysToRemove = new List<(string, int, float, float, float)>();
        
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
    /// <returns>Generated mesh or null if generation failed</returns>
    public Mesh GetMesh(int frame, string paletteName = null)
    {
        if (string.IsNullOrEmpty(paletteName))
            paletteName = "default";
        
        if (_cachedVoxData?.frames == null)
        {
            Debug.LogError($"VoxelDefinition '{name}': No vox data available");
            return null;
        }
        
        if (frame < 0 || frame >= _cachedVoxData.frames.Length)
        {
            Debug.LogError($"VoxelDefinition '{name}': Frame {frame} out of range (0-{_cachedVoxData.frames.Length - 1})");
            return null;
        }
        
        // Cache key (cubic only); include smoothing strength and radius (snapped)
        float keyStrength = Mathf.Round(Mathf.Clamp01(smoothStrength) * 10f) * 0.1f; // Snap to 0.1 increments
        float keyRadius = Mathf.Round(smoothGroupRadius * 4f) * 0.25f; // Snap to 0.25 increments
        var key = (paletteName, frame, scale, keyStrength, keyRadius);
        
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
            float effectiveScale = Mathf.Max(0.0001f, scale);
            mesh = VoxTools.GenerateMesh(_cachedVoxData.frames[frame], palette, effectiveScale);
            // Apply smoothing if strength > 0
            if (mesh != null && keyStrength > 0f)
            {
                // Convert radius from voxel units to mesh units
                float meshRadius = keyRadius * effectiveScale;
                ApplySmoothNormals(mesh, meshRadius, keyStrength);
            }
            if (mesh != null)
            {
                mesh.name = $"VoxelMesh_{voxAsset.name}_{paletteName}_{frame}_Cubic_s{effectiveScale}_sm{keyStrength:0.0}_r{keyRadius:0.00}";
                var newKey = (paletteName, frame, scale, keyStrength, keyRadius);
                _meshCache[newKey] = mesh;
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
        return _cachedVoxData?.frames?.Length ?? 0;
    }
    
    /// <summary>
    /// Gets all available palette names.
    /// </summary>
    public string[] GetAvailablePalettes()
    {
        var paletteNames = new HashSet<string>();

        // Always include default if vox data is available
        if (_cachedVoxData?.palette != null)
        {
            paletteNames.Add("default");
        }

        // Include any extra palettes by name
        if (extraPalettes != null)
        {
            foreach (var tex in extraPalettes)
            {
                if (tex != null && !string.IsNullOrEmpty(tex.name))
                {
                    paletteNames.Add(tex.name);
                }
            }
        }

        // Also include any palette names already used in the cache
        foreach (var key in _meshCache.Keys)
        {
            paletteNames.Add(key.Item1);
        }

        return new List<string>(paletteNames).ToArray();
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
        
        if (_cachedVoxData?.frames == null || _cachedVoxData.frames.Length == 0) 
        {
            Debug.LogWarning($"VoxelDefinition '{name}': Failed to parse vox data or no frames found");
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

    //=========================================================================
    // Smoothing Helpers
    private static void ApplySmoothNormals(Mesh mesh, float radius, float strength)
    {
        if (mesh == null || mesh.vertexCount == 0) return;
        if (radius <= 0f || strength <= 0f)
        {
            // Nothing to do, but ensure normals exist
            mesh.RecalculateNormals();
            return;
        }

        // Read positions and base normals
        Vector3[] vertices = mesh.vertices;
        var baseNormalList = new List<Vector3>(mesh.vertexCount);
        mesh.RecalculateNormals();
        mesh.GetNormals(baseNormalList);
        Vector3[] baseNormals = baseNormalList.ToArray();

        int vertexCount = vertices.Length;

        // Spatial hash grid to accelerate neighborhood lookup
        float cellSize = Mathf.Max(1e-6f, radius);
        float invCell = 1f / cellSize;
        var grid = new Dictionary<(int,int,int), List<int>>();
        for (int i = 0; i < vertexCount; i++)
        {
            Vector3 v = vertices[i];
            int cx = Mathf.FloorToInt(v.x * invCell);
            int cy = Mathf.FloorToInt(v.y * invCell);
            int cz = Mathf.FloorToInt(v.z * invCell);
            var key = (cx, cy, cz);
            if (!grid.TryGetValue(key, out var list))
            {
                list = new List<int>(4);
                grid[key] = list;
            }
            list.Add(i);
        }

        // For each vertex, average normals of neighbors within radius
        Vector3[] outNormals = new Vector3[vertexCount];
        float radiusSqr = radius * radius;
        // Neighbor cell search extents (1 cell in each direction is sufficient since cellSize == radius)
        int range = 1;
        for (int i = 0; i < vertexCount; i++)
        {
            Vector3 p = vertices[i];
            int cx = Mathf.FloorToInt(p.x * invCell);
            int cy = Mathf.FloorToInt(p.y * invCell);
            int cz = Mathf.FloorToInt(p.z * invCell);

            Vector3 sum = Vector3.zero;
            int count = 0;
            for (int dx = -range; dx <= range; dx++)
            for (int dy = -range; dy <= range; dy++)
            for (int dz = -range; dz <= range; dz++)
            {
                var key = (cx + dx, cy + dy, cz + dz);
                if (!grid.TryGetValue(key, out var list)) continue;
                for (int li = 0; li < list.Count; li++)
                {
                    int j = list[li];
                    Vector3 q = vertices[j];
                    if ((q - p).sqrMagnitude <= radiusSqr)
                    {
                        sum += baseNormals[j];
                        count++;
                    }
                }
            }

            Vector3 avg = count > 0 ? sum / Mathf.Max(1, count) : baseNormals[i];
            if (avg.sqrMagnitude > 1e-12f) avg.Normalize();
            outNormals[i] = Vector3.Slerp(baseNormals[i], avg, strength);
        }

        // Write back
        mesh.SetNormals(new List<Vector3>(outNormals));
    }
}

