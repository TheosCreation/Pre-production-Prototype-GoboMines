using Unity.Netcode;
using UnityEngine;

public class Weapon : Item
{
    [Header("Equip")]
    [SerializeField] private bool isEquip = false;
    [SerializeField] private float equipTime = 0.5f;
    [SerializeField] protected AudioClip equipSound;
    protected Timer equipTimer;

    [Header("Attacking")]
    public int damage = 20;
    public bool isAttacking = false;
    public float attacksPerSecond = 1.0f;
    protected float attackTimer = 0.0f;
    [SerializeField] protected AudioClip[] attackingSounds;
    protected float lastAttackTime = 0f;

    [Header("Reload")]
    public NetworkVariable<bool> isReloading = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public int magSize = 45;
    public float reloadTime = 0.5f;

    [Header("Jamming")]
    public bool isJammed = false;
    public bool isUnJamming = false;
    [SerializeField] protected AudioClip jammedSound;
    private Timer unJamTimer;
    [Range(0.0f, 1.0f)] public float jamChance = 0.3f;
    public float unJamTime = 0.3f;

    [Header("Screen Shake")]
    [Range(0.0f, 0.1f)] public float screenShakeDuration = 0.1f;
    [Range(0.0f, 0.1f)] public float screenShakeAmount = 0.1f;
    [Header("Pick Up")]
    public bool canPickup = true; // moved up to settings tab so easily visable

    [Header("Audio")]
    [SerializeField] protected AudioSource attackingAudioSource;
    [SerializeField] protected AudioSource otherAudioSource;

    protected Animator animator;
    protected BoxCollider bc;
    protected Rigidbody rb;
    protected Timer pickupTimer;
    protected PlayerController player;

    protected virtual void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        bc = GetComponent<BoxCollider>();
        equipTimer = gameObject.AddComponent<Timer>();
        pickupTimer = gameObject.AddComponent<Timer>();
        unJamTimer = gameObject.AddComponent<Timer>();
        player = GetComponentInParent<PlayerController>();   
    }


    protected void Update()
    {
        if (!IsOwner) return;

        attackTimer -= Time.deltaTime;

        if (attackTimer < 0.0f && isAttacking && isEquip && !isJammed)
        {
            //use jamChance and set isJammed to true, we cannot fire when gun is jammed
            attackTimer = CalculateAttackRate();
            if (CanAttack())
            {
                Attack();
                CheckForJam();
            }
            else
            {
                CantAttackAction();
            }
        }
    }

    protected void Jam()
    {
        isJammed = true;
        animator.SetBool("Jammed", true);
        otherAudioSource.PlayOneShot(jammedSound);
    }

    public override void StartSpecialAction()
    {
        if(!IsOwner) return;

        if (isJammed && !isUnJamming)
        {
            isUnJamming = true;
            unJamTimer.SetTimer(unJamTime, FinishUnJamming);
            animator.SetBool("Jammed", false);
        }
    }

    private void FinishUnJamming()
    {
        isUnJamming = false;
        isJammed = false;
    }

    protected void CheckForJam()
    {
        if (Random.value < jamChance)
        {
            Jam();
        }
    }

    protected virtual bool CanAttack()
    {
        return true;
    }
    public override void CantAttackAction()
    {
        if (!IsOwner) return;
    }

    private void OnEnable()
    {
        Equip();
    }

    private void OnDisable()
    {
        if (!IsOwner) return;

        equipTimer.StopTimer();
        isEquip = false;
    }

    public virtual void Attach()
    {
        bc.enabled = false;
        rb.isKinematic = true;
        transform.localRotation = Quaternion.identity;
    }
    public virtual void UseAbility()
    {

    }

    public override void Equip()
    {
        player.networkedAnimator.Animator = animator;

        if (!IsOwner) return;

        equipTimer.SetTimer(equipTime, EquipFinish);
        otherAudioSource.PlayOneShot(equipSound);

        //recoil = Vector2.zero;
    }

    protected void EquipFinish()
    {
        isEquip = true;
    }

    public void Throw(Vector3 direction, float throwForce, float pickUpDelay)
    {
        transform.SetParent(null);

        transform.localScale = Vector3.one;

        rb.isKinematic = false;
        rb.AddForce(direction * throwForce, ForceMode.VelocityChange);

        bc.enabled = true;

        canPickup = false;
        //stop timer incase of repeat
        pickupTimer.StopTimer();
        pickupTimer.SetTimer(pickUpDelay, () => canPickup = true);

        isAttacking = false;

        //disable script just like unequiping the weapon
        this.enabled = false;

    }

    public bool PickUp(ItemHolder itemHolder, PlayerController user, bool ignorePickup)
    {
        if (!canPickup && !ignorePickup) return false;

        // if the player isnt holding to many weapons we dont add it
        if(itemHolder.Add(this))
        {
            player = user;
        
            canPickup = false;
        
            Attach();
        
            return true;
        }
        return false;
    }

    protected float CalculateAttackRate()
    {
        return 1 / attacksPerSecond;
    }

    public override void StartAttacking()
    {
        if (!IsOwner) return;

        isAttacking = true;
    }

    protected virtual void Attack()
    {
        if (!IsOwner) return;

        AttackServerRpc();
        PlayAttackSound();
        player.playerLook.TriggerScreenShake(screenShakeDuration, screenShakeAmount);

        lastAttackTime = Time.time;

        player.networkedAnimator.SetTrigger("Attack");
    }


    [ServerRpc]
    protected virtual void AttackServerRpc()
    {
        // Call the ClientRpc to play particles on all clients
        AttackClientRpc();
    }

    [ClientRpc]
    protected virtual void AttackClientRpc()
    {

    }

    protected void PlayAttackSound()
    {
        if (attackingSounds.Length > 0 && attackingAudioSource != null)
        {
            AudioClip randomClip = attackingSounds[UnityEngine.Random.Range(0, attackingSounds.Length)];
            attackingAudioSource.PlayOneShot(randomClip);
        }
    }

    public override void EndAttacking()
    {
        if (!IsOwner) return;

        isAttacking = false;
    }
}