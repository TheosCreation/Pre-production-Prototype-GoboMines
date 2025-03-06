using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.InputSystem;

public class ItemHolder : NetworkBehaviour
{
    [SerializeField] private List<Item> currentHoldableItems = new List<Item>();
    public Transform idlePosition;
    [SerializeField] private Item currentItem;
    private int currentItemIndex = 0;
    [SerializeField] private float throwForce = 0.1f;
    [SerializeField] private float scrollSwitchDelay = 0.1f;
    private NetworkVariable<int> currentItemIndexNetwork = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);


    [Header("Left Hand Target")]
    [SerializeField] private TwoBoneIKConstraint leftHandIK;
    [SerializeField] private Transform leftHandTarget;

    [Header("Right Hand Target")]
    [SerializeField] private TwoBoneIKConstraint rightHandIK;
    [SerializeField] private Transform rightHandTarget;

    public bool isSwitching = false;


    // Input event handlers
    private void OnItemSwitch(InputAction.CallbackContext ctx) => ItemSwitch(ctx.ReadValue<Vector2>());
    private void OnAttackStarted(InputAction.CallbackContext ctx) => currentItem?.StartAttacking();
    private void OnAttackCanceled(InputAction.CallbackContext ctx) => currentItem?.EndAttacking();
    private void OnAltActionStarted(InputAction.CallbackContext ctx) => currentItem?.StartAltAction();
    private void OnAltActionCanceled(InputAction.CallbackContext ctx) => currentItem?.EndAltAction();
    private void OnCantAttackActionStarted(InputAction.CallbackContext ctx) => currentItem?.TryFixAttackingAction();
    private void OnSpecialActionStarted(InputAction.CallbackContext ctx) => currentItem?.StartSpecialAction();
    private void OnDropItemStarted(InputAction.CallbackContext ctx) => DropCurrentItem();

    
    private void Awake()
    {
        InputManager.Instance.Input.Player.ItemSwitch.performed += OnItemSwitch;
        InputManager.Instance.Input.Player.Attack.started += OnAttackStarted;
        InputManager.Instance.Input.Player.Attack.canceled += OnAttackCanceled;
        InputManager.Instance.Input.Player.AltAction.started += OnAltActionStarted;
        InputManager.Instance.Input.Player.AltAction.canceled += OnAltActionCanceled;
        InputManager.Instance.Input.Player.CantAttackAction.started += OnCantAttackActionStarted;
        InputManager.Instance.Input.Player.SpecialAction.started += OnSpecialActionStarted;
        InputManager.Instance.Input.Player.DropItem.started += OnDropItemStarted;

    }
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        currentItemIndexNetwork.OnValueChanged += OnItemIndexChanged;

        // Select the current item based on the network variable
        SelectItem(currentItemIndexNetwork.Value);
        foreach (Item item in currentHoldableItems)
        {
            item.Attach(idlePosition);
        }

        if (IsOwner)
        {
            currentItem?.Equip();
        }
    }

    public override void OnDestroy()
    {
        base.OnDestroy();

        if (InputManager.Instance != null)
        {
            InputManager.Instance.Input.Player.ItemSwitch.performed -= OnItemSwitch;
            InputManager.Instance.Input.Player.Attack.started -= OnAttackStarted;
            InputManager.Instance.Input.Player.Attack.canceled -= OnAttackCanceled;
            InputManager.Instance.Input.Player.AltAction.started -= OnAltActionStarted;
            InputManager.Instance.Input.Player.AltAction.canceled -= OnAltActionCanceled;
            InputManager.Instance.Input.Player.CantAttackAction.started -= OnCantAttackActionStarted;
            InputManager.Instance.Input.Player.SpecialAction.started -= OnSpecialActionStarted;
        }
    }
    private void Update()
    {
        if (currentItem == null)
        {
            DisableAllIK();
            return;
        }

        UpdateHandTargets();
    }

    private void ItemSwitch(Vector2 direction)
    {
        if (!IsOwner || isSwitching || currentHoldableItems.Count == 0) return;

        StartCoroutine(ItemSwitchDelayed(direction));
    }

    private IEnumerator ItemSwitchDelayed(Vector2 direction)
    {
        isSwitching = true;

        try
        {
            int totalItems = currentHoldableItems.Count;

            if (direction.y > 0)
            {
                currentItemIndex = (currentItemIndex + 1) % totalItems;
            }
            else if (direction.y < 0)
            {
                currentItemIndex = (currentItemIndex - 1 + totalItems) % totalItems;
            }

            RequestWeaponSwitchServerRpc(currentItemIndex);

            yield return new WaitForSeconds(scrollSwitchDelay);
        }
        finally
        {
            isSwitching = false;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestWeaponSwitchServerRpc(int newItemIndex)
    {
        if (currentItemIndexNetwork.Value == newItemIndex)
        {
            // Temporarily set to an invalid index before setting it back to trigger an update
            currentItemIndexNetwork.Value = -1;
        }

        currentItemIndexNetwork.Value = newItemIndex;
    }

    private void OnItemIndexChanged(int oldValue, int newValue)
    {
        // Update the weapon when the NetworkVariable changes
        SelectItem(newValue);
    }
    private void SelectItem(int newValue)
    {
        if (newValue >= 0 && newValue < currentHoldableItems.Count)
        {
            currentItem = currentHoldableItems[newValue];
        }
        else
        {
            // Optionally handle invalid indices; for example, disable the current item
            currentItem = null;
        }

        foreach (var item in currentHoldableItems)
        {
            item.gameObject.SetActive(item == currentItem);
        }
    }


    private void UpdateHandTargets()
    {
        // Update hand targets for both left and right sides
        if (currentItem != null && currentItem.IkLeftHandOn)
        {
            SetLeftHandIKWeight(1f);
            SetHandTarget(leftHandTarget, currentItem.IKLeftHandPos);
        }
        else
        {
            SetLeftHandIKWeight(0f);
        }

        if (currentItem != null && currentItem.IkRightHandOn)
        {
            SetRightHandIKWeight(1f);
            SetHandTarget(rightHandTarget, currentItem?.IKRightHandPos);
        }
        else
        {
            SetRightHandIKWeight(0f);
        }
    }

    private void DisableAllIK()
    {
        SetLeftHandIKWeight(0);
        SetRightHandIKWeight(0);
    }
    private void SetLeftHandIKWeight(float weight)
    {
        leftHandIK.weight = weight;
    }
    private void SetRightHandIKWeight(float weight)
    {
        rightHandIK.weight = weight;
    }
    private void SetHandTarget(Transform handTarget, Transform ikHandPos)
    {
        if (handTarget != null && ikHandPos != null)
        {
            handTarget.position = ikHandPos.position;
            handTarget.rotation = ikHandPos.rotation;
        }
    }

    public void Add(Item item)
    {
        currentHoldableItems.Add(item);
        item.Attach(idlePosition);

        // Automatically switch to the newly picked up item
        currentItemIndex = currentHoldableItems.Count - 1;
        SelectItem(currentItemIndex);
    }

    public void DropCurrentItem()
    {
        if (!IsOwner || currentItem == null) return;

        // Remove the item from the list first
        currentHoldableItems.Remove(currentItem);

        // Drop the item by calling Throw
        currentItem.Throw(transform.forward, throwForce);

        // Set currentItem to null
        currentItem = null;

        // If there are still items left, switch to another one
        if (currentHoldableItems.Count > 0)
        {
            currentItemIndex = Mathf.Clamp(currentItemIndex, 0, currentHoldableItems.Count - 1);
            SelectItem(currentItemIndex);
        }
    }
}
