using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Grids
{
    private int width;
    private int height;
    private Grid[,] xy;

    public Grids(int w,int h)
    {
        width = w;
        height = h;
        xy = new Grid[h, w];

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                xy[x, y] = new Grid(x, y);
            }
        }
    }

    public int Width
    {
        get { return width; }
    }
    public int Height
    {
        get { return height; }
    }
    public Grid this[int x, int y]
    {
        get {
#if UNITY_EDITOR
            try
            {
                return xy[x, y]; 
            }
            catch (IndexOutOfRangeException e)
            {
                Debug.LogError("Grids索引错误 x=" + x + " y=" + y);
                return xy[0,0];
            }
#else
            return xy[x, y];
#endif
        }
    }

	public Grid getGrid(int x, int y)
	{
		if(x >= 0 && y >= 0 && x < Map.Width && y < Map.Height)
			return xy[x,y];
		else
			return null;
	}

    public Grid WorldPosition2Grid(float wx,float wy)
    {
        if (wx < 0 || -wy < 0)
            return null;

        int gx = (int)(wx / 0.01 / Tracker.GridSize.x);
        int gy = (int)(-wy / 0.01 / Tracker.GridSize.y);

        if (gx >= width || gy >= height)
            return null;

        return this[gx, gy];
    }
    public Grid WorldPosition2Grid(Vector3 pos)
    {
        return WorldPosition2Grid(pos.x, pos.y);
    }
    public Vector2 Grid2WorldPosition(int x, int y)
    {
        float wx = x * Tracker.GridSize.x * 0.01f;
        float wy = -(y * Tracker.GridSize.y * 0.01f);
        return new Vector2(wx, wy);
    }
    public Vector2 Grid2WorldPosition(Grid grid)
    {
        return Grid2WorldPosition(grid.x, grid.y);
    }

    /*
    public void Flag(int x, int y, GridFlag flag, string name = "")
    {
        this[x, y].name = name;
        this[x, y].flag = flag;
    }
     */

#region 操作格子
    /// <summary>
    /// 设置边界
    /// </summary>
    public void SetBorder(int x,int y)
    {
        this[x, y].isBorder = true;
    }

    public void SetTrack(int x, int y, TrackFlag flag, string name,Direction inDir,Direction outDir)
    {
        this[x, y].trackFlag = flag;
        this[x, y].trackOwner = name;
        this[x, y].trackInDir = inDir;
        this[x, y].trackOutDir = outDir;
    }
    public void ClearTrack(int x, int y)
    {
        SetTrack(x, y, TrackFlag.None, "", Direction.None, Direction.None);
    }
#endregion

#if DEBUG
    public void DebugTraceMap()
    {
        string output = "Grids：\n";
        for (int y = 0; y < height; y++)
        {
            output += y + "行：";
            for (int x = 0; x < width; x++)
            {
                output += (int)xy[x, y].trackFlag +",";
            }
            output += "\n";
        }
        Debug.Log(output);
    }
#endif
}

public class Grid
{
    public int x;
    public int y;

    public bool isBorder = false;//是否是“边界”

    public string groundOwner = "";//地盘拥有者

    public TrackFlag trackFlag;//轨迹类型
    public string trackOwner = "";//轨迹拥有者
    public Direction trackInDir = Direction.None;//轨迹输入方向
    public Direction trackOutDir = Direction.None;//轨迹输出方向

    public string trackerOn = "";//当前在这个格子上面的Tracker

    public Grid(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    #region 属性判断
    /// <summary>
    /// 是否为空白格
    /// </summary>
    public bool IsNone
    {
        get { return !isBorder && !HasGround && !HasTrack; }
    }

    /// <summary>
    /// 是否为边界
    /// </summary>
    public bool IsBorder
    {
        get { return isBorder; }
    }

    /// <summary>
    /// 是否有地面
    /// </summary>
    public bool HasGround
    {
        get { return groundOwner != ""; }
    }
    /// <summary>
    /// 是否有轨迹
    /// </summary>
    public bool HasTrack
    {
        get { return trackFlag != TrackFlag.None; }
    }


    /// <summary>
    /// 轨迹是否是直线
    /// </summary>
    public bool TrackIsLine
    {
        get { return trackFlag == TrackFlag.HorizontalTrack || trackFlag == TrackFlag.VerticalTrack; }
    }

    /// <summary>
    /// 轨迹是否是拐角
    /// </summary>
    public bool TrackIsCorner
    {
        get { return trackFlag == TrackFlag.TrackCorner0 || trackFlag == TrackFlag.TrackCorner1 || trackFlag == TrackFlag.TrackCorner2 || trackFlag == TrackFlag.TrackCorner3; }
    }

    /*
    public bool IsTrackOrCorner
    {
        get
        {
            return IsTrack || IsCorner;
        }
    }
    public bool IsTrack
    {
        get
        {
            return flag == GridFlag.HorizontalTrack ||
                flag == GridFlag.VerticalTrack;
        }
    }
    public bool IsCorner
    {
        get{ 
            return flag == GridFlag.TrackCorner0 || 
                flag == GridFlag.TrackCorner1 || 
                flag == GridFlag.TrackCorner2 || 
                flag == GridFlag.TrackCorner3; 
        }
    }
     */
    #endregion

    public override string ToString()
    {
        return this.x + "," + this.y;
    }
}

public enum TrackFlag
{
    None,
    HorizontalTrack,//横向轨迹
    VerticalTrack,//竖向轨迹
    TrackCorner0,//拐角 
    TrackCorner1,//
    TrackCorner2,//
    TrackCorner3,//
}

