using Unity.Netcode;
using UnityEngine;

// Ingame version of the item thats why it inherits from monobehaviour
public class Item : NetworkBehaviour, IInteractable
{
    public Transform IKRightHandPos;
    public Transform IKLeftHandPos;
    public bool IkLeftHandOn = false;
    public bool IkRightHandOn = true;
    public bool isHeld = false;

    protected BoxCollider bc;
    protected Rigidbody rb;

    protected virtual void Awake()
    {
        bc = GetComponent<BoxCollider>();
        rb = GetComponent<Rigidbody>();
    }

    public virtual void StartAttacking() { }
    public virtual void EndAttacking() { }
    public virtual void StartAltAction() { }
    public virtual void EndAltAction() { }
    public virtual void CantAttackAction() { }

    public virtual void StartSpecialAction() { }

    public virtual void Equip() { }

    public void Interact(PlayerController player)
    {
        player.itemHolder.Add(this);
    }

    public virtual void Attach(Transform transformToAttach)
    {
        transform.SetParent(transformToAttach);

        bc.enabled = false;
        rb.isKinematic = true;
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        isHeld = true;
    }

    public void Throw(Vector3 direction, float throwForce)
    {
        transform.SetParent(null);

        transform.localScale = Vector3.one;

        rb.isKinematic = false;
        rb.AddForce(direction * throwForce, ForceMode.VelocityChange);

        bc.enabled = true;

        isHeld = false;

    }
}