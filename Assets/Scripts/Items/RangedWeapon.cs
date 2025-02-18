using System;
using Unity.Burst.Intrinsics;
using Unity.Netcode;
using UnityEngine;
using static UnityEngine.InputSystem.LowLevel.InputStateHistory;
using Random = UnityEngine.Random;

public class RangedWeapon : Weapon
{

    [Header("Recoil")]
    public float recoilResetTimeSeconds = 0.2f;
    public float aimRecoilReduction = 0.3f;
    public Vector2 recoil;
    public Vector2[] recoilPattern;

    private int currentRecoilIndex = 0;
    [Header("Weapon Spread")]
    [SerializeField] private float spreadAmount = 0.1f;

    [Header("Projectile Settings")]
    [SerializeField] protected Transform muzzleTransform;

    [SerializeField] public Projectile projectilePrefab;
    [SerializeField] public ParticleSystem muzzleFlashParticle;
    [SerializeField] public ParticleSystem casingParticle;

    [SerializeField] protected AudioClip reloadSound;
    [SerializeField] protected AudioClip aimSound;

    private Timer reloadTimer;
    private Vector3 shotDirection;

    private int ammo = 0;
    public int Ammo
    {
        get => ammo;
        set
        {
            ammo = value;
            UiManager.Instance.playerHud.UpdateAmmoCount(ammo, magSize);
        }
    }

    protected override void Awake()
    {
        base.Awake();

        ammo = magSize;
        reloadTimer = gameObject.AddComponent<Timer>();
    }

    public override void Equip()
    {
        base.Equip();

        isReloading.Value = false;

        UiManager.Instance.playerHud.SetAmmo(true);
        UiManager.Instance.playerHud.UpdateAmmoCount(ammo, magSize);
    }


    protected override void Attack()
    {
        base.Attack();

        Ammo--;

        FireProjectile();

        ApplyRecoil();

        weaponUser.networkedAnimator.SetTrigger("Attack1");


        muzzleFlashParticle.Play();

        // play vfx for casings and muzzleflash
        if (casingParticle != null)
        {
            casingParticle.Play();
        }
        //animator.SetTrigger("Attack1");
    }

    [ClientRpc]
    protected override void AttackClientRpc()
    {
        if (IsOwner) return;

        muzzleFlashParticle.Play();

        // play vfx for casings and muzzleflash
        if (casingParticle != null)
        {
            casingParticle.Play();
        }
    }

    protected void FireProjectile()
    {
        if (IsClient || IsHost)
        {
            // Send a request to the server to spawn the projectile
            FireProjectileServerRpc();
        }
    }

    [ServerRpc]
    private void FireProjectileServerRpc()
    {
        Vector3 firePosition = weaponUser.GetFirePoint();
        if (firePosition == Vector3.zero)
        {
            firePosition = transform.position;
        }

        Vector3 direction = transform.forward;
        if (weaponUser is PlayerController)
        {
            direction = weaponUser.GetForwardDirection();
        }

        // If not aiming, add spread
        if (!isAiming.Value)
        {
            float spread = spreadAmount;
            direction.x += Random.Range(-spread, spread);
            direction.y += Random.Range(-spread, spread);
            direction.z += Random.Range(-spread, spread);
            direction.Normalize();
        }

        // Spawn the projectile on the server
        Projectile projectile = Instantiate(projectilePrefab, muzzleTransform.position, muzzleTransform.rotation);
        NetworkObject projectileNetworkObject = projectile.GetComponent<NetworkObject>();
        projectileNetworkObject.Spawn(true); // Spawn the projectile on the network

        // Initialize the projectile
        projectile.owner = gameObject;
        projectile.ownerLayer = gameObject.layer;
        projectile.Initialize(firePosition, direction, player, damage);
    }

    private void ApplyRecoil()
    {
        float aimingReduction = isAiming.Value ? aimRecoilReduction : 1f;

        // Ensure recoil is updated only when enough time has passed
        if (Time.time - lastAttackTime >= recoilResetTimeSeconds)
        {
            recoil = Vector2.zero;
            recoil += recoilPattern[0] * aimingReduction;
            currentRecoilIndex = 1;
        }
        else
        {
            recoil += recoilPattern[currentRecoilIndex] * aimingReduction;

            if (currentRecoilIndex + 1 < recoilPattern.Length)
            {
                currentRecoilIndex++;
            }
            else
            {
                currentRecoilIndex = 0;
            }
        }
    }

    public override void StartAltAction()
    {
        if (!IsOwner) return;

        isAiming.Value = true;

        //play aim in sound
        otherAudioSource.PlayOneShot(aimSound);

        //UiManager.Instance.playerHud.SetCrosshair(false);
    }

    public override void EndAltAction()
    {
        if (!IsOwner) return;

        isAiming.Value = false;

        //UiManager.Instance.playerHud.SetCrosshair(true);
    }

    protected override bool CanAttack()
    {
        return ammo > 0 && !isReloading.Value;
    }

    public override void CantAttackAction()
    {
        base.CantAttackAction();

        StartReload();
    }

    private void StartReload()
    {
        if (ammo < magSize && !isReloading.Value)
        {
            isReloading.Value = true;
            player.networkedAnimator.SetTrigger("Reload");
            //animator.SetTrigger("Reload");
            reloadTimer.SetTimer(reloadTime, FinishReload);
            otherAudioSource.PlayOneShot(reloadSound);
        }
    }

    private void FinishReload()
    {
        if (isReloading.Value)
        {
            isReloading.Value = false;
            Ammo = magSize;
        }
    }
}