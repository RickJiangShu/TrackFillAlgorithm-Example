/*
 * Author:  Rick
 * Create:  7/3/2017 9:50:39 PM
 * Email:   rickjiangshu@gmail.com
 * Follow:  https://github.com/RickJiangShu
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 划线
/// </summary>
public class TileLines : MonoBehaviour
{
    private TileMap tileMap;
    // Use this for initialization
    void Start()
    {
        tileMap = GetComponent<TileMap>();

        float rx = TileMap.tilePixel.x * 0.5f + tileMap.width * TileMap.tilePixel.x * 0.5f;
        float ry = TileMap.tilePixel.y * 0.5f + tileMap.height * TileMap.tilePixel.y * 0.5f;

        float right = -rx + tileMap.width * TileMap.tilePixel.x;
        float bottom = ry + tileMap.height * TileMap.tilePixel.y * -1f;
        ///列
        for (int col = 0; col <= tileMap.width; col++)
        {
            float x = -rx + col * TileMap.tilePixel.x;
            Vector3 start = new Vector3(x, ry, -1f);
            Vector3 end = new Vector3(x, bottom, -1f);
            DrawLine(start, end, Color.red);
        }

        //行
        for (int row = 0; row <= tileMap.height; row++)
        {
            float y = ry + row * TileMap.tilePixel.y * -1f;
            Vector3 start = new Vector3(-rx, y, -1f);
            Vector3 end = new Vector3(right, y, -1f);
            DrawLine(start, end, Color.red);
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void DrawLine(Vector3 start, Vector3 end, Color color)
    {
        GameObject line = new GameObject("Line");
        
        line.transform.localPosition = start;
        line.transform.SetParent(transform, true);

        LineRenderer lr = line.AddComponent<LineRenderer>();
        lr.material = new Material(Shader.Find("Particles/Alpha Blended Premultiply"));
        lr.startColor = color;
        lr.endColor = color;
        lr.startWidth = 0.1f;
        lr.endWidth = 0.1f;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        
    }
}
