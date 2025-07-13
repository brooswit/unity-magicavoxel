using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class VoxAsset : ScriptableObject
{
    public byte[] rawData;
    public VoxData data;
}
