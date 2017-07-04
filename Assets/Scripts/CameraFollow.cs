using UnityEngine;
using System.Collections;

public class CameraFollow : MonoBehaviour
{
    private const float Speed = 5.0f;
    public Transform m_Target;

    private float defaultZ;
    // Use this for initialization
    void Start()
    {
        defaultZ = transform.position.z;



#if UNITY_EDITOR
        GetComponent<AudioSource>().enabled = false;
#endif
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (m_Target == null)
            return;
        Vector3 targetPosition = m_Target.position;
        targetPosition.z = defaultZ;

        /*
        Vector3 diff = targetPosition - transform.position;
        if (diff.magnitude < Speed)
        {
            transform.position = targetPosition;
        }
        else
        {
            transform.position = diff.normalized * Speed;
        }
         */

        transform.position = targetPosition;
    }
}
