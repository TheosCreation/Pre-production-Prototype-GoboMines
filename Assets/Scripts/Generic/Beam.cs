using Unity.Netcode;
using UnityEngine;

public class Beam : NetworkBehaviour
{
    private LineRenderer lineRenderer;
    public bool destroyWithTime = true;
    public float lifeTime = 0.5f;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        NetworkObjectDestroyer.Instance.DestroyNetObjWithDelay(NetworkObject, lifeTime);
    }

    [ClientRpc]
    public void DrawBeamClientRpc(Vector3 hitPoint)
    {
        if (lineRenderer == null) return;

        lineRenderer.SetPosition(0, transform.position); // Start of the beam
        lineRenderer.SetPosition(1, hitPoint); // End at the hit position
    }
}