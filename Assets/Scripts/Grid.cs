/*
 * Author:  Rick
 * Create:  2017/7/4 13:37:14
 * Email:   rickjiangshu@gmail.com
 * Follow:  https://github.com/RickJiangShu
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Grid
/// </summary>
public class Grid
{
    public int x;
    public int y;
    public int flag;//0 为空 1 为Ground 2 为Track

    public event System.Action onFlagChanged;
    public void SetFlag(int value)
    {
        flag = value;
        if (onFlagChanged != null) onFlagChanged();
    }
}
