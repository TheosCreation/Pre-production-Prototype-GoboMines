using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] private SkinnedMeshRenderer[] thridPersonRenderers;
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if(IsOwner)
        {
            foreach (SkinnedMeshRenderer renderer in thridPersonRenderers)
            {
                renderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            }
        }    
    }
    // instead of passing in ore type we do generic item
    public void AddToInventory(OreType oreType, float amount)
    {
    }
}
