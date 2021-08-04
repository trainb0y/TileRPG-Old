using UnityEngine;

[System.Serializable]
public class OreClass
{
    public string name;
    public TileClass tileClass;
    [Range(0,1)]
    public float rarity;
    [Range(0, 1)]
    public float size;
    public int maxSpawnHeight;
    public Texture2D spreadTexture;
}
