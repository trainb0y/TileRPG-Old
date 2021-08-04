using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGeneratorOld : MonoBehaviour
{
    [Header("Tile Atlas")]
    public TileAtlas tileAtlas;

    [Header("TEMPORARY, IMPLEMENT BETTER TREES")]
    public int treeChance = 10;

    [Header("Terrain Generation")]
    public int chunkSize = 16;
    public int worldSize = 100;


    [Header("Noise Settings")]
    public float caveFreq = 0.05f;
    public float terrainFreq = 0.05f;
    public float caveCutoff = 0.3f;
    public float seed;
    public Texture2D caveNoiseTexture;


    [Header("Biome Settings")]
    public BiomeClass[] biomes;
    /*
     Biomes define ores, vegetation, and materials.
     */
    

    private List<Vector2> worldTiles = new List<Vector2>();
    private GameObject[] worldChunks;

    // Start is called before the first frame update
    void Start()
    {
        seed = Random.Range(-10000, 10000);

        caveNoiseTexture = new Texture2D(worldSize, worldSize);

        GenerateNoiseTexture(caveFreq, caveCutoff, caveNoiseTexture);

        foreach (BiomeClass biome in biomes)
        {
            foreach (OreClass ore in biome.ores)
            {
                ore.spreadTexture = new Texture2D(worldSize, worldSize);
                GenerateNoiseTexture(ore.rarity, ore.size, ore.spreadTexture);
            }
        }
        
        
        CreateChunks();
        AssignBiomes();
        GenerateTerrain();
        SmoothBiomeBorders();
    }

    public void CreateChunks()
    {
        int numChunks = worldSize / chunkSize;
        worldChunks = new GameObject[numChunks];
        for (int i = 0; i < numChunks; i++)
        {
            GameObject newChunk = new GameObject();
            newChunk.name = i.ToString();
            newChunk.transform.parent = this.transform;
            worldChunks[i] = newChunk;
        }
    }

    public void AssignBiomes()
    {
        BiomeClass lastBiome = biomes[0];
        foreach(GameObject chunk in worldChunks)
        {
            chunk.AddComponent<Biome>();
            Biome chunkBiome = chunk.GetComponent<Biome>();
            chunkBiome.biomeClass = lastBiome;
            if (Random.Range(0,5) == 1)
            {
                // this biome is different
                chunkBiome.biomeClass = biomes[Random.Range(0, biomes.Length)];
            }
            lastBiome = chunkBiome.biomeClass;
        }
    }

    public void GenerateTerrain()
    {
        for (int x  = 0; x < worldSize; x++)
        {
            // Get a refrence to the current chunk
            float chunkCoord = (Mathf.Round(x / chunkSize) * chunkSize);
            chunkCoord /= chunkSize;
            GameObject chunk = worldChunks[(int)chunkCoord];

            // Get a refrence to the current biome
            BiomeClass biome = chunk.GetComponent<Biome>().biomeClass;
            int dirtLayerHeight = biome.surfaceHeight;
            int heightMultiplier = biome.heightMultiplier;
            int heightAddition = biome.heightAddition;


            float height = Mathf.PerlinNoise((x + seed) * terrainFreq, seed * terrainFreq) * heightMultiplier + heightAddition;
            for (int y = 0; y < height; y++)
            {
                TileClass tileClass;
                if (y < height - dirtLayerHeight)
                {
                    // Generate something like stone
                    tileClass = biome.stoneTile;

                    foreach(OreClass ore in biome.ores)
                    {
                        if (ore.spreadTexture.GetPixel(x, y).r > 0.5f && y <= ore.maxSpawnHeight)
                        {
                            tileClass = ore.tileClass;
                        }
                    }
                }
                else if (y < height - 1)
                {
                    tileClass = biome.dirtTile;
                }
                else
                {
                    // Terrain top layer
                    tileClass = biome.surfaceTile;
                }
                
                if (biome.generateCaves)
                {
                    if (caveNoiseTexture.GetPixel(x, y).r > 0.5) {PlaceTile(tileClass,x,y);}
                }
                else {PlaceTile(tileClass,x,y);}

                /*
                if (y >= height - 1)
                { // generate trees on top 
                    int t = Random.Range(0, treeChance);
                    if (t == 1)
                    {
                        if (worldTiles.Contains(new Vector2(x, y))) { //  make sure there is a tile under it... 
                            GenerateTree(x, y + 1);                // Seems pretty inneffecient for large worlds? (checking ALL the tiles, that is)
                        }
                    }
                }
                */

            }
        }
    }

    public void SmoothBiomeBorders()
    {
        // Identify the area that we need to smooth
        BiomeClass lastBiome = worldChunks[0].GetComponent<Biome>().biomeClass;
        foreach (GameObject chunk in worldChunks)
        {
            if (chunk.GetComponent<Biome>().biomeClass != lastBiome)
            {
                // We don't care what the biomes are, but we have to add some tiles if
                // there is a cliff
            }
            lastBiome = chunk.GetComponent<Biome>().biomeClass;
        }
    }

    public void GenerateNoiseTexture(float frequency, float limit, Texture2D noiseTexture)
    {
        for (int x = 0; x < noiseTexture.width; x++)
        {
            for (int y = 0; y < noiseTexture.height; y++)
            {
                float v = Mathf.PerlinNoise((x + seed) * frequency, (y + seed) * frequency);
                if (v > limit)
                {
                    noiseTexture.SetPixel(x, y, Color.white);
                }
                else
                {
                    noiseTexture.SetPixel(x, y, Color.black);
                }
              
            }
        }
        noiseTexture.Apply();
    }

    public void GenerateTree(int x, int y)
    {
        // THIS IS TEMPORARY
        // trees should be their own object, capable of falling over

        for (int i = 0; i < 4; i++) {
            PlaceTile(tileAtlas.log, x, y + i);
        }

        // now for the leaves

        PlaceTile(tileAtlas.leaf, x-1, y + 4);
        PlaceTile(tileAtlas.leaf, x-1, y + 5);
        PlaceTile(tileAtlas.leaf, x+1, y + 4);
        PlaceTile(tileAtlas.leaf, x+1, y + 5);
        PlaceTile(tileAtlas.leaf, x, y + 6);
        PlaceTile(tileAtlas.leaf, x, y + 5);
        PlaceTile(tileAtlas.leaf, x, y + 4);
    }

    public void PlaceTile(TileClass tileClass, int x, int y, bool safe=false)
    {
        if (safe) // Safe mode makes it not replace tiles
        {
            if (!worldTiles.Contains(new Vector2Int(x, y))){ return; }
        }
        // CAN OVERWRITE TILES, see https://youtu.be/PaDUYXfbiL0?list=PLn1X2QyVjFVDE9syarF1HoUFwB_3K7z2y&t=145 
        // but eh, seems not worth it performance-wise
        Sprite tileSprite = tileClass.tileSprites[Random.Range(0, tileClass.tileSprites.Length)]; // pick a random one

        GameObject newTile = new GameObject();

        float chunkCoord = (Mathf.Round(x / chunkSize) * chunkSize);
        chunkCoord /= chunkSize;

        newTile.transform.parent = worldChunks[(int)chunkCoord].transform;

        newTile.AddComponent<SpriteRenderer>();
        newTile.GetComponent<SpriteRenderer>().sprite = tileSprite;
        newTile.transform.position = new Vector2(x + 0.5f, y + 0.5f);

        newTile.name = tileClass.name;

        worldTiles.Add(newTile.transform.position - (Vector3.one * 0.5f)); // Remove the 0.5 offset
    }
}
