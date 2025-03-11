using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbyItemUI : MonoBehaviour
{

    [SerializeField] private TextMeshProUGUI lobbyNameTxt;
    [SerializeField] private TextMeshProUGUI playersNumberTxt;

    private Lobby lobby;


    private void Awake()
    {
        Debug.Log("Listner Added");

        GetComponent<Button>().onClick.AddListener(() =>
        {
            LobbyManager.Instance.JoinLobby(lobby);
        });
    }

    public void UpdateLobby(Lobby lobby)
    {
        this.lobby = lobby;

        lobbyNameTxt.text = lobby.Name;
        playersNumberTxt.text = lobby.Players.Count + "/" + lobby.MaxPlayers;

    }



}
