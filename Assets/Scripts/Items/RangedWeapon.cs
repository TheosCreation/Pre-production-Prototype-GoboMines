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

    [SerializeField] private Beam beamPrefab;
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

        AttackServerRpc();

        ApplyRecoil();

        // play vfx for casings and muzzleflash
        if (casingParticle != null)
        {
            casingParticle.Play();
        }
    }


    [ServerRpc]
    private void AttackServerRpc()
    {
        Vector3 firePosition = player.playerLook.playerCamera.transform.position;

        Vector3 direction = player.playerLook.playerCamera.transform.forward;

        RaycastHit hit;
        if (Physics.Raycast(firePosition, direction, out hit))
        {
            Beam revolverBeam = Instantiate(beamPrefab, muzzleTransform.position, Quaternion.LookRotation(direction));
            revolverBeam.GetComponent<NetworkObject>().Spawn();
            revolverBeam.DrawBeamClientRpc(hit.point);
            if(hit.collider.TryGetComponent<IDamageable>(out IDamageable hitDamageable))
            {
                hitDamageable.TakeDamage(damage, player);
                SpawnHitParticles(hit.point, hit.normal, hitDamageable.HitParticlePrefab);
            }
            else
            {
                SpawnHitParticles(hit.point, hit.normal, GameManager.Instance.prefabs.hitWallPrefab);
            }
        }
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
        return ammo > 0 && !isReloading.Value && !isJammed;
    }

    public override void CantAttackAction()
    {
        base.CantAttackAction();

        if (isJammed)
        {
            attackingAudioSource.PlayOneShot(jammedSound);
        }
        else
        {
            StartReload();
        }
    }


    public override void TryFixAttackingAction()
    {
        if (isUnJamming) return;

        if (isJammed)
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
            FinishUnJamming();
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

    protected void SpawnHitParticles(Vector3 hitPosition, Vector3 wallNormal, ParticleSystem particleToSpawn)
    {
        if (!IsServer) return; // Only the server spawns particles

        // Spawn hit particles with offset
        Vector3 particlePositionOffset = wallNormal * 0.1f;
        ParticleSystem hitParticles = Instantiate(particleToSpawn, hitPosition + particlePositionOffset, Quaternion.LookRotation(wallNormal)).GetComponent<ParticleSystem>();
        NetworkObject netObj = hitParticles.GetComponent<NetworkObject>();
        netObj.Spawn(true);

        float duration = hitParticles.main.duration + hitParticles.main.startLifetime.constantMax;
        NetworkObjectDestroyer.Instance.DestroyNetObjWithDelay(netObj, duration);
    }
}