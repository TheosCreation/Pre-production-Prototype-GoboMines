using System;
using System.Collections;
using UnityEngine;

public class Timer : MonoBehaviour
{
    public void StopTimer()
    {
        StopAllCoroutines();
    }

    // Set timer with a basic callback
    public void SetTimer(float delay, System.Action callback)
    {
        StartCoroutine(TimerCoroutine(delay, callback));
    }

    // Set timer with a parameterized callback
    public void SetTimer<T>(float delay, System.Action<T> callback, T parameter)
    {
        StartCoroutine(TimerCoroutine(delay, callback, parameter));
    }

    // Set timer to change a bool after a delay
    public void SetTimer(float delay, System.Action<bool> callback, bool parameter)
    {
        StartCoroutine(TimerCoroutine(delay, callback, parameter));
    }

    private IEnumerator TimerCoroutine(float delay, System.Action callback)
    {
        yield return new WaitForSeconds(delay);
        callback?.Invoke();
    }

    private IEnumerator TimerCoroutine<T>(float delay, System.Action<T> callback, T parameter)
    {
        yield return new WaitForSeconds(delay);
        callback?.Invoke(parameter);
    }

    private IEnumerator TimerCoroutine(float delay, System.Action<bool> callback, bool parameter)
    {
        yield return new WaitForSeconds(delay);
        callback?.Invoke(parameter);
    }

    internal void SetTimer(float attackStartDelay, object v)
    {
        throw new NotImplementedException();
    }
}