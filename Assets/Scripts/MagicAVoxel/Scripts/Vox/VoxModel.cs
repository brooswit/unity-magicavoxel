using UnityEngine;

[System.Serializable]
public class VoxModel
{
    public int sizeX;
    public int sizeY;
    public int sizeZ;
    
    // Voxel data as flat array (colorIndex, 0 = empty)
    [SerializeField] private byte[] voxelData;
    
    // Constructor with byte array
    public VoxModel(int sizeX, int sizeY, int sizeZ, byte[] voxelData)
    {
        this.sizeX = sizeX;
        this.sizeY = sizeY;
        this.sizeZ = sizeZ;

        this.voxelData = voxelData ?? new byte[sizeX * sizeY * sizeZ];
    }
    
    public void SetVoxel(Vector3 position, byte colorIndex)
    {
        int index = GetArrayIndex(position);
        if (index >= 0 && index < voxelData.Length)
        {
            voxelData[index] = colorIndex;
        }
    }
    
    public byte GetVoxel(Vector3 position)
    {
        int index = GetArrayIndex(position);
        if (index >= 0 && index < voxelData.Length)
        {
            return voxelData[index];
        }
        return 0; // Empty voxel
    }
    
    public bool HasVoxel(Vector3 position)
    {
        return GetVoxel(position) != 0;
    }
    
    private int GetArrayIndex(Vector3 position)
    {
        // The position comes from mesh generation with coordinate transformation:
        // mesh generation uses: pos = new Vector3(x, z, -y)
        // So we need to reverse this transformation to get original voxel coordinates
        
        int origX = Mathf.RoundToInt(position.x);           // x stays x
        int origY = Mathf.RoundToInt(-position.z);          // -z becomes y  
        int origZ = Mathf.RoundToInt(position.y);           // y becomes z
        
        // Check bounds using original coordinate system
        if (origX < 0 || origX >= sizeX || origY < 0 || origY >= sizeY || origZ < 0 || origZ >= sizeZ)
        {
            return -1; // Out of bounds
        }
        
        // Convert 3D coordinates to 1D index using original .vox layout
        return origX + origY * sizeX + origZ * sizeX * sizeY;
    }
} 