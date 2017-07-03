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
    public static Vector2 TilePixel = new Vector2(0.8f, 0.8f);

    public int width;
    public int height;
    public GameObject tilePref;
    
    void Awake()
    {
    }

    // Use this for initialization
    void Start()
    {
        float wx = width * TilePixel.x * 0.5f;
        float wy = height * TilePixel.y * 0.5f;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                GameObject tile = GameObject.Instantiate(tilePref);
                tile.transform.parent = transform;
                tile.transform.position = new Vector3(-wx + x * TilePixel.x, wy + -y * TilePixel.y, 0f);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}
