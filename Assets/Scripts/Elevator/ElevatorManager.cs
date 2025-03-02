using UnityEngine;

public class ElevatorManager : MonoBehaviour
{
    [SerializeField] private Transform pointA;
    [SerializeField] private Transform pointB;
    [SerializeField] private float elevatorSpeed = 2f;
    private bool movingUp = true;
    private bool isMoving = false;
    private float t = 0f;

    private void Update()
    {
        if (isMoving)
        {
            MoveElevator();
        }
    }

    private void MoveElevator()
    {
        t += Time.deltaTime * elevatorSpeed;

        if (movingUp)
            transform.position = new Vector3(transform.position.x, Vector3.Lerp(pointA.position, pointB.position, t).y, transform.position.z);
        else
            transform.position = new Vector3(transform.position.x, Vector3.Lerp(pointB.position, pointA.position, t).y, transform.position.z);

        if (t >= 1f)
        {
            isMoving = false;
            t = 0f;
        }
    }

    public void ToggleElevator()
    {
        if (!isMoving)
        {
            isMoving = true;
            movingUp = !movingUp;
        }
    }
}