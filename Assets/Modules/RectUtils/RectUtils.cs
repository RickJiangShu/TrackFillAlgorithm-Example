/*
 * Author:  Rick
 * Create:  2017/6/29 14:28:45
 * Email:   rickjiangshu@gmail.com
 * Follow:  https://github.com/RickJiangShu
 */
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 用来处理矩形的工具，这里的矩形指的是二维坐标下的矩形，而不是图形的矩形。
/// </summary>
public class RectUtils
{
#region 切割 Split
    public delegate bool CheckIsBlock(int x,int y);//检测x,y是否是阻碍

    public static List<Rectangle> Split(CheckIsBlock isBlock, Rectangle rect)
    {
        return Split(isBlock, rect.x, rect.y, rect.right, rect.bottom);
    }

    /// <summary>
    /// 将一个矩形分割成N个矩形
    /// </summary>
    public static List<Rectangle> Split(CheckIsBlock isBlock,int left,int top,int right,int bottom)
    {
        int[,] testMap = new int[right + 1, bottom + 1];
        List<Rectangle> rects = new List<Rectangle>();
        for (int y = top; y <= bottom; y++)
        {
            for (int x = left; x <= right; x++)
            {
               // Debug.Log("test x=" + x + " y=" + y + "  " + isBlock(x, y));

                if (testMap[x, y] == 0 && !isBlock(x,y))
                {
                    int r = FindRight(isBlock,testMap, x, y, right);
                    int b = FindBottom(isBlock,testMap, x, y, r, bottom);
                    Rectangle rect = new Rectangle(x, y, r - x + 1, b - y +1);
                    FilLTestMap(testMap, rect);
                    rects.Add(rect);

                    x = r;
                }
            }
        }
        return rects;
    }

    /// <summary>
    /// 找到最优
    /// </summary>
    /// <param name="isBlock"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    private static int FindRight(CheckIsBlock isBlock,int[,] testMap,int left, int top, int right)
    {
        int x = left;
        for (int y = top; x < right; x++)
        {
            if (testMap[x + 1, y] == 1 || isBlock(x + 1, y))
            {
                break;
            }
        }
        return x;
    }
    private static int FindBottom(CheckIsBlock isBlock,int[,] testMap, int left, int top, int right,int bottom)
    {
        int y;
        for (y = top; y < bottom; y++)
        {
            for (int x = left; x <= right; x++)
            {
                if (testMap[x,y + 1] == 1 || isBlock(x,y + 1))
                {
                    return y;
                }
            }
        }
        return y;
    }

    private static void FilLTestMap(int[,] testMap,Rectangle rect)
    {
        for (int y = rect.y; y <= rect.bottom; y++)
        {
            for (int x = rect.x; x <= rect.right; x++)
            {
                testMap[x, y] = 1;
            }
        }
    }
#endregion
}
