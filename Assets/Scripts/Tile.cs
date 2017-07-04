/*
 * Author:  Rick
 * Create:  2017/7/4 13:34:57
 * Email:   rickjiangshu@gmail.com
 * Follow:  https://github.com/RickJiangShu
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tile
/// </summary>
public class Tile : MonoBehaviour
{
    public Grid grid;

    private SpriteRenderer renderer;
    // Use this for initialization
    void Start()
    {
        renderer = GetComponent<SpriteRenderer>();

        grid.onFlagChanged += RefreshSprite;
        RefreshSprite();
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void RefreshSprite()
    {
        switch (grid.flag)
        {
            case 0:
                renderer.sprite = null;
                break;
            case 1:
                renderer.sprite = Resources.Load<Sprite>("Base");
                break;
            case 2:
                renderer.sprite = Resources.Load<Sprite>("Track");
                break;
        }
    }
}
