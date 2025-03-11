using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class PlayerItemUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI PlayerNameTxt;


    private Player player;


    public void UpldatePlayer(Player player)
    {
        this.player = player;
        PlayerNameTxt.text = player.Data["PlayerName"].Value;

    }

}
