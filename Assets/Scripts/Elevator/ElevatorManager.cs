using UnityEngine;

public class ElevatorManager : MonoBehaviour
{
    private bool isMoving = false;
    [SerializeField] private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void ToggleElevator()
    {
        if (!isMoving)
        {
            animator.SetTrigger("Move");
            isMoving = true;
        }
    }

    public void FinishMoving()
    {
        isMoving = false;
    }
}