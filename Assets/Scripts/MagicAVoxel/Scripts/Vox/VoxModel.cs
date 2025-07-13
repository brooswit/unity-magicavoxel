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
        int x = Mathf.RoundToInt(position.x);
        int y = Mathf.RoundToInt(position.y);
        int z = Mathf.RoundToInt(position.z);
        
        // Check bounds
        if (x < 0 || x >= sizeX || y < 0 || y >= sizeY || z < 0 || z >= sizeZ)
        {
            return -1; // Out of bounds
        }
        
        // Convert 3D coordinates to 1D index
        return x + y * sizeX + z * sizeX * sizeY;
    }
} 