using System.Collections.Generic;
using UnityEngine;

// Static cache for runtime-generated palette textures
public static class VoxPaletteCache
{
    private static Dictionary<string, Texture2D> _paletteCache = new Dictionary<string, Texture2D>();
    
    public static Texture2D GetOrCreatePalette(string paletteName, VoxPalette basePalette, Dictionary<int, Color32> colorOverrides)
    {
        // Check cache first
        if (_paletteCache.TryGetValue(paletteName, out var cachedTexture) && cachedTexture != null)
            return cachedTexture;
        
        // Create new palette texture
        var paletteTexture = CreatePaletteTexture(basePalette, colorOverrides);
        paletteTexture.name = paletteName;
        
        // Cache and return
        _paletteCache[paletteName] = paletteTexture;
        return paletteTexture;
    }
    
    private static Texture2D CreatePaletteTexture(VoxPalette basePalette, Dictionary<int, Color32> colorOverrides)
    {
        // Create 16x16 texture for easy visual editing if needed
        var texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        
        // Start with base palette colors
        var colors = new Color32[256];
        for (int i = 0; i < 256; i++)
        {
            colors[i] = basePalette[i];
        }
        
        // Apply overrides
        foreach (var kvp in colorOverrides)
        {
            if (kvp.Key >= 0 && kvp.Key < 256)
                colors[kvp.Key] = kvp.Value;
        }
        
        // Set texture pixels (16x16 = 256 pixels)
        texture.SetPixels32(colors);
        texture.Apply();
        
        return texture;
    }
    
    public static bool HasPalette(string paletteName)
    {
        return _paletteCache.ContainsKey(paletteName);
    }
    
    public static void RemovePalette(string paletteName)
    {
        if (_paletteCache.TryGetValue(paletteName, out var texture))
        {
            if (texture != null) Object.DestroyImmediate(texture);
            _paletteCache.Remove(paletteName);
        }
    }
    
    public static void ClearCache()
    {
        foreach (var texture in _paletteCache.Values)
            if (texture != null) Object.DestroyImmediate(texture);
        _paletteCache.Clear();
    }
    
    // Debug info
    public static int CacheSize => _paletteCache.Count;
    
    public static void LogCacheStats()
    {
        Debug.Log($"VoxPaletteCache: {_paletteCache.Count} cached palettes");
    }
} 