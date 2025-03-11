using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode.Transports.UTP;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEngine;

public class LobbyManager : SingletonNetwork<LobbyManager>
{
    public TMP_InputField nameField;
    private Lobby hostLobby;
    private Lobby joinedLobby;
    private string playerName;
    private bool gameIsStarted = false;


    private float lobbyUpdateTimer;

    
    private async void Start()
    {
        await UnityServices.InitializeAsync();

        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log("Signed in " + AuthenticationService.Instance.PlayerId);
        };

       if(!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

        playerName = "Villian " + UnityEngine.Random.Range(0, 10);

        nameField.text = playerName;
        Debug.Log(playerName);
    }
    

    private void Update()
    {
        if (gameIsStarted)
            return;
        HandleLobbyUpdates();
    }


    public async void CreateLobby()
    {
        try
        {
            //relay creation and obtaining the join code
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(4);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            string lobbyName = playerName + " Lobby";
            int maxPlayers = 4;
            CreateLobbyOptions options = new CreateLobbyOptions()
            {
                Player = GetPlayer(),
                Data = new Dictionary<string, DataObject>()
                {
                    {"JoinCode", new DataObject(DataObject.VisibilityOptions.Public, joinCode)}
                }
            };


            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);
            hostLobby = lobby;
            joinedLobby = hostLobby;
            Debug.Log("Lobby created successfully : " + lobby.Name + " " + lobby.MaxPlayers);
            LobbyUI.Instance.UpdateLobby(joinedLobby);
            LobbiesListUI.Instance.gameObject.SetActive(false);
            StartCoroutine(HeartBeat(lobby));
            //StartCoroutine(HandleLobbyUpdates());


            NetworkManager.Singleton.GetComponent<UnityTransport>().SetHostRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
                );
            NetworkManager.Singleton.StartHost();

            PrintPlayers();

        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }

    }


    private async void HandleLobbyUpdates()
    {
        if (joinedLobby != null)
        {
            lobbyUpdateTimer -= Time.deltaTime;
            if (lobbyUpdateTimer < 0)
            {
                float lobbyUpdateTimerMax = 1.1f;
                lobbyUpdateTimer = lobbyUpdateTimerMax;

                Lobby lobby = await LobbyService.Instance.GetLobbyAsync(joinedLobby.Id);
                joinedLobby = lobby;
                if (gameIsStarted)
                    return;
                LobbyUI.Instance.UpdateLobby(joinedLobby);

            }
        }
    }



    IEnumerator HeartBeat(Lobby hostLobby)
    {
        yield return new WaitForSeconds(20);
        LobbyService.Instance.SendHeartbeatPingAsync(hostLobby.Id);
        ListLobbies();
        StartCoroutine(HeartBeat(hostLobby));
    }




    public async void ListLobbies()
    {
        try
        {
            QueryLobbiesOptions queryOptions = new QueryLobbiesOptions
            {
                Count = 10,
                Filters = new List<QueryFilter>
                {
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
                },
                Order = new List<QueryOrder>
                {
                    new QueryOrder(false, QueryOrder.FieldOptions.Created)
                }
            };
            QueryResponse queryResponse = await Lobbies.Instance.QueryLobbiesAsync();

            print(queryResponse.Results.Count);
            LobbiesListUI.Instance.UpdateLobbiesList(queryResponse.Results);


            Debug.Log("Lobbies found: " + queryResponse.Results.Count);
            foreach (var result in queryResponse.Results)
            {
                {
                    Debug.Log(result.Name + " " + result.MaxPlayers);
                }
            }
        } catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }


    }


    public async void JoinLobby(Lobby lobby)
    {
        try
        {
            Player player = GetPlayer();


            joinedLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobby.Id, new JoinLobbyByIdOptions
            {
                Player = player
            });
            print("we joined lobby");
            LobbiesListUI.Instance.gameObject.SetActive(false);
            LobbyUI.Instance.gameObject.SetActive(true);
            LobbyUI.Instance.UpdateLobby(joinedLobby);

            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinedLobby.Data["JoinCode"].Value);


            NetworkManager.Singleton.GetComponent<UnityTransport>().SetClientRelayData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData
                );
            NetworkManager.Singleton.StartClient();

        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    public async void QuickJoinLobby()
    {
        try
        {
            QuickJoinLobbyOptions queryOptions = new QuickJoinLobbyOptions()
            {
                Player = GetPlayer()
            };
            Lobby lobby = await LobbyService.Instance.QuickJoinLobbyAsync(queryOptions);
            joinedLobby = lobby;
            Debug.Log(" We have quickly joined the lobby ");
            PrintPlayers();
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }


    public void PrintPlayers()
    {
        Debug.Log("Here is the list of players inside the lobby: " + joinedLobby.Name);
        foreach (Player player in joinedLobby.Players)
        {
            Debug.Log(player.Data["PlayerName"].Value);
        }
    }

    private Player GetPlayer()
    {


        return new Player
        {
            Data = new Dictionary<string, PlayerDataObject>
                    {
                        {"PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerName) }
                    }
        };
    }

    public async void UpdatePlayerName()
    {
        try
        {

            if (joinedLobby == null) {
                playerName = nameField.text;
                return;
            }

            playerName = nameField.text;
            await LobbyService.Instance.UpdatePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId, new UpdatePlayerOptions()
            {
                Data = new Dictionary<string, PlayerDataObject>
                        {
                            {"PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerName) }
                        }
            });

            PrintPlayers();
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    public async void LeaveLobby()
    {
        if (joinedLobby != null)
        {
            try
            {
                await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId);
                joinedLobby = null;
                LobbyUI.Instance.gameObject.SetActive(false);
                LobbiesListUI.Instance.gameObject.SetActive(true);

            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }
        }
    }


    public void PlayGame()
    {
        gameIsStarted = true;
        if (!IsServer) return;
        StartGameClientRpc();
        LoadingSceneManager.Instance.LoadScene(SceneName.CharacterSelection);
    }

    [ClientRpc]
    void StartGameClientRpc()
    {
        LoadingFadeEffect.Instance.FadeAll();

    }

}
