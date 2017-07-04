/*
 * Author:  Rick
 * Create:  2017/7/4 13:40:56
 * Email:   rickjiangshu@gmail.com
 * Follow:  https://github.com/RickJiangShu
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// UI
/// </summary>
public class UI : MonoBehaviour
{
    void OnGUI()
    {
        if (GUI.Button(new Rect(0, 0, 150, 30), "Switch Grid/Tile"))
        {
            Debug.Log("Clicked the button with text");
        }

    }
}
