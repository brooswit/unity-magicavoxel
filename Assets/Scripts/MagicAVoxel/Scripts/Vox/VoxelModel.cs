using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class VoxelModel : MonoBehaviour
{
    private int sizeX = 0;
    private int sizeY = 0;
    private int sizeZ = 0;

    [SerializeField]
    public float voxelsPerUnit = 1f;
    // Reference to the imported .vox file data
    public VoxAsset voxAsset;

    // Dictionary to store voxel positions and palette indices
    private Dictionary<Vector3, byte> voxels = new Dictionary<Vector3, byte>();
    private Color32[] palette = new Color32[256];

    // Mesh components
    private Mesh mesh;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private MeshCollider meshCollider;

    // Toggle to update the collider when mesh changes
    [SerializeField]
    private bool updateCollider = false;

    void OnEnable()
    {
        // Initialize components
        InitializeComponents();

        // Assign material if not already assigned
        AssignMaterial();

        // Initialize the palette and load the voxel model
        InitializeDefaultPalette();
        LoadVoxelModel();
    }

    private void InitializeComponents()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        meshCollider = GetComponent<MeshCollider>();
    }


    private void AssignMaterial()
    {
        if (meshRenderer.sharedMaterial == null)
        {
            // Assign your shader here if needed
            // For example:
            Shader shader = Shader.Find("Custom/VertexColorShader");
            if (shader != null)
            {
                Material material = new Material(shader);
                meshRenderer.sharedMaterial = material;
            }
        }
    }


    public void LoadVoxelModel()
    {
        if (voxAsset != null)
        {
            InitializeDefaultPalette();
            LoadVoxFile(voxAsset.data);
        }
        else
        {
            ClearVoxelData();
            Debug.LogError("VoxelModel: No .vox file assigned to " + gameObject.name + " object, child of " + transform.parent.name);
        }
    }

    private void ClearVoxelData()
    {
        if (mesh != null)
        {
            voxels.Clear();
            GenerateMesh();
        }
    }

    private void LoadVoxFile(byte[] voxData)
    {
        voxels.Clear();

        using (MemoryStream ms = new MemoryStream(voxData))
        using (BinaryReader reader = new BinaryReader(ms))
        {
            ParseVoxFile(reader);
        }
        GenerateMesh();
    }

    private void InitializeDefaultPalette()
    {
        uint[] defaultPalette = new uint[256]
        {
            // Full 256 colors here as per MagicaVoxel default palette
            0x00000000, 0xffffffff, 0xffccffff, 0xff99ffff, 0xff66ffff, 0xff33ffff, 0xff00ffff, 0xffffccff,
            0xffccccff, 0xff99ccff, 0xff66ccff, 0xff33ccff, 0xff00ccff, 0xffff99ff, 0xffcc99ff, 0xff9999ff,
            0xff6699ff, 0xff3399ff, 0xff0099ff, 0xffff66ff, 0xffcc66ff, 0xff9966ff, 0xff6666ff, 0xff3366ff,
            0xff0066ff, 0xffff33ff, 0xffcc33ff, 0xff9933ff, 0xff6633ff, 0xff3333ff, 0xff0033ff, 0xffff00ff,
            0xffcc00ff, 0xff9900ff, 0xff6600ff, 0xff3300ff, 0xff0000ff, 0xffffffcc, 0xffccffcc, 0xff99ffcc,
            0xff66ffcc, 0xff33ffcc, 0xff00ffcc, 0xffffcccc, 0xffcccccc, 0xff99cccc, 0xff66cccc, 0xff33cccc,
            0xff00cccc, 0xffff99cc, 0xffcc99cc, 0xff9999cc, 0xff6699cc, 0xff3399cc, 0xff0099cc, 0xffff66cc,
            0xffcc66cc, 0xff9966cc, 0xff6666cc, 0xff3366cc, 0xff0066cc, 0xffff33cc, 0xffcc33cc, 0xff9933cc,
            0xff6633cc, 0xff3333cc, 0xff0033cc, 0xffff00cc, 0xffcc00cc, 0xff9900cc, 0xff6600cc, 0xff3300cc,
            0xff0000cc, 0xffffff99, 0xffccff99, 0xff99ff99, 0xff66ff99, 0xff33ff99, 0xff00ff99, 0xffffcc99,
            0xffcccc99, 0xff99cc99, 0xff66cc99, 0xff33cc99, 0xff00cc99, 0xffff9999, 0xffcc9999, 0xff999999,
            0xff669999, 0xff339999, 0xff009999, 0xffff6699, 0xffcc6699, 0xff996699, 0xff666699, 0xff336699,
            0xff006699, 0xffff3399, 0xffcc3399, 0xff993399, 0xff663399, 0xff333399, 0xff003399, 0xffff0099,
            0xffcc0099, 0xff990099, 0xff660099, 0xff330099, 0xff000099, 0xffffff66, 0xffccff66, 0xff99ff66,
            0xff66ff66, 0xff33ff66, 0xff00ff66, 0xffffcc66, 0xffcccc66, 0xff99cc66, 0xff66cc66, 0xff33cc66,
            0xff00cc66, 0xffff9966, 0xffcc9966, 0xff999966, 0xff669966, 0xff339966, 0xff009966, 0xffff6666,
            0xffcc6666, 0xff996666, 0xff666666, 0xff336666, 0xff006666, 0xffff3366, 0xffcc3366, 0xff993366,
            0xff663366, 0xff333366, 0xff003366, 0xffff0066, 0xffcc0066, 0xff990066, 0xff660066, 0xff330066,
            0xff000066, 0xffffff33, 0xffccff33, 0xff99ff33, 0xff66ff33, 0xff33ff33, 0xff00ff33, 0xffffcc33,
            0xffcccc33, 0xff99cc33, 0xff66cc33, 0xff33cc33, 0xff00cc33, 0xffff9933, 0xffcc9933, 0xff999933,
            0xff669933, 0xff339933, 0xff009933, 0xffff6633, 0xffcc6633, 0xff996633, 0xff666633, 0xff336633,
            0xff006633, 0xffff3333, 0xffcc3333, 0xff993333, 0xff663333, 0xff333333, 0xff003333, 0xffff0033,
            0xffcc0033, 0xff990033, 0xff660033, 0xff330033, 0xff000033, 0xffffff00, 0xffccff00, 0xff99ff00,
            0xff66ff00, 0xff33ff00, 0xff00ff00, 0xffffcc00, 0xffcccc00, 0xff99cc00, 0xff66cc00, 0xff33cc00,
            0xff00cc00, 0xffff9900, 0xffcc9900, 0xff999900, 0xff669900, 0xff339900, 0xff009900, 0xffff6600,
            0xffcc6600, 0xff996600, 0xff666600, 0xff336600, 0xff006600, 0xffff3300, 0xffcc3300, 0xff993300,
            0xff663300, 0xff333300, 0xff003300, 0xffff0000, 0xffcc0000, 0xff990000, 0xff660000, 0xff330000,
            0xff0000ee, 0xff0000dd, 0xff0000bb, 0xff0000aa, 0xff000088, 0xff000077, 0xff000055, 0xff000044,
            0xff000022, 0xff000011, 0xff00ee00, 0xff00dd00, 0xff00bb00, 0xff00aa00, 0xff008800, 0xff007700,
            0xff005500, 0xff004400, 0xff002200, 0xff001100, 0xffee0000, 0xffdd0000, 0xffbb0000, 0xffaa0000,
            0xff880000, 0xff770000, 0xff550000, 0xff440000, 0xff220000, 0xff110000, 0xffeeeeee, 0xffdddddd,
            0xffbbbbbb, 0xffaaaaaa, 0xff888888, 0xff777777, 0xff555555, 0xff444444, 0xff222222, 0xff111111
        };

        for (int i = 0; i < 256; i++)
        {
            uint color = defaultPalette[i];
            byte r = (byte)((color >> 24) & 0xFF);
            byte g = (byte)((color >> 16) & 0xFF);
            byte b = (byte)((color >> 8) & 0xFF);
            byte a = (byte)(color & 0xFF);
            palette[i] = new Color32(r, g, b, a);
        }
    }

    private void ParseVoxFile(BinaryReader reader)
    {
        string magic = new string(reader.ReadChars(4)); // Should be 'VOX '
        if (magic != "VOX ")
        {
            Debug.LogError("Invalid .vox file header.");
            return;
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
                    sizeX = reader.ReadInt32();
                    sizeY = reader.ReadInt32();
                    sizeZ = reader.ReadInt32();
                    break;

                case "XYZI":
                    ReadXYZIChunk(reader);
                    break;

                case "RGBA":
                    ReadRGBAChunk(reader);
                    break;

                default:
                    reader.BaseStream.Position += contentSize;
                    break;
            }

            reader.BaseStream.Position = chunkStart + contentSize;
        }
    }

    private void ReadXYZIChunk(BinaryReader reader)
    {
        int numVoxels = reader.ReadInt32();
        for (int i = 0; i < numVoxels; i++)
        {
            byte x = reader.ReadByte();
            byte y = reader.ReadByte();
            byte z = reader.ReadByte();
            byte colorIndex = reader.ReadByte();

            // Adjust axes to fix the orientation
            Vector3 position = new Vector3(x, z, -y);
            voxels[position] = colorIndex;
        }
    }


    private void ReadRGBAChunk(BinaryReader reader)
    {
        for (int i = 0; i < 256; i++)
        {
            byte r = reader.ReadByte();
            byte g = reader.ReadByte();
            byte b = reader.ReadByte();
            byte a = reader.ReadByte();
            palette[i] = new Color32(r, g, b, a);
        }
    }
    private void GenerateMesh()
    {
        if (mesh == null)
        {
            mesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        }
        else
        {
            mesh.Clear();
        }

        if (voxelsPerUnit == 0f)
        {
            Debug.LogError("voxelsPerUnit cannot be zero.");
            return;
        }

        float scale = 1f / voxelsPerUnit;

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

        // Adjusted center calculation to match axis adjustments
        Vector3 center = new Vector3(
            (sizeX) / 2f,
            (sizeZ) / 2f,
            -(sizeY) / 2f + 1f
        );


        foreach (var voxel in voxels)
        {
            Vector3 pos = voxel.Key;
            byte paletteIndex = voxel.Value;

            if (paletteIndex < 1 || paletteIndex > 256)
            {
                Debug.LogWarning($"Palette index {paletteIndex} out of range at position {pos}. Skipping voxel.");
                continue;
            }

            Color32 color = palette[paletteIndex - 1];

            for (int d = 0; d < 6; d++)
            {
                Vector3 neighborPos = pos + directions[d];
                if (!voxels.ContainsKey(neighborPos))
                {
                    int vertexIndex = vertices.Count;

                    foreach (var corner in faceVertices[d])
                    {
                        Vector3 vertexPos = pos + corner;
                        vertexPos -= center;
                        vertexPos *= scale; // Apply scaling
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

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetColors(colors);
        mesh.RecalculateNormals();

        meshFilter.sharedMesh = mesh;

        if (updateCollider)
        {
            if (meshCollider == null)
            {
                meshCollider = gameObject.GetComponent<MeshCollider>();

                if (meshCollider == null)
                {
                    meshCollider = gameObject.AddComponent<MeshCollider>();
                }
            }

            meshCollider.sharedMesh = null;
            meshCollider.sharedMesh = mesh;

            // Set the convex property based on the presence of a Rigidbody
            if (GetComponent<Rigidbody>() != null)
            {
                meshCollider.convex = true;
            }
            else
            {
                meshCollider.convex = false;
            }
        }
        Debug.Log($"Generated Mesh - Vertices: {mesh.vertexCount}, Triangles: {triangles.Count}");
    }


    void OnValidate()
    {
        // Initialize components if they are null
        InitializeComponents();

        // Initialize the mesh if it is null
        if (mesh == null)
        {
            mesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            meshFilter.sharedMesh = mesh;
        }

        // Initialize the voxels dictionary if null
        if (voxels == null)
            voxels = new Dictionary<Vector3, byte>();

        // Assign material if not already assigned
        AssignMaterial();

        // Reload the voxel model
        if (voxAsset != null)
        {
            InitializeDefaultPalette();
            LoadVoxelModel();
        }
        else
        {
            ClearVoxelData();
        }
    }
}
