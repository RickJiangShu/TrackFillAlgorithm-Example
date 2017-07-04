using UnityEngine;
using System.Collections;
using System;

public class Main : MonoBehaviour
{
    public static Main ins;


    //Unity 
    public Canvas m_Canvas;
    public Map m_Map;

	public Action appLunchAction;
	public Action gameStartAction;
	public Action gameOverAction;

    // Use this for initialization
    void Awake()
    {
        ins = this;

    }

    void Start()
    {
        Application.targetFrameRate = 60;//所有平台都设为60帧

		if(appLunchAction!=null)
			appLunchAction.Invoke();

		StartGame();

    }

    // Update is called once per frame
    void Update()
    {

    }


    public static void StartGame()
    {
        ins.m_Map.gameObject.SetActive(true);

		ResetCamera();

        if (!ins.m_Map.gameStarted)
            ins.m_Map.Enter();
        else
        {
            ins.m_Map.ChangeTrackerName(Map.ins.m_Player.name, "");
			ins.m_Map.m_Player.Job = 0;
            ins.m_Map.PlayerReborn();
        }

		if(ins.gameStartAction!=null)
			ins.gameStartAction.Invoke();
			
    }

	//TODO: 可移动至Camera控制类中
	public static void ResetCamera()
	{
		Camera.main.orthographicSize=9f;
	}

}
