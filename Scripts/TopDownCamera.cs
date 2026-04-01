using UnityEngine;

public class TopDownCamera : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0, 20, 0);

    void LateUpdate()
    {
        if (target != null)
        {
            transform.position = target.position + offset;
            transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }
    }
}
