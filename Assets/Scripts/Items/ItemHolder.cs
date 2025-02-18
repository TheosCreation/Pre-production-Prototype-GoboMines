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
    [SerializeField] private Item currentItem;
    private int currentItemIndex = 0;
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
    private void OnCantAttackActionStarted(InputAction.CallbackContext ctx) => currentItem?.CantAttackAction();

    
    private void Awake()
    {
        InputManager.Instance.Input.Player.ItemSwitch.performed += OnItemSwitch;
        InputManager.Instance.Input.Player.Attack.started += OnAttackStarted;
        InputManager.Instance.Input.Player.Attack.canceled += OnAttackCanceled;
        InputManager.Instance.Input.Player.AltAction.started += OnAltActionStarted;
        InputManager.Instance.Input.Player.AltAction.canceled += OnAltActionCanceled;
        InputManager.Instance.Input.Player.CantAttackAction.started += OnCantAttackActionStarted;

    }
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        currentItemIndexNetwork.OnValueChanged += OnItemIndexChanged;

        // Select the current item based on the network variable
        SelectItem(currentItemIndexNetwork.Value);

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

        //// Determine the transform to attach the weapon
        //Transform transformToAttachWeapon = currentWeapon.isAiming.Value && !currentWeapon.isReloading.Value ? aimingPos : idlePos;
        //
        //// Smoothly interpolate weapon's position and rotation to the target transform
        //currentWeapon.transform.position = Vector3.Lerp(currentWeapon.transform.position, transformToAttachWeapon.position, Time.deltaTime * transitionSpeed);
        //currentWeapon.transform.rotation = Quaternion.Slerp(currentWeapon.transform.rotation, transformToAttachWeapon.rotation, Time.deltaTime * transitionSpeed);
        //currentWeapon.transform.parent = transformToAttachWeapon;
        //
        //if (!IsOwner) return;
        //
        //// Adjust player zoom if aiming
        //if (currentWeapon.isAiming.Value && !currentWeapon.isReloading.Value)
        //{
        //    if (player != null)
        //    {
        //        player.playerLook.SetZoomLevel(currentWeapon.zoomLevel, currentWeapon.cameraZoomZ, 0.9f);
        //    }
        //}
        //else
        //{
        //    if (player != null)
        //    {
        //        player.playerLook.ResetZoomLevel();
        //    }
        //}
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
        if (newValue < currentHoldableItems.Count)
        {
            currentItem = currentHoldableItems[newValue];
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
}
