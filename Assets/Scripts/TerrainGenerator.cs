using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour
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
    

    private GameObject[,] worldTiles;
    private GameObject[,] backgroundTiles;
    private GameObject[] worldChunks;

    // Start is called before the first frame update
    void Start()
    {
        seed = Random.Range(-10000, 10000);
        worldTiles = new GameObject[worldSize, worldSize];
        backgroundTiles = new GameObject[worldSize, worldSize];
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
        ColorTerrain();
        GenerateOres();
        PlaceBackgroundTiles();
        CarveCaves();

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
            if (Random.Range(0,3) == 1)
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
                PlaceTile(tileAtlas.red, x, y);
            }
        }
    }

    public void GenerateOres()
    {
        for (int x = 0; x < worldTiles.GetLength(0); x++)
        {
            // Get a refrence to the current chunk
            float chunkCoord = (Mathf.Round(x / chunkSize) * chunkSize);
            chunkCoord /= chunkSize;
            GameObject chunk = worldChunks[(int)chunkCoord];

            // Get a refrence to the current biome
            BiomeClass biome = chunk.GetComponent<Biome>().biomeClass;

            foreach (OreClass ore in biome.ores)
            {
                for (int y = 0; y < worldTiles.GetLength(1); y++)
                if (ore.spreadTexture.GetPixel(x, y).r > 0.5f && y <= ore.maxSpawnHeight)
                {
                    PlaceTile(ore.tileClass,x,y);
                }
            }
        }
    }

    public void ColorTerrain()
    {
        // Have to replace the tileAtlas.red that GenerateTerrain amd SmoothBiomeBorders create
        for (int x = 0; x < worldTiles.GetLength(0); x++)
        {
            // Get a refrence to the current chunk
            float chunkCoord = (Mathf.Round(x / chunkSize) * chunkSize);
            chunkCoord /= chunkSize;
            GameObject chunk = worldChunks[(int)chunkCoord];

            // Get a refrence to the current biome
            BiomeClass biome = chunk.GetComponent<Biome>().biomeClass;
            bool grass = false; // we haven't done grass yet
            int soil = 0; // no soil so far

            for (int y = worldTiles.GetLength(1); y >= 0; y--) {
                // heading top down this time

                if (GetTile(x,y) != null && !grass)
                {
                    RemoveTile(x, y);
                    PlaceTile(biome.surfaceTile, x, y);
                    grass = true;
                }
                else if (GetTile(x, y) != null && soil < biome.surfaceHeight)
                {
                    RemoveTile(x, y);
                    PlaceTile(biome.dirtTile, x, y);
                    soil ++;
                }
                else if (GetTile(x, y) != null)
                {
                    RemoveTile(x, y);
                    PlaceTile(biome.stoneTile, x, y);
                }
            }
        }
    }

    public void PlaceBackgroundTiles()
    {
        // For every tile, place a background tile of the same type
        for (int x = 0; x < worldTiles.GetLength(0); x++)
            for (int y = 0; y < worldTiles.GetLength(1); y++)
            {
                if (GetTile(x,y) != null)
                {
                    PlaceTile(GetTileType(x, y), x, y, true);
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

                // Get our x coordinate
                int chunkIndex = System.Array.IndexOf(worldChunks, chunk);
                int x = chunkIndex * chunkSize;
                // X is the first block in the new chunk, so compare it and -1

                // no point checking beneath y=10, it might see the void as a cliff
                for (int y = 10; y < worldTiles.GetLength(1); y++)
                {
                    if(GetTile(x,y-1) == null && GetTile(x-1,y+1) != null)
                    {
                        // we have a cliff facing right, fill it in
                        for (int i = 0; i < 20; i++)
                        {
                            for (int j = 0; j < 20; j++)
                            {
                                if ((i + j) < 20)
                                {
                                    PlaceTile(tileAtlas.red, x + i, y + (j-19),safe: true);
                                }
                            }
                        }
                    }
                    if (GetTile(x - 1, y - 1) == null && GetTile(x, y + 1) != null)
                    {
                        // we have a cliff facing left, fill it in
                        for (int i = 0; i < 20; i++)
                        {
                            for (int j = 0; j < 20; j++)
                            {
                                if ((i + j) < 20)
                                {
                                    PlaceTile(tileAtlas.red, x - (i -1), y + (j - 19), safe: true);
                                }
                            }
                        }
                    }
                }
            }
            lastBiome = chunk.GetComponent<Biome>().biomeClass;
        }
    }

    public void CarveCaves()
    {
        caveNoiseTexture = new Texture2D(worldSize, worldSize);

        GenerateNoiseTexture(caveFreq, caveCutoff, caveNoiseTexture);

        for (int x = 0; x < worldTiles.GetLength(0); x++)
        {
            // Get a refrence to the current chunk
            float chunkCoord = (Mathf.Round(x / chunkSize) * chunkSize);
            chunkCoord /= chunkSize;
            GameObject chunk = worldChunks[(int)chunkCoord];

            // Get a refrence to the current biome
            BiomeClass biome = chunk.GetComponent<Biome>().biomeClass;

            if (biome.generateCaves)
            {
                for (int y = 0; y < worldTiles.GetLength(1); y++)
                    if (caveNoiseTexture.GetPixel(x, y).r < 0.5) { RemoveTile(x, y); }
            }
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

    public void PlaceTile(TileClass tileClass, int x, int y, bool background=false, bool safe=false)
    {
        try
        {
            if (safe) // Safe mode makes it not replace tiles
            {
                if (GetTile(x, y) != null) { return; }
                if (background && GetTile(x, y, true) != null) { return; }
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
            newTile.AddComponent<Tile>();
            newTile.GetComponent<Tile>().tileClass = tileClass;
            newTile.transform.position = new Vector2(x + 0.5f, y + 0.5f);

            newTile.name = tileClass.name;

            if (background)
            {
                backgroundTiles[x, y] = newTile;
                newTile.GetComponent<SpriteRenderer>().color = new Color(0.5f, 0.5f, 0.5f); // ??
                newTile.GetComponent<SpriteRenderer>().sortingOrder = -10;
            }
            else
            {
                newTile.GetComponent<SpriteRenderer>().sortingOrder = -5;
                worldTiles[x, y] = newTile;
                newTile.AddComponent<BoxCollider2D>();  // not sure this is smart because there are so many tiles
                newTile.GetComponent<BoxCollider2D>().size = Vector2.one;
            }
        }
        catch (System.IndexOutOfRangeException)
        {
            // probably invalid coordinates
            return;
        }
    }


    public GameObject GetTile(int x, int y, bool background = false)
    {
        try
        {
            if (background)
            {
                return backgroundTiles[x, y];
            }
            return worldTiles[x, y];
        }
        catch (System.IndexOutOfRangeException) // invalid coords
        {
            return null;
        }
    }


    public TileClass GetTileType(int x, int y, bool background = false)
    {
        if (GetTile(x, y, background) != null)
        {
            return GetTile(x, y, background).GetComponent<Tile>().tileClass;
        }
        return null;
    }

    public void RemoveTile(int x, int y, bool background = false)
    {
        if (background)
        {
            Destroy(backgroundTiles[x, y]);
            backgroundTiles[x, y] = null;
        }
        else
        {
            Destroy(worldTiles[x, y]);
            worldTiles[x, y] = null; // not sure if this is needed, better safe than sorry
        }
    }
}
