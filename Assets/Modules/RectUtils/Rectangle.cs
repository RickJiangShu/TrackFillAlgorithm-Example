/*
 * Author:  Rick
 * Create:  2017/6/29 14:32:03
 * Email:   rickjiangshu@gmail.com
 * Follow:  https://github.com/RickJiangShu
 */
using System.Collections;

/// <summary>
/// 二维坐标下的矩形
/// </summary>
public struct Rectangle
{
    public int x;
    public int y;
    public int width;
    public int height;
    public int right;
    public int bottom;

    public Rectangle(int x,int y,int width,int height)
    {
        this.x = x;
        this.y = y;
        this.width = width;
        this.height = height;
        this.right = x + width - 1;
        this.bottom = y + height - 1;
    }

    public override string ToString()
    {
        return string.Format("({0}, {1}, {2}, {3})", x, y, width, height);
    }
}
