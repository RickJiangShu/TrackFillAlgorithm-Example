using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class NamePool
{
    private static List<string> list;

    public static void SetConfig(string cfg)
    {
        string[] arr = cfg.Split(new string[1] { "\r\n" }, System.StringSplitOptions.None);
        list = new List<string>(arr);
    }

    public static string GetName()
    {
        int i = Random.Range(0, list.Count);
        string name = list[i];
        list.RemoveAt(i);
        return name;
    }
}
