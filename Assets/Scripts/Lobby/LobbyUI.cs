using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;


public class LobbyUI : Singleton<LobbyUI>
{
    [SerializeField] private Transform playerItemTemplate;
    [SerializeField] private Transform container;
    [SerializeField] private Button leaveBtn;
    [SerializeField] private TextMeshProUGUI lobbyNameTxt;
    [SerializeField] private TextMeshProUGUI playersNumberTxt;

    public override void Awake()
    {
        playerItemTemplate.gameObject.SetActive(false);
        base.Awake();
    }

    private void Start()
    {
        gameObject.SetActive(false);
    }


    public void UpdateLobby(Lobby lobby)
    {
        // Clear the lobby from player
        foreach (Transform child in container)
        {
            if (child == playerItemTemplate) continue;
            Destroy(child.gameObject);
        }

        // list new players
        foreach (Player player in lobby.Players)
        {
            Transform playerItem = Instantiate(playerItemTemplate, container);
            playerItem.gameObject.SetActive(true);
            PlayerItemUI playerItemUI = playerItem.GetComponent<PlayerItemUI>();
            playerItemUI.UpldatePlayer(player);
        }

        lobbyNameTxt.text = "Lobby: "+lobby.Name;
        playersNumberTxt.text = lobby.Players.Count + "/" + lobby.MaxPlayers;

        gameObject.SetActive(true);

    }
}
