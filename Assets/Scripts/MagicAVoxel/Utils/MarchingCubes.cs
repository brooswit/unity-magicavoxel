using System.Collections.Generic;
using UnityEngine;

public static class MarchingCubes
{
    

    // Corner offsets moved to tables

    public static Mesh GenerateMesh(VoxFrame voxFrame, VoxPalette palette, float scale)
    {
        var mesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        if (voxFrame == null || palette == null)
            return mesh;

        int sizeX = voxFrame.sizeX;
        int sizeY = voxFrame.sizeY;
        int sizeZ = voxFrame.sizeZ;

        // Build scalar field at lattice points with configurable empty padding around the volume
        int pad = 1;
        int gx = sizeX + 1 + pad * 2;
        int gy = sizeY + 1 + pad * 2;
        int gz = sizeZ + 1 + pad * 2;
        float[,,] density = new float[gx, gy, gz];

        // Helper to get occupancy of voxel cell at (x,y,z) in original indices
        // Out-of-bounds returns empty (0) – used for density so the padding remains empty
        byte GetOccupancyCell(int cx, int cy, int cz)
        {
            if (cx < 0 || cy < 0 || cz < 0 || cx >= sizeX || cy >= sizeY || cz >= sizeZ)
                return 0;
            return voxFrame.GetVoxel(new Vector3(cx, cz, -cy));
        }

        // Helper to get buffered palette index – out-of-bounds clamps to nearest in-bounds voxel
        byte GetBufferedCell(int cx, int cy, int cz)
        {
            int bx = Mathf.Clamp(cx, 0, sizeX - 1);
            int by = Mathf.Clamp(cy, 0, sizeY - 1);
            int bz = Mathf.Clamp(cz, 0, sizeZ - 1);
            return voxFrame.GetVoxel(new Vector3(bx, bz, -by));
        }

        // Corner density = average of the 8 adjacent voxel cells
        // Evaluate on padded lattice coordinates so boundary cubes have an explicit outside
        for (int z = -pad; z <= sizeZ + pad; z++)
        {
            for (int y = -pad; y <= sizeY + pad; y++)
            {
                for (int x = -pad; x <= sizeX + pad; x++)
                {
                    int count = 0;
                    int sum = 0;
                    for (int dz = -1; dz <= 0; dz++)
                    for (int dy = -1; dy <= 0; dy++)
                    for (int dx = -1; dx <= 0; dx++)
                    {
                        int vx = x + dx;
                        int vy = y + dy;
                        int vz = z + dz;
                        // Treat out-of-bounds neighbors as empty; always count 8 samples
                        byte c = GetOccupancyCell(vx, vy, vz);
                        sum += (c != 0) ? 1 : 0;
                        count++;
                    }
                    float baseD = (count > 0) ? (float)sum / count : 0f;
                    density[x + pad, y + pad, z + pad] = baseD;
                }
            }
        }

        // No smoothing; use raw density field

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Color32> colors = new List<Color32>();

        float iso = 0.25f;

        // Center pivot consistent with VoxTools (pos = (x, z, -y))
        Vector3 center = new Vector3(sizeX / 2f, sizeZ / 2f, -sizeY / 2f);

        // Iterate cubes in voxel index space, including a 1-cube empty padding
        for (int z = -pad; z < sizeZ + pad; z++)
        {
            for (int y = -pad; y < sizeY + pad; y++)
            {
                for (int x = -pad; x < sizeX + pad; x++)
                {
                    // Corner scalar values and positions
                    float[] cornerVal = new float[8];
                    Vector3[] cornerPos = new Vector3[8];

                    for (int i = 0; i < 8; i++)
                    {
                        int cx = x + (int)MarchingCubesTables.CornerOffsets[i].x;
                        int cy = y + (int)MarchingCubesTables.CornerOffsets[i].y;
                        int cz = z + (int)MarchingCubesTables.CornerOffsets[i].z;
                        cornerVal[i] = density[cx + pad, cy + pad, cz + pad];
                        // Transform to mesh-space using unpadded base indices
                        Vector3 off = MarchingCubesTables.CornerOffsets[i];
                        Vector3 p = new Vector3(x, z, -y) + new Vector3(off.x, off.z, -off.y);
                        p -= center;
                        p *= scale;
                        cornerPos[i] = p;
                    }

                    // Compute cube index (canonical: set bit when value < iso)
                    int cubeIndex = 0;
                    for (int i = 0; i < 8; i++)
                    {
                        if (cornerVal[i] < iso) cubeIndex |= 1 << i;
                    }
                    int edgeMask = MarchingCubesTables.EdgeTable[cubeIndex];
                    if (edgeMask == 0) continue;

                    // Interpolate vertices on edges
                    Vector3[] edgeVertex = new Vector3[12];
                    Color32[] edgeColor = new Color32[12];
                    bool[] edgeReady = new bool[12];

                    // Determine dominant color among non-zero samples
                    byte selectedColorIndex = 0;
                    Dictionary<byte, int> counts = new Dictionary<byte, int>();
                    for (int ci = 0; ci < 8; ci++)
                    {
                        var co = MarchingCubesTables.CornerOffsets[ci];
                        byte cidx = GetBufferedCell(x + (int)co.x, y + (int)co.y, z + (int)co.z);
                        if (cidx == 0) continue;
                        if (!counts.ContainsKey(cidx)) counts[cidx] = 0;
                        counts[cidx]++;
                    }
                    int best = 0;
                    foreach (var kv in counts)
                    {
                        if (kv.Value > best)
                        {
                            best = kv.Value;
                            selectedColorIndex = kv.Key;
                        }
                    }

                    for (int e = 0; e < 12; e++)
                    {
                        if ((edgeMask & (1 << e)) == 0) continue;
                        int a0 = MarchingCubesTables.EdgeIndexPairs[e, 0];
                        int b0 = MarchingCubesTables.EdgeIndexPairs[e, 1];
                        float va = cornerVal[a0];
                        float vb = cornerVal[b0];
                        Vector3 pa = cornerPos[a0];
                        Vector3 pb = cornerPos[b0];
                        float t = (Mathf.Abs(vb - va) < 1e-6f) ? 0.5f : (iso - va) / (vb - va);
                        t = Mathf.Clamp01(t);
                        edgeVertex[e] = Vector3.Lerp(pa, pb, t);

                        // Use selected cube color; fall back to a neutral gray
                        if (selectedColorIndex > 0 && selectedColorIndex <= 255)
                            edgeColor[e] = palette[selectedColorIndex - 1];
                        else
                            edgeColor[e] = new Color32(200, 200, 200, 255);

                        edgeReady[e] = true;
                    }

                    // Emit triangles
                    int triRow = cubeIndex;
                    for (int i = 0; i < 16; i += 3)
                    {
                        int a = MarchingCubesTables.TriTable[triRow, i];
                        if (a == -1) break;
                        if (i + 2 >= 16) break;
                        int b = MarchingCubesTables.TriTable[triRow, i + 1];
                        int c = MarchingCubesTables.TriTable[triRow, i + 2];

                        // Validate edge indices and ensure they were computed
                        if (a < 0 || a >= 12 || b < 0 || b >= 12 || c < 0 || c >= 12)
                            continue;
                        if (!edgeReady[a] || !edgeReady[b] || !edgeReady[c])
                            continue;

                        int vi = vertices.Count;
                        vertices.Add(edgeVertex[a]);
                        vertices.Add(edgeVertex[b]);
                        vertices.Add(edgeVertex[c]);

                        colors.Add(edgeColor[a]);
                        colors.Add(edgeColor[b]);
                        colors.Add(edgeColor[c]);

                        triangles.Add(vi);
                        triangles.Add(vi + 1);
                        triangles.Add(vi + 2);
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


