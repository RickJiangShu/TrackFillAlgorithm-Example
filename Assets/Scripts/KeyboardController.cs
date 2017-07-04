using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Tracker))]
public class KeyboardController : MonoBehaviour {

    private const float SlideDistance = 0.5f;

    private Tracker m_Player;
	// Use this for initialization
	void Start () {
        m_Player = GetComponent<Tracker>();
	}
	
	// Update is called once per frame
	void Update () {
        Direction direction = Direction.None;


#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        if (Input.GetKeyDown(KeyCode.LeftArrow))
            direction = Direction.Left;
        else if (Input.GetKeyDown(KeyCode.UpArrow))
            direction = Direction.Up;
        else if (Input.GetKeyDown(KeyCode.RightArrow))
            direction = Direction.Right;
        else if (Input.GetKeyDown(KeyCode.DownArrow))
            direction = Direction.Down;
#endif

        /*
#elif UNITY_ANDROID || UNITY_IPHONE
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Moved)
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");
            float absX = Mathf.Abs(mouseX);
            float absY = Mathf.Abs(mouseY);
            if (absX > SlideDistance || absY > SlideDistance)
            {
                if (absX > absY)
                    m_Player.SetChangeDirection(mouseX > 0f ? Direction.Right : Direction.Left);
                else
                    m_Player.SetChangeDirection(mouseY > 0f ? Direction.Up : Direction.Down);
            }
        }
 */
        if (direction != Direction.None)
            m_Player.SetChangeDirection(direction);
	}
}
