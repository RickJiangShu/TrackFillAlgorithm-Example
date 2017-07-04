/*
 * Author:  Rick
 * Create:  7/3/2017 9:39:48 PM
 * Email:   rickjiangshu@gmail.com
 * Follow:  https://github.com/RickJiangShu
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 二维格子地图
/// </summary>
public class TileMap : MonoBehaviour
{
    public static TileMap ins;
    public static Vector2 tilePixel = new Vector2(0.8f, 0.8f);

    public int width;
    public int height;
    public GameObject tilePref;

    public Grid[,] gridMap;//格子地图
    
    void Awake()
    {
        ins = this;
    }

    // Use this for initialization
    void Start()
    {
        gridMap = new Grid[width, height];

        float wx = width * tilePixel.x * 0.5f;
        float wy = height * tilePixel.y * 0.5f;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Grid grid = new Grid();
                grid.x = x;
                grid.y = y;
                grid.flag = 0;
                gridMap[x, y] = grid;

                GameObject tileGO = GameObject.Instantiate(tilePref);
                tileGO.transform.parent = transform;
                tileGO.transform.position = new Vector3(-wx + x * tilePixel.x, wy + -y * tilePixel.y, 0f);
                Tile tile = tileGO.AddComponent<Tile>();
                tile.grid = grid;
            }
        }
    }

}
