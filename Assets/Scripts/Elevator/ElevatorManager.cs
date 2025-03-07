using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class ElevatorManager : NetworkBehaviour
{
    private NetworkVariable<bool> isMoving = new NetworkVariable<bool>(false);
    [SerializeField] private NetworkAnimator animator;
    private bool isUp = true;


    private void Awake()
    {
        animator = GetComponent<NetworkAnimator>();
    }

    [ServerRpc(RequireOwnership = false)]
    public void ToggleElevatorServerRpc()
    {
        if (!isMoving.Value)
        {
            animator.SetTrigger("Move");
            isMoving.Value = true;
        }
    }

    public void FinishMoving()
    {
        isMoving.Value = false;
        isUp = !isUp;
        if (isUp)
        {
            GameManager.Instance.EndDay();
        }
        else
        {
            GameManager.Instance.StartDay();
        }
    }
}