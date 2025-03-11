using System;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using System.Collections.Generic;

/*
* Singleton to control the changes on the char sprites and the flow of the scene
*/

public enum ConnectionState : byte
{
    connected,
    disconnected,
    ready
}

// Struct for better serialization on the player connection
[Serializable]
public struct PlayerConnectionState
{
    public ConnectionState playerState;                  // State of the player
    public PlayerCharSelection playerObject;        // The Object of player selection
    public string playerName;                       // The name of the player when spawn
    public ulong playerId;                          //Id of the clien
}

// Struct for better serialization on the container of the character
[Serializable]
public struct CharacterContainer
{
    public Image imageContainer;                    // The image of the character container
    public TextMeshProUGUI nameContainer;           // Character name container
    public GameObject border;                       // The border of the character container when not ready
    public GameObject borderReady;                  // The border of the character container when ready
    public GameObject borderClient;                 // Client border of the character container
    public Image playerIcon;                        // The background icon of the player (p1, p2)
    public GameObject waitingText;                  // The waiting text on the container were no client connected
    public GameObject backgroundShip;               // The background of the ship when not ready
    public Image backgroundShipImage;               // The image of the ship when not ready
    public GameObject backgroundShipReady;          // The background of the ship when ready
    public Image backgroundShipReadyImage;          // The image of the ship when ready
    public GameObject backgroundClientShipReady;    // Client background of the ship when ready
    public Image backgroundClientShipReadyImage;    // Client image of the ship when ready
}

public class CharacterSelectionManager : SingletonNetwork<CharacterSelectionManager>
{
    public CharacterDataSO[] charactersData;

    [SerializeField]
    CharacterContainer[] m_charactersContainers;

    [SerializeField]
    GameObject m_readyButton;

    [SerializeField]
    GameObject m_cancelButton;

    [SerializeField]
    float m_timeToStartGame;

    [SerializeField]
    SceneName m_nextScene = SceneName.Gameplay;

    [SerializeField]
    Color m_clientColor;

    [SerializeField]
    Color m_playerColor;

    [SerializeField]
    PlayerConnectionState[] m_playerStates;

    [SerializeField]
    GameObject m_playerPrefab;

    [Header("Audio clips")]
    [SerializeField]
    AudioClip m_confirmClip;

    [SerializeField]
    AudioClip m_cancelClip;

    bool m_isTimerOn;
    float m_timer;

    private readonly Color k_selectedColor = new Color32(74, 74, 74, 255);
    private List<ulong> m_connectedClients = new List<ulong>();


    void Start()
    {
        m_timer = m_timeToStartGame;
    }



    void Update()
    {
        if (!IsServer) return;

        if (m_isTimerOn)
        {
            m_timer -= Time.deltaTime;

            if (m_timer < 0f)
            {
                m_isTimerOn = false;
                StartGame();
            }
        }
    }


    void StartGame()
    {
        StartGameClientRpc();
        LoadingSceneManager.Instance.LoadScene(m_nextScene);
    }

    [ClientRpc]
    void StartGameClientRpc()
    {
        LoadingFadeEffect.Instance.FadeAll();

    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            NetworkManager.Singleton.OnClientDisconnectCallback += PlayerDisconnects;
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
            NetworkManager.Singleton.OnClientDisconnectCallback -= PlayerDisconnects;
    }
  

    void StartGameTimer()
    {

        foreach (PlayerConnectionState state in m_playerStates)
        {
            // we check if there is any player is not ready (means he is only connected)
            if (state.playerState == ConnectionState.connected)
                return;
        }
        m_timer = m_timeToStartGame;
        m_isTimerOn = true;
    }

    void RemoveSelectedStates()
    {
        for (int i = 0; i < charactersData.Length; i++)
        {
            charactersData[i].isSelected = false;
        }
    }

    void RemoveReadyStates(ulong clientId, bool disconnected)
    {
        for (int i = 0; i < m_playerStates.Length; i++)
        {
            if (m_playerStates[i].playerState == ConnectionState.ready && m_playerStates[i].playerId == clientId)
            {
                if (disconnected)
                {
                    m_playerStates[i].playerState = ConnectionState.disconnected;
                    UpdatePlayersStateClientRpc(clientId, i, ConnectionState.disconnected);
                }
                else
                {
                    m_playerStates[i].playerState = ConnectionState.connected;
                    UpdatePlayersStateClientRpc(clientId, i, ConnectionState.connected);
                }
            }
        }
    }

    [ClientRpc]
    void UpdatePlayersStateClientRpc(ulong clientId, int stateIndex, ConnectionState state)
    {
        if (IsServer) return;
        m_playerStates[stateIndex].playerState = state;
        m_playerStates[stateIndex].playerId = clientId;
    }




    public bool IsReady(int playerId)
    {
        return charactersData[playerId].isSelected;
    }

    public void SetCharacterColor(int playerId, int characterSelected)
    {
        if (charactersData[characterSelected].isSelected)
        {
            m_charactersContainers[playerId].imageContainer.color = k_selectedColor;
            m_charactersContainers[playerId].nameContainer.color = k_selectedColor;
        }
        else
        {
            m_charactersContainers[playerId].imageContainer.color = Color.white;
            m_charactersContainers[playerId].nameContainer.color = Color.white;
        }
    }

    public void SetCharacterUI(int playerId, int characterSelected)
    {
        m_charactersContainers[playerId].imageContainer.sprite =
            charactersData[characterSelected].characterSprite;

        m_charactersContainers[playerId].backgroundShipImage.sprite =
            charactersData[characterSelected].characterShipSprite;

        m_charactersContainers[playerId].backgroundShipReadyImage.sprite =
            charactersData[characterSelected].characterShipSprite;

        m_charactersContainers[playerId].backgroundClientShipReadyImage.sprite =
            charactersData[characterSelected].characterShipSprite;

        m_charactersContainers[playerId].nameContainer.text =
            charactersData[characterSelected].characterName;

        SetCharacterColor(playerId, characterSelected);
    }

    public void SetPlayebleChar(int playerId, int characterSelected, bool isClientOwner)
    {
        SetCharacterUI(playerId, characterSelected);
        m_charactersContainers[playerId].playerIcon.gameObject.SetActive(true);
        if(isClientOwner)
        {
            m_charactersContainers[playerId].borderClient.SetActive(true);
            m_charactersContainers[playerId].border.SetActive(false);
            m_charactersContainers[playerId].borderReady.SetActive(false);
            m_charactersContainers[playerId].playerIcon.color = m_clientColor;
        }
        else
        {
            m_charactersContainers[playerId].border.SetActive(true);
            m_charactersContainers[playerId].borderReady.SetActive(false);
            m_charactersContainers[playerId].borderClient.SetActive(false);
            m_charactersContainers[playerId].playerIcon.color = m_playerColor;

        }




        m_charactersContainers[playerId].backgroundShip.SetActive(true);
        m_charactersContainers[playerId].waitingText.SetActive(false);
    }

    void SetNonPlayableChar(int playerId)
    {
        m_charactersContainers[playerId].imageContainer.sprite = null;
        m_charactersContainers[playerId].imageContainer.color = new Color(1f, 1f, 1f, 0f);
        m_charactersContainers[playerId].nameContainer.text = "";
        m_charactersContainers[playerId].border.SetActive(true);
        m_charactersContainers[playerId].borderClient.SetActive(false);
        m_charactersContainers[playerId].borderReady.SetActive(false);
        m_charactersContainers[playerId].playerIcon.gameObject.SetActive(false);
        m_charactersContainers[playerId].playerIcon.color = m_playerColor;
        m_charactersContainers[playerId].backgroundShip.SetActive(false);
        m_charactersContainers[playerId].backgroundShipReady.SetActive(false);
        m_charactersContainers[playerId].backgroundClientShipReady.SetActive(false);
        m_charactersContainers[playerId].waitingText.SetActive(true);
    }



    public ConnectionState GetConnectionState(int playerID)
    {
        if(playerID != -1)
            return m_playerStates[playerID].playerState;
        return ConnectionState.disconnected;
    }

    public void ServerSceneInit(ulong clientID)
    {

        GameObject go =
            Instantiate(
                m_playerPrefab,
                transform.position,
                quaternion.identity);
        go.GetComponent<NetworkObject>().SpawnWithOwnership(clientID,true);
        for (int i = 0; i < m_playerStates.Length; i++)
        {
            if (m_playerStates[i].playerState == ConnectionState.disconnected)
            {
                m_playerStates[i].playerState = ConnectionState.connected;
                m_playerStates[i].playerObject = go.GetComponent<PlayerCharSelection>();
                m_playerStates[i].playerName = go.name;
                m_playerStates[i].playerId = clientID;

                break;
            }
        }

        //Sync states to clients
        for (int i = 0; i < m_playerStates.Length; i++)
        {
            if(m_playerStates[i].playerObject != null)
            {
                PlayerConnectsClientRpc(
                    m_playerStates[i].playerId,
                    i,
                    m_playerStates[i].playerState,
                    m_playerStates[i].playerObject.GetComponent<NetworkObject>());
            }
        }


    }

    [ClientRpc]
    void PlayerConnectsClientRpc(ulong clientID, int stateIndex, ConnectionState state, NetworkObjectReference player)
    {
        if(!IsServer) return;
         if (state != ConnectionState.disconnected)
        {
            m_playerStates[stateIndex].playerState = state;
            m_playerStates[stateIndex].playerId = clientID;

            if(player.TryGet(out NetworkObject playerObject))
            {
                m_playerStates[stateIndex].playerObject = playerObject.GetComponent<PlayerCharSelection>();
            }
        }  
    }



    public void PlayerNotReady(int characterSelected = 0, bool isDisconected = false)
    {
        m_isTimerOn = false;
        m_timer = m_timeToStartGame;


    }



    // Set the player ready if the player is not selected and check if all player are ready to start the countdown
    public void PlayerReady( ulong clientId, int playerId, int characterSelected)
    {
        if (!charactersData[characterSelected].isSelected)
        {
            PlayerReadyClientRpc(clientId, playerId, characterSelected);
            StartGameTimer();
        }
    }

    // Set the players UI button
    public void SetPlayerReadyUIButtons(bool isReady, int characterSelected)
    {
        if (isReady && !charactersData[characterSelected].isSelected)
        {
            m_readyButton.SetActive(false);
            m_cancelButton.SetActive(true);
        }
        else if (!isReady && charactersData[characterSelected].isSelected)
        {
            m_readyButton.SetActive(true);
            m_cancelButton.SetActive(false);
        }
    }

    // Check if the player has selected the character
    public bool IsSelectedByPlayer(int playerId, int characterSelected)
    {
        return charactersData[characterSelected].playerId == playerId ? true : false;
    }

    [ClientRpc]
    void PlayerReadyClientRpc(ulong clientId, int playerId, int characterSelected)
    {
        charactersData[characterSelected].isSelected = true;
        charactersData[characterSelected].playerId = playerId;
        charactersData[characterSelected].clientId = clientId;
        m_playerStates[playerId].playerState = ConnectionState.ready;

        if(clientId == NetworkManager.Singleton.LocalClientId)
        {
            m_charactersContainers[playerId].backgroundClientShipReady.SetActive(true);
            m_charactersContainers[playerId].backgroundShip.SetActive(false);
        }
        else
        {
            m_charactersContainers[playerId].border.SetActive(false);
            m_charactersContainers[playerId].borderReady.SetActive(true);
            m_charactersContainers[playerId].backgroundShip.SetActive(false) ;
            m_charactersContainers[playerId].backgroundShipReady.SetActive(true) ;
        }

        for (int i = 0; i < m_playerStates.Length; i++)
        {
            //only changes the ones on clients that are not selected
            if (m_playerStates[i].playerState == ConnectionState.connected) 
            {
                if (m_playerStates[i].playerObject.CharSelected == characterSelected)
                {
                    SetCharacterColor(i,characterSelected); 
                }
            }
        }

        AudioManager.Instance.PlaySoundEffect(m_confirmClip);
    }


    [ClientRpc]
    void PlayerNotReadyClientRpc(ulong clientId, int playerId, int characterSelected)
    {
        charactersData[characterSelected].isSelected = false;
        charactersData[characterSelected].playerId = -1;
        charactersData[characterSelected].clientId = 0;

        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            m_charactersContainers[playerId].borderClient.SetActive(true);
            m_charactersContainers[playerId].backgroundClientShipReady.SetActive(false);
            m_charactersContainers[playerId].backgroundShip.SetActive(true);
        }
        else
        {
            m_charactersContainers[playerId].border.SetActive(true);
            m_charactersContainers[playerId].borderReady.SetActive(false);
            m_charactersContainers[playerId].borderClient.SetActive(false) ;
            m_charactersContainers[playerId].backgroundShip.SetActive(true);
            m_charactersContainers[playerId].backgroundShipReady.SetActive(false);
        }
        AudioManager.Instance.PlaySoundEffect(m_cancelClip);

        for (int i = 0; i < m_playerStates.Length; i++)
        {
            //only changes the ones on clients that are not selected
            if (m_playerStates[i].playerState == ConnectionState.connected)
            {
                if (m_playerStates[i].playerObject.CharSelected == characterSelected)
                {
                    SetCharacterColor(i, characterSelected);
                }
            }
        }

    }

    public int GetPlayerId(ulong clientId)
    {
        for (int i = 0; i < m_playerStates.Length; i++)
        {
            if (m_playerStates[i].playerId == clientId)
            {
                return i;
            }
        }
        return -1;
    }

    public void PlayerNotReady(ulong clientId, int characterSelected = 0, bool isDisconected = false)
    {
        int playerId = GetPlayerId(clientId);
        m_isTimerOn = false;
        m_timer = m_timeToStartGame;

        RemoveReadyStates(clientId, isDisconected);

        //notify clients to Change UI
        if(isDisconected) 
        {
            PlayerDisconnectedClientRpc(playerId);
        }
        else
        {
            PlayerNotReadyClientRpc(clientId, playerId, characterSelected);
        }
    }

    [ClientRpc]
    public void PlayerDisconnectedClientRpc(int playerId)
    {
        SetNonPlayableChar(playerId);
        RemoveSelectedStates();
        m_playerStates[playerId].playerState = ConnectionState.disconnected;
    }

    public void PlayerDisconnects(ulong clientId)
    {
        if (!ClientConnectionManager.Instance.IsExtraClient(clientId))
            return;
        PlayerNotReady(clientId, isDisconected: true);
        m_playerStates[GetPlayerId(clientId)].playerObject.Despawn();

        //the client disconnected is the host

        if(clientId == 0)
        {
            NetworkManager.Singleton.Shutdown();
        }
    }
}
