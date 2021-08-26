using UnityEngine;

public class ChunkLoader : MonoBehaviour
{
    public TerrainGenerator terrain;

    private void Start()
    {
        InvokeRepeating("LoadChunks", 1f, 0.5f);  //1s delay, repeat every 0.5s
    }
    void LoadChunks()
    {
        for (int i = 0; i < terrain.worldChunks.Length; i++)
        {
            if (Vector2.Distance(new Vector2((i*terrain.chunkSize) + terrain.chunkSize,0), new Vector2(gameObject.transform.position.x,0)) > Camera.main.orthographicSize * 4f)
            {
                terrain.worldChunks[i].SetActive(false);
            }
            else
            {
                terrain.worldChunks[i].SetActive(true);
            }
        }
       
    }
}
