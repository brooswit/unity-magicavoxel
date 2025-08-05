using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Static mesh cache for just-in-time generated meshes
public static class VoxMeshCache
{
    // Cache key: (VoxAsset hash, Texture2D hash, model index)
    private static Dictionary<(int, int, int), Mesh> _meshCache = new Dictionary<(int, int, int), Mesh>();
    
    // Cache for parsed VoxData to avoid re-parsing the same asset
    private static Dictionary<int, VoxData> _parsedDataCache = new Dictionary<int, VoxData>();
    
    public static Mesh GetOrCreateMesh(VoxAsset voxAsset, Texture2D paletteTexture, int modelIndex)
    {
        if (voxAsset?.rawData == null)
            return null;
        
        // Parse the vox data if not already cached
        var voxData = GetParsedData(voxAsset);
        if (voxData?.models == null || modelIndex < 0 || modelIndex >= voxData.models.Length)
            return null;
        
        // Generate cache key based on asset and palette
        int assetHash = voxAsset.GetHashCode();
        int textureHash = paletteTexture != null ? paletteTexture.GetHashCode() : 0;
        var key = (assetHash, textureHash, modelIndex);
        
        // Check cache first
        if (_meshCache.TryGetValue(key, out var cachedMesh) && cachedMesh != null)
            return cachedMesh;
        
        // Generate new mesh just-in-time
        VoxPalette palette;
        string meshName;
        
        if (paletteTexture == null)
        {
            // Use original palette from asset
            palette = voxData.palette;
            meshName = $"VoxelMesh_{voxAsset.name}_{modelIndex}";
        }
        else
        {
            // Use alternate palette from texture
            palette = CreatePaletteFromTexture(paletteTexture);
            meshName = $"VoxelMesh_{voxAsset.name}_{paletteTexture.name}_{modelIndex}";
        }
        
        var mesh = VoxTools.GenerateMesh(voxData.models[modelIndex], palette);
        mesh.name = meshName;
        
        // Cache and return
        _meshCache[key] = mesh;
        return mesh;
    }

    private static VoxData GetParsedData(VoxAsset voxAsset)
    {
        int assetHash = voxAsset.GetHashCode();
        
        if (_parsedDataCache.TryGetValue(assetHash, out var cachedData))
            return cachedData;
        
        var voxData = new VoxData(voxAsset.rawData);
        _parsedDataCache[assetHash] = voxData;
        return voxData;
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
        _parsedDataCache.Clear();
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
        
        // Also clear parsed data cache for this asset
        _parsedDataCache.Remove(assetHash);
    }
    
    public static void ClearPaletteCache(Texture2D paletteTexture)
    {
        int textureHash = paletteTexture != null ? paletteTexture.GetHashCode() : 0;
        var keysToRemove = _meshCache.Keys.Where(k => k.Item2 == textureHash).ToList();
        
        foreach (var key in keysToRemove)
        {
            if (_meshCache[key] != null) Object.DestroyImmediate(_meshCache[key]);
            _meshCache.Remove(key);
        }
    }
    
    // Debug info
    public static int CacheSize => _meshCache.Count;
    public static int ParsedDataCacheSize => _parsedDataCache.Count;
    
    public static void LogCacheStats()
    {
        Debug.Log($"VoxMeshCache: {_meshCache.Count} cached meshes, {_parsedDataCache.Count} parsed data entries");
    }
} 