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

    private int ammoReserve = 500;
    private int ammoInMag = 0;
    public int AmmoInMag
    {
        get => ammoInMag;
        set
        {
            ammoInMag = value;
            if(ammoInMag == 0)
            {
                animator.SetBool("Empty", true);
            }
            else
            {
                animator.SetBool("Empty", false);
            }
            UiManager.Instance.playerHud.UpdateAmmo(ammoInMag, ammoReserve);
        }
    }

    protected override void Awake()
    {
        base.Awake();

        ammoInMag = magSize;
        reloadTimer = gameObject.AddComponent<Timer>();
    }

    public override void Equip()
    {
        base.Equip();


        UiManager.Instance.playerHud.UpdateAmmo(ammoInMag, ammoReserve);
        isReloading.Value = false;
        isAttacking = false;

        //UiManager.Instance.playerHud.SetAmmo(true);
        //UiManager.Instance.playerHud.UpdateAmmoCount(ammo, magSize);
    }


    protected override void Attack()
    {
        base.Attack();

        AmmoInMag--;

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
        return ammoInMag > 0 && !isReloading.Value && !isJammed;
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
        // Only start reloading if there's room in the magazine, you're not already reloading, and there’s ammo in reserve.
        if (ammoInMag < magSize && !isReloading.Value && ammoReserve > 0)
        {
            isReloading.Value = true;
            FinishUnJamming();
            reloadTimer.SetTimer(reloadTime, FinishReload);
            player.networkedAnimator.SetTrigger("Reload");
            if (ammoInMag == 0)
            {
                otherAudioSource.PlayOneShot(reloadEmptySound);
            }
            else
            {
                otherAudioSource.PlayOneShot(reloadSound);
            }
        }
    }

    private void FinishReload()
    {
        if (isReloading.Value)
        {
            int bulletsNeeded = magSize - ammoInMag;
            int bulletsToReload = Mathf.Min(ammoReserve, bulletsNeeded);
            AmmoInMag += bulletsToReload;
            ammoReserve -= bulletsToReload;

            isReloading.Value = false;
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