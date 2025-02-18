using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.Rendering;

public class PlayerController : NetworkBehaviour, IDamageable
{
    [SerializeField] private SkinnedMeshRenderer[] thirdPersonRenderers;
    public NetworkAnimator networkedAnimator;
    public PlayerLook playerLook;
    private bool isDead = false;
    public bool IsDead { get => isDead; set => isDead = value; }
    [SerializeField] private ParticleSystem hitParticles;
    public ParticleSystem HitParticlePrefab { get => hitParticles; set => hitParticles = value; }

    [SerializeField] private AudioClip hitSound;
    public AudioClip HitSound { get => hitSound; set => hitSound = value; }

    private void Awake()
    {
        playerLook = GetComponent<PlayerLook>();
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

    List<ItemSO> items = new List<ItemSO>();

    

    // instead of passing in ore type we do generic item
    public void AddToInventory(OreSO ore, float amount)
    {
        if(items.Contains(ore))
        {
            // OreSO ore = items.Find(ore);
            //UiManager.Instance.NotifyItem(ore);
        }
    }

    public void TakeDamage(float amount, ulong attackerId)
    {
    }
}
