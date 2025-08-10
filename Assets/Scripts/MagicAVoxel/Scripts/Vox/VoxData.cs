using UnityEngine;

[System.Serializable]
public class VoxData
{
    public VoxPalette palette;
    public VoxModel[] frames;
    
    public VoxData()
    {
        palette = new VoxPalette();
        frames = new VoxModel[0];
    }
    
    public VoxData(VoxPalette palette, VoxModel[] frames)
    {
        this.palette = palette ?? new VoxPalette();
        this.frames = frames ?? new VoxModel[0];
    }
    
    // Constructor with Color32[] for backward compatibility
    public VoxData(Color32[] paletteColors, VoxModel[] frames)
    {
        this.palette = new VoxPalette(paletteColors);
        this.frames = frames ?? new VoxModel[0];
    }
    
    // Constructor from raw .vox file data - handles parsing only
    public VoxData(byte[] rawVoxData)
    {
        if (rawVoxData == null || rawVoxData.Length == 0)
        {
            palette = new VoxPalette();
            frames = new VoxModel[0];
            return;
        }
        
        // Parse the raw vox data
        var (parsedFrames, parsedPalette) = VoxTools.ParseVoxData(rawVoxData);
        
        if (parsedFrames == null || parsedFrames.Length == 0 || parsedPalette == null)
        {
            // Parsing failed - create empty data
            palette = new VoxPalette();
            frames = new VoxModel[0];
            return;
        }
        
        // Store parsed data only - no mesh generation
        palette = parsedPalette;
        frames = parsedFrames;
    }
    
    // Copy constructor - clones another VoxData
    public VoxData(VoxData other)
    {
        if (other == null)
        {
            palette = new VoxPalette();
            frames = new VoxModel[0];
            return;
        }
        
        // Clone palette
        palette = new VoxPalette(other.palette);
        
        // Clone frames array (shallow copy - VoxModel instances are shared)
        frames = new VoxModel[other.frames.Length];
        System.Array.Copy(other.frames, frames, other.frames.Length);
    }
} 