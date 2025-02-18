using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] private SkinnedMeshRenderer[] thirdPersonRenderers;
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if(IsOwner)
        {
            foreach (SkinnedMeshRenderer renderer in thirdPersonRenderers)
            {
                renderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            }
        }    
    }

    List<ItemSO> items = new List<ItemSO>();
    // instead of passing in ore type we do generic item
    public void AddToInventory(OreSO ore, float amount)
    {
        if(items.Contains(ore))
        {
            // OreSO ore = items.Find(ore);
            //UiManager.Instance.NotifyItem(ore);
        }
    }
}
