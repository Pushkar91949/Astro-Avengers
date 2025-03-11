using System.Runtime.InteropServices.WindowsRuntime;
using Unity.Netcode;
using UnityEngine;

public class ClientConnectionManager : SingletonNetworkPersistent<ClientConnectionManager> 
{
    [SerializeField]
    private int m_maxConnection;

    [SerializeField]
    private CharacterDataSO[] m_characterDatas;


    public bool IsExtraClient(ulong clientId)
    {
        return CanConnect(clientId);
    }

    public bool CanClientConnect(ulong clientID)
    {
        if(!IsServer) return false;

        bool canConnect = CanConnect(clientID);
        if(!canConnect) RemoveClient(clientID);

        return canConnect;
    }

    private void RemoveClient(ulong clientID)
    {
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { clientID },
            }

        };

    }

    [ClientRpc]
    private void ShutdownClientRPC(ClientRpcParams clientRpcParams = default)
    {
        NetworkManager.Singleton.Shutdown();
        LoadingSceneManager.Instance.LoadScene(SceneName.Menu, false);
    }

    private bool CanConnect(ulong clientID)
    {
        if(LoadingSceneManager.Instance.SceneActive == SceneName.CharacterSelection) 
        { 
            int playersConnected = NetworkManager.Singleton.ConnectedClientsList.Count;

            if(playersConnected > m_maxConnection)
            {
                return false;
            }
            return true;
        }
        else
        {
            if(ItHasCharacterSelecterd(clientID))
            {
                return true;
            }
            return false;
        }
    }

    private bool ItHasCharacterSelecterd(ulong clientID)
    {
        foreach (var data in m_characterDatas)
        {
            if(data.clientId == clientID)
                return true;    
        }
        return false;
    }


}
