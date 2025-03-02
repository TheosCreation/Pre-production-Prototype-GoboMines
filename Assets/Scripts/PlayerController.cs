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
    private void Interact()
    {
        RaycastHit hit;
        if (Physics.Raycast(playerLook.playerCamera.transform.position, playerLook.playerCamera.transform.forward, out hit, interactDistance, interactMask))
        {
            // Check if the hit object has an Interactable component
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();
            if (interactable != null)
            {
                interactable.Interact(this);
            }
            else
            {
                Debug.Log("Hit object is not interactable: " + hit.collider.name);
            }
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
}
