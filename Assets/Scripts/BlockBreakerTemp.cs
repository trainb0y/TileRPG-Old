using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockBreakerTemp : MonoBehaviour
{
    public TerrainGenerator terrain;
    public Camera camera;
    public TileAtlas tileAtlas;
    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            terrain.RemoveTile((int)camera.ScreenToWorldPoint(Input.mousePosition).x, (int)camera.ScreenToWorldPoint(Input.mousePosition).y, Input.GetKey(KeyCode.B));
        }
        if (Input.GetMouseButton(1))
        {
            terrain.PlaceTile(tileAtlas.red, (int)camera.ScreenToWorldPoint(Input.mousePosition).x, (int)camera.ScreenToWorldPoint(Input.mousePosition).y, Input.GetKey(KeyCode.B), true);
        }

    }
}
