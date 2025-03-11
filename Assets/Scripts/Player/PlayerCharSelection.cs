using System.Collections;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerCharSelection : NetworkBehaviour
{
    
    public int CharSelected => m_charSelected.Value;

    private const int  k_noCharacterSelectedValue = -1;

    [SerializeField]
    private NetworkVariable<int> m_charSelected = new NetworkVariable<int>(k_noCharacterSelectedValue);

    [SerializeField]
    private NetworkVariable<int> m_playerId = new NetworkVariable<int>(k_noCharacterSelectedValue);


    [SerializeField]
    private AudioClip _changedCharacterClip;



    private IEnumerator Start()
    {
        if(IsClient)
        {
            yield return new WaitForSeconds(0.5f);
            m_playerId.OnValueChanged += OnPlayerIdSet;
            m_charSelected.OnValueChanged += OnCharacterChanged;
            OnButtonPress.a_OnButtonPress += OnUIButtonPress;
        }

        if (IsServer)
        {
            m_playerId.Value = CharacterSelectionManager.Instance.GetPlayerId(OwnerClientId);
        }
        else if (!IsOwner && HasACharacterSelected())
        {
            CharacterSelectionManager.Instance.SetPlayebleChar(
                m_playerId.Value,
                m_charSelected.Value,
                IsOwner
                );
        }

        //assigne the name of the object based on the player id for every instance
        gameObject.name = "Player" + (m_playerId.Value + 1);
    }


    
    private void OnEnable()
    {
        if (!IsServer) return;
        m_playerId.OnValueChanged += OnPlayerIdSet;
        m_charSelected.OnValueChanged += OnCharacterChanged;
        OnButtonPress.a_OnButtonPress += OnUIButtonPress;
    }



    
    private void OnDisable()
    {
        m_playerId.OnValueChanged -= OnPlayerIdSet;
        m_charSelected.OnValueChanged -= OnCharacterChanged;
        OnButtonPress.a_OnButtonPress -= OnUIButtonPress;
    }

    private void OnPlayerIdSet( int oldValue, int newValue)
    {
        
        CharacterSelectionManager.Instance.SetPlayebleChar(newValue, newValue, IsOwner);
        if (IsServer)
            m_charSelected.Value = newValue;
    }

    private void OnCharacterChanged(int oldValue, int newValue)
    {
        if (!IsOwner && HasACharacterSelected())
            CharacterSelectionManager.Instance.SetCharacterUI(m_playerId.Value, newValue);
    }

    private void OnUIButtonPress(ButtonActions buttonActions)
    {
        if (!IsOwner)
            return;
        switch(buttonActions)
        {
            case ButtonActions.lobby_ready:
                CharacterSelectionManager.Instance.SetPlayerReadyUIButtons(
                    true,
                    m_charSelected.Value);
                ReadyServerRpc();
                break;
            case ButtonActions.lobby_not_ready:
                CharacterSelectionManager.Instance.SetPlayerReadyUIButtons(
                    false,
                    m_charSelected.Value);
                NotReadyServerRpc();
                break;

        }
    }

    [ServerRpc]
    private void NotReadyServerRpc()
    {
        CharacterSelectionManager.Instance.PlayerNotReady(OwnerClientId, m_charSelected.Value);
    }
    [ServerRpc]
    private void ReadyServerRpc()
    {
        CharacterSelectionManager.Instance.PlayerReady(
            OwnerClientId,
            m_playerId.Value,
            m_charSelected.Value
            );
    }


    private bool HasACharacterSelected()
    {
        return m_playerId.Value != k_noCharacterSelectedValue;
    }

    private void Update()
    {
        if (IsOwner && CharacterSelectionManager.Instance.GetConnectionState(m_playerId.Value) != ConnectionState.ready)
        {
            if (Input.GetKeyDown(KeyCode.A))
            {
                ChangeCharacterSelection(-1);
                AudioManager.Instance.PlaySoundEffect(_changedCharacterClip);
            }
            else if (Input.GetKeyDown(KeyCode.D))
            {
                ChangeCharacterSelection(1);
                AudioManager.Instance.PlaySoundEffect(_changedCharacterClip);
            }
        }

        if (IsOwner)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {

                // Check that the character is not selected
                if (!CharacterSelectionManager.Instance.IsReady(m_charSelected.Value))
                {
                    CharacterSelectionManager.Instance.SetPlayerReadyUIButtons(
                        true,
                        m_charSelected.Value);

                    ReadyServerRpc();
                }
                else
                {
                    // if selected check if is selected by me
                    if (CharacterSelectionManager.Instance.IsSelectedByPlayer(
                            m_playerId.Value, m_charSelected.Value))
                    {
                        // If it's selected by me, de-select
                        CharacterSelectionManager.Instance.SetPlayerReadyUIButtons(
                            false,
                            m_charSelected.Value);

                        NotReadyServerRpc();
                    }
                }
            }
            if(Input.GetKeyDown(KeyCode.Escape))
            {
                //exit the network state and return to main menu
                if(m_playerId.Value == 0)// the host
                {
                    // all players should shutdown and exit
                    StartCoroutine(HostShutDown());
                }
                else
                {
                    Shutdown();
                }
            }

        }
    }



    void Shutdown()
    {
        
        CharacterSelectionManager.Instance.PlayerDisconnectedClientRpc((int)OwnerClientId);
        NetworkManager.Singleton.Shutdown();
        LoadingSceneManager.Instance.LoadScene(SceneName.Menu,false);
    }

    [ClientRpc]
    void ShutdownClientRpc()
    {
        if (IsServer) return;
        Shutdown();
    }

    IEnumerator HostShutDown()
    {
        //tell the clients to shutdown
        ShutdownClientRpc();

        //wait a bit till clients shut down
        yield return new WaitForSeconds(0.5f);

        Shutdown();
    }


    private void ChangeCharacterSelection(int value)
    {
        // Assign a temp value to prevent the call of onchange event in the charSelected
        int charTemp = m_charSelected.Value;
        charTemp += value;
        
        if (charTemp >= CharacterSelectionManager.Instance.charactersData.Length)
            charTemp = 0;
        else if (charTemp < 0)
            charTemp = CharacterSelectionManager.Instance.charactersData.Length - 1;

        if(IsOwner) {
            //notify the server of the change
            ChangeCharacterSelectionServerRpc(charTemp);

            //owner doesn't wait for the onValueChange
            CharacterSelectionManager.Instance.SetPlayebleChar(
                    m_playerId.Value,
                    charTemp,
                    IsOwner);
        }


    }

    [ServerRpc]
    private void ChangeCharacterSelectionServerRpc(int value)
    {
        m_charSelected.Value = value;
    }




    public void Despawn()
    {
        GetComponent<NetworkObject>().Despawn();
    }

}