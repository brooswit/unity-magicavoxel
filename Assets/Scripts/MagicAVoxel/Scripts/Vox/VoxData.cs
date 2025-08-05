using UnityEngine;

[System.Serializable]
public class VoxData
{
    public VoxPalette palette;
    public VoxModel[] models;
    
    public VoxData()
    {
        palette = new VoxPalette();
        models = new VoxModel[0];
    }
    
    public VoxData(VoxPalette palette, VoxModel[] models)
    {
        this.palette = palette ?? new VoxPalette();
        this.models = models ?? new VoxModel[0];
    }
    
    // Constructor with Color32[] for backward compatibility
    public VoxData(Color32[] paletteColors, VoxModel[] models)
    {
        this.palette = new VoxPalette(paletteColors);
        this.models = models ?? new VoxModel[0];
    }
    
    // Constructor from raw .vox file data - handles parsing only
    public VoxData(byte[] rawVoxData)
    {
        if (rawVoxData == null || rawVoxData.Length == 0)
        {
            palette = new VoxPalette();
            models = new VoxModel[0];
            return;
        }
        
        // Parse the raw vox data
        var (parsedModels, parsedPalette) = VoxTools.ParseVoxData(rawVoxData);
        
        if (parsedModels == null || parsedModels.Length == 0 || parsedPalette == null)
        {
            // Parsing failed - create empty data
            palette = new VoxPalette();
            models = new VoxModel[0];
            return;
        }
        
        // Store parsed data only - no mesh generation
        palette = parsedPalette;
        models = parsedModels;
    }
    
    // Copy constructor - clones another VoxData
    public VoxData(VoxData other)
    {
        if (other == null)
        {
            palette = new VoxPalette();
            models = new VoxModel[0];
            return;
        }
        
        // Clone palette
        palette = new VoxPalette(other.palette);
        
        // Clone models array (shallow copy - VoxModel instances are shared)
        models = new VoxModel[other.models.Length];
        System.Array.Copy(other.models, models, other.models.Length);
    }
} 