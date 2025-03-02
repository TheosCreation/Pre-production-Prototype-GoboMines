using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour, IDamageable
{
    [Header("State Assets")]
    [Tooltip("Assign all state assets here")]
    public BaseState[] states;

    [SerializeField] public IEnemyState currentState;

    private Dictionary<Type, IEnemyState> stateDictionary;

    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Vector3 homePosition;
    [SerializeField] private Transform target;
    [SerializeField] public Timer timer;

    [SerializeField] private ParticleSystem hitParticle;
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private bool isDead = false;
    [SerializeField] private int health = 100;



    private float currentMoveSpeed;
    private float currentRotationSpeed;
    private float currentDetectionRange;
    private float currentAttackRange;
    private float currentAttackCooldown;

    public ParticleSystem HitParticlePrefab { get => hitParticle; set => hitParticle = value; }
    public AudioClip HitSound { get => hitSound; set => hitSound = value; }
    public bool IsDead { get => isDead; set => isDead = value; }
    public int Health
    {
        get => health;
        set
        {
            health = value;
            if (health > 0) { return; }
            IsDead = true;
            Die();
        }
    }

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        homePosition = transform.position;

        timer = transform.AddComponent<Timer>();
        BuildStateDictionary();

        ChangeState<RoamingStateSO>();
    }

    private void Update()
    {
        currentState?.OnUpdate(this);
    }
    private void BuildStateDictionary()
    {
        stateDictionary = new Dictionary<Type, IEnemyState>();

        if (states == null || states.Length == 0)
        {
            Debug.LogError("No states assigned in the Inspector!");
            return;
        }

        foreach (BaseState state in states)
        {
            if (state == null)
            {
                Debug.LogError("Found null state in states array! Please check Inspector assignments.");
                continue;
            }

            Type key = state.GetType();
            if (!stateDictionary.ContainsKey(key))
            {
                stateDictionary.Add(key, state);
            }
            else
            {
                Debug.LogWarning($"Duplicate state key found for type: {key}");
            }
        }
    }


    public void ChangeState<T>() where T : IEnemyState
    {
        Type key = typeof(T);
        if (!stateDictionary.TryGetValue(key, out var newState))
        {
            Debug.LogError("state of type " + key + " not found in dic");
            return;
        }

        if (currentState != null)
        {
            currentState.OnExit(this);
        }
        currentState = newState;
        currentState.OnEnter(this);
    }
    public void ChangeStateByType(Type stateType)
    {
        if (!stateDictionary.TryGetValue(stateType, out var newState))
        {
            Debug.LogError("State of type " + stateType + " not found in dictionary");
            return;
        }

        if (currentState != null)
        {
            currentState.OnExit(this);
        }

        currentState = newState;
        currentState.OnEnter(this);
    }
    public void SetMoveSpeed(float speed) { currentMoveSpeed = speed; agent.speed = speed; }
    public void SetRotationSpeed(float speed) { currentRotationSpeed = speed; agent.angularSpeed = speed; }
    public void SetDetectionRange(float range) { currentDetectionRange = range; }
    public void SetAttackRange(float range) { currentAttackRange = range; }
    public void SetAttackCooldown(float cooldown) { currentAttackCooldown = cooldown; }

    public NavMeshAgent GetAgent() => agent;
    public Vector3 GetHomePosition() => homePosition;
    public Transform GetTarget() => target;
    public void SetTarget(Transform newTarget) => target = newTarget;

    public void PerformAttack()
    {
    }

    public void TakeDamage(int amount, PlayerController fromPlayer)
    {
        Health -= amount;
    }

    public void Die()
    {
        Destroy(gameObject);
    }
}