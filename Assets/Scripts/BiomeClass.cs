
[System.Serializable]
public class BiomeClass {
    public string name;

    public bool generateCaves = true;

    public int heightAddition;
    public int heightMultiplier;

    public TileClass surfaceTile;
    public TileClass dirtTile;
    public int surfaceHeight = 5;
    public TileClass stoneTile;
    public OreClass[] ores;
    
}
