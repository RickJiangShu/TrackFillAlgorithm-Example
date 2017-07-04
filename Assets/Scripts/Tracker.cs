/*
 * Author:  Rick
 * Create:  2017/7/4 13:46:31
 * Email:   rickjiangshu@gmail.com
 * Follow:  https://github.com/RickJiangShu
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracker
/// </summary>
public class Tracker : MonoBehaviour
{
    private Vector2[] direction4 = new Vector2[4] { new Vector2(-1, 0), new Vector2(0, 1), new Vector2(1, 0), new Vector2(0, -1) };

    public int bornX;
    public int bornY;

    private Rect rect;//Track + Ground 的最大矩形

    private bool trackStart = false;//是否开始Track
    private Grid trackHead;//头

    // Use this for initialization
    void Start()
    {
        rect = new Rect(bornX, bornY, 3, 3);
        Born();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                Tile tile = hit.collider.GetComponent<Tile>();

                if (!trackStart)
                {
                    //判断这个格子是否是地面周围的格子
                    if (CheckStart(tile.grid))
                    {
                        trackStart = true;
                        FlagTrack(tile.grid);
                    }
                }
            }
        }

        if (trackStart && Input.GetMouseButton(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                Tile tile = hit.collider.GetComponent<Tile>();
                if(CheckTrack(tile.grid))
                {
                    FlagTrack(tile.grid);
                }
            }
        }

    }

    #region Core 填充算法
    private int[,] testTempMap;

    private void TestAndFill()
    {

    }

    /// <summary>
    /// 纵向检测
    /// </summary>
    private void VerticalTest()
    {
    }
    /// <summary>
    /// 横向检测
    /// </summary>
    private void HorizontalTest()
    {
    }
    #endregion

    /// <summary>
    /// 初始化格子
    /// </summary>
    /// <param name="grid"></param>
    public void Born()
    {
        for (int y = (int)rect.y; y < rect.yMax; y++)
        {
            for (int x = (int)rect.x; x < rect.xMax; x++)
            {
                TileMap.ins.gridMap[x, y].SetFlag(1);
            }
        }
    }


    private void FlagTrack(Grid grid)
    {
        trackHead = grid;
        grid.SetFlag(2);


        //校准rect
        if (trackHead.x < (int)rect.x)
            rect.x = trackHead.x;
        if (trackHead.x > (int)rect.xMax)
            rect.xMax = rect.x;
        if (trackHead.y < (int)rect.y)
            rect.y = trackHead.y;
        if (trackHead.y > (int)rect.yMax)
            rect.yMax = trackHead.y;
    }

    private bool CheckStart(Grid grid)
    {
        if (grid.flag != 0)
            return false;

        for (int i = 0; i < 4; i++)
        {
            Vector2 os = direction4[i];
            if (TileMap.ins.gridMap[grid.x + (int)os.x, grid.y + (int)os.y].flag == 1)
            {
                return true;
            }
        }
        return false;
    }
    private bool CheckTrack(Grid grid)
    {
        if (grid.flag != 0)
            return false;

        for (int i = 0; i < 4; i++)
        {
            Vector2 os = direction4[i];
            if (trackHead == TileMap.ins.gridMap[grid.x + (int)os.x, grid.y + (int)os.y])
            {
                return true;
            }
        }
        return false;
    }
}
