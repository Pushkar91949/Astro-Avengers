using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class NetworkUIManager : NetworkBehaviour
{
    public void OnClickHost()
    {
        NetworkManager.Singleton.StartHost();
    }

    public void OnClickClient()
    {
        NetworkManager.Singleton.StartClient();
    }
}
