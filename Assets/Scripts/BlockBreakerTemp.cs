using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockBreakerTemp : MonoBehaviour
{
    public TerrainGenerator terrain;
    public Camera mainCamera;
    public TileAtlas tileAtlas;
    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            terrain.RemoveTile((int)mainCamera.ScreenToWorldPoint(Input.mousePosition).x, (int)mainCamera.ScreenToWorldPoint(Input.mousePosition).y, Input.GetKey(KeyCode.B));
        }
        if (Input.GetMouseButton(1))
        {
            terrain.PlaceTile(tileAtlas.red, (int)mainCamera.ScreenToWorldPoint(Input.mousePosition).x, (int)mainCamera.ScreenToWorldPoint(Input.mousePosition).y, Input.GetKey(KeyCode.B), true);
        }

    }
}
