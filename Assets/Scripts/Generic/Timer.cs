using System;
using System.Collections;
using UnityEngine;

public class Timer : MonoBehaviour
{
    public bool IsRunning = false;


    public void StopTimer()
    {
        StopAllCoroutines();
    }

    // Set timer with a basic callback
    public void SetTimer(float delay, System.Action callback)
    {
        StartCoroutine(TimerCoroutine(delay, callback));
        IsRunning = true;
    }

    // Set timer with a parameterized callback
    public void SetTimer<T>(float delay, System.Action<T> callback, T parameter)
    {
        StartCoroutine(TimerCoroutine(delay, callback, parameter));
        IsRunning = true;
    }

    // Set timer to change a bool after a delay
    public void SetTimer(float delay, System.Action<bool> callback, bool parameter)
    {
        StartCoroutine(TimerCoroutine(delay, callback, parameter));
        IsRunning = true;
    }

    private IEnumerator TimerCoroutine(float delay, System.Action callback)
    {
        yield return new WaitForSeconds(delay);
        IsRunning = false;
        callback?.Invoke();
    }

    private IEnumerator TimerCoroutine<T>(float delay, System.Action<T> callback, T parameter)
    {
        yield return new WaitForSeconds(delay);
        IsRunning = false;
        callback?.Invoke(parameter);
    }

    private IEnumerator TimerCoroutine(float delay, System.Action<bool> callback, bool parameter)
    {
        yield return new WaitForSeconds(delay);
        IsRunning = false;
        callback?.Invoke(parameter);
    }

    internal void SetTimer(float attackStartDelay, object v)
    {
        throw new NotImplementedException();
    }
}