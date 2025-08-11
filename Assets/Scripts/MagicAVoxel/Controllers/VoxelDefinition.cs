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
    public enum MeshingMode
    {
        Cubic = 0,
        MarchingCubes = 1
    }

    [Header("Generation Settings")]
    [Tooltip("Scale applied when generating meshes (1.0 = 1 unit per voxel)")]
    public float scale = 1f;
    [Tooltip("Meshing algorithm used to generate the surface")] 
    public MeshingMode meshingMode = MeshingMode.Cubic;

    [Header("Marching Cubes Settings")]
    [Range(0f, 1f)]
    [Tooltip("Isovalue threshold for surface extraction (0..1). Lower pulls the surface outward.")]
    public float mcIsoValue = 0.25f;

    [Min(0)]
    [Tooltip("Padding cells of empty space around the volume to ensure boundary faces generate.")]
    public int mcPadding = 1;

    [Tooltip("How vertex colors are chosen per cube.")]
    public MarchingCubesColorMode mcColorMode = MarchingCubesColorMode.Dominant;

    [Header("Smoothing (Normals)")]
    [Tooltip("When enabled, averages vertex normals across shared positions for smoother lighting.")]
    public bool smoothNormals = false;
    [Range(0f, 1f)]
    [Tooltip("Blend between original (0) and averaged (1) normals.")]
    public float smoothNormalsStrength = 1f;
    [Min(0f)]
    [Tooltip("World-space tolerance for considering vertices at the same position.")]
    public float smoothNormalsEpsilon = 0.0001f;
    // Removed smoothing support
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

    // Internal cache for mesh data per palette, frame, scale, meshing mode, and smoothness
    // Key: (paletteName, frameIndex, scale, mode, smoothness), Value: Mesh
    private Dictionary<(string, int, float, int, float), Mesh> _meshCache = new Dictionary<(string, int, float, int, float), Mesh>();
    
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
        
        var keysToRemove = new List<(string, int, float, int, float)>();
        
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
        
        // Include options in cache key so toggling regenerates meshes
        float mcHash = (meshingMode == MeshingMode.MarchingCubes) ? (mcIsoValue * 10f + mcPadding + (int)mcColorMode * 0.01f) : 0f;
        float smoothHash = smoothNormals ? (1f + smoothNormalsStrength * 0.1f + Mathf.Clamp(smoothNormalsEpsilon, 0f, 1f) * 0.001f) : 0f;
        float optionsHash = mcHash + smoothHash;
        var key = (paletteName, frame, scale, (int)meshingMode, optionsHash);
        
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
            if (meshingMode == MeshingMode.Cubic)
            {
                mesh = VoxTools.GenerateMesh(_cachedVoxData.frames[frame], palette, effectiveScale);
            }
            else
            {
                var opts = new MarchingCubesOptions
                {
                    isoValue = Mathf.Clamp01(mcIsoValue),
                    padding = Mathf.Max(0, mcPadding),
                    colorMode = mcColorMode
                };
                mesh = MarchingCubes.GenerateMesh(_cachedVoxData.frames[frame], palette, effectiveScale, opts);
            }
            // Optional smoothing pass on normals
            if (mesh != null && smoothNormals)
            {
                ApplySmoothNormals(mesh, Mathf.Max(0f, smoothNormalsEpsilon), Mathf.Clamp01(smoothNormalsStrength));
            }
            if (mesh != null)
            {
                mesh.name = $"VoxelMesh_{voxAsset.name}_{paletteName}_{frame}_{meshingMode}_s{effectiveScale}_iso{mcIsoValue:0.00}_pad{mcPadding}_cm{mcColorMode}_sm{(smoothNormals?1:0)}_{smoothNormalsStrength:0.00}";
                var newKey = (paletteName, frame, scale, (int)meshingMode, optionsHash);
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
    private static void ApplySmoothNormals(Mesh mesh, float epsilon, float strength)
    {
        if (mesh == null || mesh.vertexCount == 0) return;

        var vertices = mesh.vertices;
        var normals = new Vector3[mesh.vertexCount];
        mesh.RecalculateNormals();
        mesh.GetNormals(new List<Vector3>(normals));
        // The above method signature is awkward; use direct GetNormals into a list then copy
        var normalList = new List<Vector3>(mesh.vertexCount);
        mesh.GetNormals(normalList);
        for (int i = 0; i < mesh.vertexCount; i++) normals[i] = normalList[i];

        // Group by position with tolerance
        var groups = new Dictionary<(int,int,int), List<int>>();
        float inv = epsilon > 0f ? 1f / epsilon : 1e6f;
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 v = vertices[i];
            int gx = Mathf.RoundToInt(v.x * inv);
            int gy = Mathf.RoundToInt(v.y * inv);
            int gz = Mathf.RoundToInt(v.z * inv);
            var key = (gx, gy, gz);
            if (!groups.TryGetValue(key, out var list))
            {
                list = new List<int>();
                groups[key] = list;
            }
            list.Add(i);
        }

        // Average within each group
        var outNormals = new Vector3[normals.Length];
        foreach (var kv in groups)
        {
            var list = kv.Value;
            Vector3 avg = Vector3.zero;
            for (int i = 0; i < list.Count; i++) avg += normals[list[i]];
            if (avg.sqrMagnitude > 1e-12f) avg.Normalize();
            for (int i = 0; i < list.Count; i++)
            {
                int idx = list[i];
                outNormals[idx] = Vector3.Slerp(normals[idx], avg, strength);
            }
        }

        mesh.SetNormals(new List<Vector3>(outNormals));
    }
}

