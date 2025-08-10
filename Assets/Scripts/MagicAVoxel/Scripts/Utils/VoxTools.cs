using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class VoxTools
{
    public static (VoxFrame[] frames, VoxPalette palette) ParseVoxData(byte[] rawVoxData)
    {
        // Storage for multiple frames
        var frames = new List<VoxFrame>();
        var tempPalette = VoxPalette.CreateDefault();
        
        // Current model being parsed
        int currentSizeX = 0, currentSizeY = 0, currentSizeZ = 0;
        byte[] currentVoxels = null;
        
        using (MemoryStream ms = new MemoryStream(rawVoxData))
        using (BinaryReader reader = new BinaryReader(ms))
        {
            string magic = new string(reader.ReadChars(4)); // Should be 'VOX '
            if (magic != "VOX ")
            {
                Debug.LogError("Invalid .vox file header.");
                return (null, null);
            }

            int version = reader.ReadInt32();

            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                string chunkId = new string(reader.ReadChars(4));
                int contentSize = reader.ReadInt32();
                int childrenSize = reader.ReadInt32();

                long chunkStart = reader.BaseStream.Position;

                switch (chunkId)
                {
                    case "MAIN":
                        break;

                    case "SIZE":
                        // Read and store model size
                        currentSizeX = reader.ReadInt32();
                        currentSizeY = reader.ReadInt32();
                        currentSizeZ = reader.ReadInt32();
                        // Initialize voxel array for this model
                        currentVoxels = new byte[currentSizeX * currentSizeY * currentSizeZ];
                        break;

                    case "XYZI":
                        if (currentVoxels != null)
                        {
                            ReadXYZIChunk(reader, currentVoxels, currentSizeX, currentSizeY, currentSizeZ);
                            // Create VoxModel and add to collection
                            var model = new VoxFrame(currentSizeX, currentSizeY, currentSizeZ, currentVoxels);
                            frames.Add(model);
                            
                            // Reset for next model
                            currentVoxels = null;
                            currentSizeX = 0;
                            currentSizeY = 0;
                            currentSizeZ = 0;
                        }
                        else
                        {
                            Debug.LogError("XYZI chunk encountered before SIZE chunk.");
                        }
                        break;

                    case "RGBA":
                        tempPalette = ReadRGBAChunk(reader);
                        break;

                    default:
                        reader.BaseStream.Position += contentSize;
                        break;
                }

                reader.BaseStream.Position = chunkStart + contentSize;
            }
        }
        
        // Return array of frames
        if (frames.Count > 0)
        {
            return (frames.ToArray(), tempPalette);
        }
        else
        {
            Debug.LogError("No voxel frames found in .vox file.");
            return (new VoxFrame[0], tempPalette);
        }
    }

    private static void ReadXYZIChunk(BinaryReader reader, byte[] voxelArray, int sizeX, int sizeY, int sizeZ)
    {        
        int numVoxels = reader.ReadInt32();

        for (int i = 0; i < numVoxels; i++)
        {
            byte x = reader.ReadByte();
            byte y = reader.ReadByte();
            byte z = reader.ReadByte();
            byte colorIndex = reader.ReadByte();

            // Store voxels in original .vox coordinate system
            // Coordinate transformation will be handled by VoxModel.GetVoxel() when accessed
            if (x < sizeX && y < sizeY && z < sizeZ)
            {
                // Convert 3D coordinates to 1D array index using original .vox layout
                int index = x + y * sizeX + z * sizeX * sizeY;
                voxelArray[index] = colorIndex;
            }
        }
    }

    private static VoxPalette ReadRGBAChunk(BinaryReader reader)
    {
        var colors = new Color32[256];
        
        for (int i = 0; i < 256; i++)
        {
            byte r = reader.ReadByte();
            byte g = reader.ReadByte();
            byte b = reader.ReadByte();
            byte a = reader.ReadByte();
            colors[i] = new Color32(r, g, b, a);
        }
        
        return new VoxPalette(colors);
    }
    
    public static Mesh GenerateMesh(VoxFrame voxModel, VoxPalette palette)
    {
        // Backwards-compatible call with scale = 1
        return GenerateMesh(voxModel, palette, 1f);
    }

    public static Mesh GenerateMesh(VoxFrame voxModel, VoxPalette palette, float scale)
    {
        var mesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        
        // Check for null voxel data
        if (voxModel == null)
        {
            Debug.LogError("VoxModel is null. Cannot generate mesh.");
            return mesh; // Return empty mesh
        }

        if (palette == null)
        {
            Debug.LogError("Palette is null. Cannot generate mesh.");
            return mesh; // Return empty mesh
        }

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Color32> colors = new List<Color32>();

        Vector3[] directions = new Vector3[]
        {
            Vector3.right,    // +X
            Vector3.left,     // -X
            Vector3.up,       // +Y
            Vector3.down,     // -Y
            Vector3.forward,  // +Z
            Vector3.back      // -Z
        };
        
        Vector3[][] faceVertices = new Vector3[][]
        {
            // Right face (+X)
            new Vector3[] { new Vector3(1, 0, 0), new Vector3(1, 1, 0), new Vector3(1, 1, 1), new Vector3(1, 0, 1) },
            // Left face (-X)
            new Vector3[] { new Vector3(0, 0, 1), new Vector3(0, 1, 1), new Vector3(0, 1, 0), new Vector3(0, 0, 0) },
            // Top face (+Y)
            new Vector3[] { new Vector3(0, 1, 1), new Vector3(1, 1, 1), new Vector3(1, 1, 0), new Vector3(0, 1, 0) },
            // Bottom face (-Y)
            new Vector3[] { new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 0, 1), new Vector3(0, 0, 1) },
            // Front face (+Z)
            new Vector3[] { new Vector3(0, 0, 1), new Vector3(1, 0, 1), new Vector3(1, 1, 1), new Vector3(0, 1, 1) },
            // Back face (-Z)
            new Vector3[] { new Vector3(0, 1, 0), new Vector3(1, 1, 0), new Vector3(1, 0, 0), new Vector3(0, 0, 0) },
        };

        // Center calculation to match axis adjustments
        Vector3 center = new Vector3(
            (voxModel.sizeX) / 2f,
            (voxModel.sizeZ) / 2f,
            -(voxModel.sizeY) / 2f + 1f
        );

        // Iterate through all possible voxel positions
        for (int x = 0; x < voxModel.sizeX; x++)
        {
            for (int y = 0; y < voxModel.sizeY; y++)
            {
                for (int z = 0; z < voxModel.sizeZ; z++)
                {
                    // Adjust axes to match the orientation used in parsing
                    Vector3 pos = new Vector3(x, z, -y);
                    
                    byte paletteIndex = voxModel.GetVoxel(pos);
                    if (paletteIndex == 0) continue; // Empty voxel
                    
                    if (paletteIndex < 1 || paletteIndex > 255)
                    {
                        Debug.LogWarning($"Palette index {paletteIndex} out of range at position {pos}. Skipping voxel.");
                        continue;
                    }

                    Color32 color = palette[paletteIndex - 1];

                    for (int d = 0; d < 6; d++)
                    {
                        Vector3 neighborPos = pos + directions[d];
                        if (!voxModel.HasVoxel(neighborPos))
                        {
                            int vertexIndex = vertices.Count;

                            foreach (var corner in faceVertices[d])
                            {
                                Vector3 vertexPos = pos + corner;
                                vertexPos -= center;
                                vertexPos *= scale;
                                vertices.Add(vertexPos);
                                colors.Add(color);
                            }

                            triangles.Add(vertexIndex);
                            triangles.Add(vertexIndex + 1);
                            triangles.Add(vertexIndex + 2);
                            triangles.Add(vertexIndex);
                            triangles.Add(vertexIndex + 2);
                            triangles.Add(vertexIndex + 3);
                        }
                    }
                }
            }
        }

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetColors(colors);
        mesh.RecalculateNormals();

        return mesh;
    }
} 