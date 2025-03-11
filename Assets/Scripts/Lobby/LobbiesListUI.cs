using System.Collections;
using System.Collections.Generic;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbiesListUI : Singleton<LobbiesListUI> 
{
    [SerializeField] private Transform lobbyItemTemplate;
    [SerializeField] private Transform container;
    [SerializeField] private Button createLobbyBtn;
    [SerializeField] private Button refreshLobbiesListBtn;

    public override void Awake()
    {
        lobbyItemTemplate.gameObject.SetActive(false);
        base.Awake();
    }

    public void UpdateLobbiesList(List<Lobby> lobbiesList)
    {
        // Clear the old lobbies list while keeping the template
         foreach (Transform child in container)
          {
              if (child == lobbyItemTemplate) continue;
              Destroy(child.gameObject);
          }

        // list new lobbies list 
         foreach (Lobby  lobby in lobbiesList)
         {
             Transform lobbyItemTransform = Instantiate(lobbyItemTemplate, container);
             lobbyItemTransform.gameObject.SetActive(true);
             LobbyItemUI lobbyItemUI = lobbyItemTransform.GetComponent<LobbyItemUI>();
             lobbyItemUI.UpdateLobby(lobby);
         }
        print(lobbiesList);
    }

}
