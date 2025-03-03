using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

public class RangedWeapon : Weapon
{
    [Header("Recoil")]
    public float recoilResetTimeSeconds = 0.2f;
    public Vector2 recoil;
    public Vector2[] recoilPattern;

    private int currentRecoilIndex = 0;

    [Header("Projectile Settings")]
    [SerializeField] protected Transform muzzleTransform;

    [SerializeField] public Projectile projectilePrefab;
    [SerializeField] public ParticleSystem muzzleFlashParticle;
    [SerializeField] public ParticleSystem casingParticle;

    [SerializeField] protected AudioClip reloadEmptySound;
    [SerializeField] protected AudioClip reloadSound;

    private Timer reloadTimer;
    private Vector3 shotDirection;

    private int ammo = 0;
    public int Ammo
    {
        get => ammo;
        set
        {
            ammo = value;
            //UiManager.Instance.playerHud.UpdateAmmoCount(ammo, magSize);
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
        isAttacking = false;

        //UiManager.Instance.playerHud.SetAmmo(true);
        //UiManager.Instance.playerHud.UpdateAmmoCount(ammo, magSize);
    }


    protected override void Attack()
    {
        base.Attack();

        Ammo--;

        FireProjectile();

        ApplyRecoil();


        muzzleFlashParticle.Play();

        // play vfx for casings and muzzleflash
        if (casingParticle != null)
        {
            casingParticle.Play();
        }
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
        Vector3 firePosition = player.playerLook.playerCamera.transform.position;

        Vector3 direction = player.playerLook.playerCamera.transform.forward;

        // Spawn the projectile on the server
        Projectile projectile = Instantiate(projectilePrefab, muzzleTransform.position, muzzleTransform.rotation);
        NetworkObject projectileNetworkObject = projectile.GetComponent<NetworkObject>();
        projectileNetworkObject.Spawn(true); // Spawn the projectile on the network

        // Initialize the projectile
        projectile.Initialize(firePosition, direction, player, damage);
    }

    private void ApplyRecoil()
    {
        // Ensure recoil is updated only when enough time has passed
        if (Time.time - lastAttackTime >= recoilResetTimeSeconds)
        {
            recoil = Vector2.zero;
            recoil += recoilPattern[0];
            currentRecoilIndex = 1;
        }
        else
        {
            recoil += recoilPattern[currentRecoilIndex];

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

    protected override bool CanAttack()
    {
        return ammo > 0 && !isReloading.Value;
    }

    public override void CantAttackAction()
    {
        base.CantAttackAction();

        if (isJammed && !isUnJamming)
        {
            isUnJamming = true;
            unJamTimer.SetTimer(unJamTime, FinishUnJamming);
            animator.SetBool("Jammed", false);
        }
        else
        {
            StartReload();
        }
    }

    private void StartReload()
    {
        if (ammo < magSize && !isReloading.Value)
        {
            isReloading.Value = true;
            isJammed = false;
            //animator.SetTrigger("Reload");
            reloadTimer.SetTimer(reloadTime, FinishReload);
            if (ammo == 0)
            {
                player.networkedAnimator.SetTrigger("ReloadEmpty");
                otherAudioSource.PlayOneShot(reloadEmptySound);
            }
            else
            {
                player.networkedAnimator.SetTrigger("Reload");
                otherAudioSource.PlayOneShot(reloadSound);
            }
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