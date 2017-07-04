using UnityEngine;
using System.Collections;

/// <summary>
/// 坐标转换工具集
/// </summary>
public class TransferUtils
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="camera"></param>
    /// <param name="worldPos"></param>
    /// <param name="canvas"></param>
    /// <returns>localPosition</returns>
    public static Vector2 World2UI(Vector3 worldPos, RectTransform canvas)
    { 
        Vector2 uiPort = (Vector2)Camera.main.WorldToViewportPoint(worldPos) - canvas.pivot;
        return new Vector2(uiPort.x * canvas.rect.width, uiPort.y * canvas.rect.height);
    }

    public static Vector3 UI2World(Vector3 uiPos, Camera camera, RectTransform canvas)
    {
        float width = canvas.rect.width * 0.5f;
        float height = canvas.rect.height * 0.5f;
        Vector3 viewport = new Vector3((uiPos.x / width + 1f) / 2, (uiPos.y / height + 1f) / 2, uiPos.z);
        return camera.ViewportToWorldPoint(viewport);
    }
}
