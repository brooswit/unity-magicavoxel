using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Static mesh cache for alternate palette meshes
// Pre-generated meshes from VoxAsset are used directly without caching
public static class VoxelMeshCache
{
    // Cache key: (VoxAsset hash, Texture2D hash, model index) - only for alternate palettes
    private static Dictionary<(int, int, int), Mesh> _meshCache = new Dictionary<(int, int, int), Mesh>();
    
    public static Mesh GetOrCreateMesh(VoxAsset voxAsset, Texture2D paletteTexture, int modelIndex)
    {
        // If no alternate palette, use the pre-generated mesh from the asset (no caching needed)
        if (paletteTexture == null)
        {
            if (voxAsset?.data?.meshes != null && modelIndex >= 0 && modelIndex < voxAsset.data.meshes.Length)
                return voxAsset.data.meshes[modelIndex];
            return null;
        }
        
        // Alternate palette: use cache for generated meshes
        int assetHash = voxAsset != null ? voxAsset.GetHashCode() : 0;
        int textureHash = paletteTexture.GetHashCode();
        var key = (assetHash, textureHash, modelIndex);
        
        // Check cache first
        if (_meshCache.TryGetValue(key, out var cachedMesh) && cachedMesh != null)
            return cachedMesh;
        
        // Generate new mesh with alternate palette
        var alternatePalette = CreatePaletteFromTexture(paletteTexture);
        var mesh = VoxTools.GenerateMesh(voxAsset.data.models[modelIndex], alternatePalette);
        
        // Set descriptive name
        mesh.name = $"VoxelMesh_{voxAsset.name}_{paletteTexture.name}_{modelIndex}";
        
        // Cache and return
        _meshCache[key] = mesh;
        return mesh;
    }
    

    
    private static VoxPalette CreatePaletteFromTexture(Texture2D texture)
    {
        var colors = new Color32[256];
        
        // Read pixels from texture (left-to-right, top-to-bottom)
        int pixelIndex = 0;
        for (int y = texture.height - 1; y >= 0 && pixelIndex < 256; y--)
        {
            for (int x = 0; x < texture.width && pixelIndex < 256; x++)
            {
                Color32 pixel = texture.GetPixel(x, y);
                colors[pixelIndex] = pixel;
                pixelIndex++;
            }
        }
        
        // Fill remaining slots with default colors if needed
        if (pixelIndex < 256)
        {
            var defaultPalette = VoxPalette.CreateDefault();
            for (int i = pixelIndex; i < 256; i++)
            {
                colors[i] = defaultPalette[i];
            }
        }
        
        return new VoxPalette(colors);
    }
    
    // Cache management
    public static void ClearCache()
    {
        foreach (var mesh in _meshCache.Values)
            if (mesh != null) Object.DestroyImmediate(mesh);
        _meshCache.Clear();
    }
    
    public static void ClearAssetCache(VoxAsset voxAsset)
    {
        if (voxAsset == null) return;
        
        int assetHash = voxAsset.GetHashCode();
        var keysToRemove = _meshCache.Keys.Where(k => k.Item1 == assetHash).ToList();
        
        foreach (var key in keysToRemove)
        {
            if (_meshCache[key] != null) Object.DestroyImmediate(_meshCache[key]);
            _meshCache.Remove(key);
        }
    }
    
    public static void ClearPaletteCache(Texture2D paletteTexture)
    {
        if (paletteTexture == null) 
        {
            // No cached meshes for null palette (uses pre-generated meshes)
            return;
        }
        
        int textureHash = paletteTexture.GetHashCode();
        var keysToRemove = _meshCache.Keys.Where(k => k.Item2 == textureHash).ToList();
        
        foreach (var key in keysToRemove)
        {
            if (_meshCache[key] != null) Object.DestroyImmediate(_meshCache[key]);
            _meshCache.Remove(key);
        }
    }
    
    // Debug info
    public static int CacheSize => _meshCache.Count;
    
    public static void LogCacheStats()
    {
        Debug.Log($"VoxelMeshCache: {_meshCache.Count} cached meshes");
    }
} 