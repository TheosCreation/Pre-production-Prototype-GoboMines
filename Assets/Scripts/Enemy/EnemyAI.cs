using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    [Header("State Assets (Assign in Inspector)")]
    [Tooltip("Assign all state assets here (RoamingState, ChasingState, etc.)")]
    public BaseState[] states;

    private IEnemyState currentState;
    private Dictionary<Type, IEnemyState> stateDictionary;

    private NavMeshAgent agent;
    private Vector3 homePosition;
    private Transform target;

    private float currentRoamRadius;
    private float currentMoveSpeed;
    private float currentRotationSpeed;
    private float currentDetectionRange;
    private float currentAttackRange;
    private float currentAttackCooldown;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        homePosition = transform.position;
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
        foreach (BaseState state in states)
        {
            Type key = state.GetType();
            if (!stateDictionary.ContainsKey(key))
            {
                stateDictionary.Add(key, state);
            }
            else
            {
                Debug.LogWarning("Duplicate state key found for type: " + key);
            }
        }
    }


    public void ChangeState<T>() where T : IEnemyState
    {
        Type key = typeof(T);
        if (!stateDictionary.TryGetValue(key, out var newState))
        {
            Debug.LogError("State of type " + key + " not found in dictionary.");
            return;
        }

        if (currentState != null)
        {
            currentState.OnExit(this);
        }
        currentState = newState;
        currentState.OnEnter(this);
    }

    public void SetRoamRadius(float radius) { currentRoamRadius = radius; }
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
}