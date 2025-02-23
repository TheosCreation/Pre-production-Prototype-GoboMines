using Unity.Netcode;
using UnityEngine;

// Ingame version of the item thats why it inherits from monobehaviour
public class Item : NetworkBehaviour
{
    public Transform IKRightHandPos;
    public Transform IKLeftHandPos;
    public bool IkLeftHandOn = false;
    public bool IkRightHandOn = true;

    public virtual void StartAttacking() { }
    public virtual void EndAttacking() { }
    public virtual void StartAltAction() { }
    public virtual void EndAltAction() { }
    public virtual void CantAttackAction() { }

    public virtual void StartSpecialAction() { }

    public virtual void Equip() { }
}