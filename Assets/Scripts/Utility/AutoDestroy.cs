using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class AutoDestroy : NetworkBehaviour
{

    [SerializeField] private float timeToDestroy = 3f;

    public override void OnNetworkDespawn()
    {
        // we only call despawn on server so no need to be active on clients
        if(!IsServer)
            enabled = false;
    }


    private void Update()
    {
        // we only call despawn on server
        if (!IsServer) return;
        timeToDestroy -= Time.deltaTime;
        if(timeToDestroy <= 0)
        {
            GetComponent<NetworkObject>().Despawn();    
        }
    }

}
