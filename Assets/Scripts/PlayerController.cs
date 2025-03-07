using System;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

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

    [SerializeField] private AudioClip[] hitSounds;
    public AudioClip[] HitSounds { get => hitSounds; set => hitSounds = value; }
    [SerializeField] private AudioClip deathSound;
    [SerializeField] private AudioSource aud;

    [SerializeField] private int health = 100;

    [SerializeField] private float damageCooldown = 1f; 
    private float lastDamageTime = -Mathf.Infinity;
    public int Health { 
        get => health;
        set
        {
            health = value;
            if (health < 0)
            {
                Die();
            }

        }
    }

    private void Die()
    {
        if (!IsOwner)
        {
            return;
        }

        LocalClientHandler.Instance.TempCamera(true);
        LocalClientHandler.Instance.SetCameraToPlayer(0);

        if (NetworkSpawnHandler.Instance != null)
        {
            NetworkSpawnHandler.Instance.MarkPlayerToRespawnServerRpc(OwnerClientId, default);
        }

        NetworkSpawnHandler.Instance.SpawnSound(deathSound, transform.position);

        NetworkObject.Despawn(gameObject);
        Destroy(gameObject);
    }


    private void OnInteractStarted(InputAction.CallbackContext ctx) => Interact();
    private void OnInventoryStarted(InputAction.CallbackContext ctx) => OpenCloseInventory();

    private void Awake()
    {
        playerLook = GetComponent<PlayerLook>();
        itemHolder = GetComponent<ItemHolder>();
        inventory = GetComponent<Inventory>();

        InputManager.Instance.Input.Player.Interact.started += OnInteractStarted;
        InputManager.Instance.Input.UI.Inventory.started += OnInventoryStarted;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();


        InputManager.Instance.Input.Player.Interact.started -= OnInteractStarted;
        InputManager.Instance.Input.UI.Inventory.started -= OnInventoryStarted;
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
        if(!IsOwner) return;

        UiManager.Instance.ToggleInventory();
    }

    public void TakeDamage(int amount, PlayerController fromPlayer)
    {
        if(!IsOwner) return;

        if (Time.time - lastDamageTime < damageCooldown)
        {
            return;
        };

        lastDamageTime = Time.time;

        ulong attackerId = fromPlayer.OwnerClientId;
        Health -= amount;
        playerLook.TriggerScreenShake(0.2f, amount * 0.005f);
        UiManager.Instance.playerHud.damageFlash.Play();
    }

    public void TakeDamage(int amount, GameObject fromObject)
    {
        if (!IsOwner) return;

        if (Time.time - lastDamageTime < damageCooldown)
        {
            return;
        }

        lastDamageTime = Time.time;
        Health -= amount;
        playerLook.TriggerScreenShake(0.2f, amount*0.005f);
        UiManager.Instance.playerHud.damageFlash.Play();
    }

    public void DropAllItems()
    {
        inventory.DropAllItems();
    }
}
