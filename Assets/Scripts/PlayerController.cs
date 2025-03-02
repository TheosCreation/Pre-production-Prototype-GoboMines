using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class PlayerController : NetworkBehaviour, IDamageable
{
    [SerializeField] private SkinnedMeshRenderer[] thirdPersonRenderers;
    public NetworkAnimator networkedAnimator;
    [HideInInspector] public PlayerLook playerLook;
    [HideInInspector] public ItemHolder itemHolder;
    [HideInInspector] public Inventory inventory;
    private bool isDead = false;
    [SerializeField] private float interactDistance = 2.0f;
    [SerializeField] private LayerMask interactMask;
    private IInteractable currentInteractable;
    public bool IsDead { get => isDead; set => isDead = value; }
    [SerializeField] private ParticleSystem hitParticles;
    public ParticleSystem HitParticlePrefab { get => hitParticles; set => hitParticles = value; }

    [SerializeField] private AudioClip hitSound;
    public AudioClip HitSound { get => hitSound; set => hitSound = value; }
    private int health = 100;
    public int Health { get => health; set => health = value; }
    private void OnInteractStarted(InputAction.CallbackContext ctx) => Interact();
    private void OnInventoryStarted(InputAction.CallbackContext ctx) => OpenCloseInventory();

    private void Awake()
    {
        playerLook = GetComponent<PlayerLook>();
        itemHolder = GetComponent<ItemHolder>();
        inventory = GetComponent<Inventory>();

        InputManager.Instance.Input.Player.Interact.started += OnInteractStarted;
        InputManager.Instance.Input.Player.Inventory.started += OnInventoryStarted;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();


        InputManager.Instance.Input.Player.Interact.started -= OnInteractStarted;
        InputManager.Instance.Input.Player.Inventory.started -= OnInventoryStarted;
    }
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if(IsOwner)
        {
            foreach (SkinnedMeshRenderer renderer in thirdPersonRenderers)
            {
                renderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            }
        }
    }

    private void FixedUpdate()
    {
        if (IsOwner)
        {
            CheckInteract();
        }
    }

    private void CheckInteract()
    {
        RaycastHit hit;
        string interactText = "";

        if (Physics.Raycast(playerLook.playerCamera.transform.position,
                            playerLook.playerCamera.transform.forward,
                            out hit,
                            interactDistance,
                            interactMask))
        {
            if (hit.collider.TryGetComponent<IInteractable>(out var interactable))
            {
                currentInteractable = interactable;
                interactText = interactable.InteractionText;
            }
            else
            {
                currentInteractable = null;
            }
        }
        else
        {
            currentInteractable = null;
        }

        UiManager.Instance.playerHud.UpdateInteractText(interactText);
    }

    public void Interact()
    {
        if (!IsOwner) return;

        if(currentInteractable != null)
        {
            currentInteractable.Interact(this);
        }
    }

    private void OpenCloseInventory()
    {
        UiManager.Instance.ToggleInventory();
    }

    public void TakeDamage(int amount, PlayerController fromPlayer)
    {
        ulong attackerId = fromPlayer.OwnerClientId;
        Health -= amount;
    }

    public void TakeDamage(int amount, GameObject fromObject)
    {
        throw new System.NotImplementedException();
    }
}
