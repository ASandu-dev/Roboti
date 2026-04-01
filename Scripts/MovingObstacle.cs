using UnityEngine;

public class MovingObstacle : MonoBehaviour
{
    public Vector3 endPosition;
    public float speed = 2f;

    private Vector3 startPosition;
    private bool movingToEnd = true;

    void Start()
    {
        startPosition = transform.position;
    }

    void Update()
    {
        Vector3 target = movingToEnd ? endPosition : startPosition;
        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, target) < 0.1f)
        {
            movingToEnd = !movingToEnd;
        }
    }

    void OnDrawGizmosSelected()
    {
        // Draw a line in the editor to show the movement path
        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(transform.position, endPosition);
    }
}
