using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class NetworkObjectDestroyer : Singleton<NetworkObjectDestroyer>
{
    public void DestroyNetObjWithDelay(NetworkObject netObj, float delay)
    {
        StartCoroutine(DelayedDespawn(netObj, delay));
    }

    private IEnumerator DelayedDespawn(NetworkObject netObj, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (netObj != null && netObj.IsSpawned)
        {
            netObj.Despawn(true);
        }
        else
        {
            Debug.LogWarning("Object is no longer valid or was already despawned.");
        }
    }
}
