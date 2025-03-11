using System.Collections;
using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Lobbies.Models;

public class RelayTest : MonoBehaviour
{
    public TMP_InputField inputField;
    
    
    private async void Start()
    {
        await UnityServices.InitializeAsync();

        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log("Sign in: " + AuthenticationService.Instance.PlayerId);
        };
        await AuthenticationService.Instance.SignInAnonymouslyAsync();

    }

    public async void CreateRelay()
    {
        try
        {
            Allocation allocation =  await RelayService.Instance.CreateAllocationAsync(4);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Debug.Log("Join code is: " + joinCode);

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetHostRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
                );

            NetworkManager.Singleton.StartHost();
            LoadingSceneManager.Instance.LoadScene(SceneName.CharacterSelection);
        }
        catch (RelayServiceException e)
        {
            Debug.Log(e);
        }

    }

    public async void JoinRelay()
    {
        
        try
        {
            string joinCode = inputField.text;

            Debug.Log("Joining the relayt with code: " + joinCode);
            JoinAllocation joinAllocation =  await RelayService.Instance.JoinAllocationAsync(joinCode);

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetClientRelayData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData
                );

            StartCoroutine(Join());
        }
        catch (RelayServiceException e)
        {
            Debug.Log(e);
        }
    }

    private IEnumerator Join()
    {
        LoadingFadeEffect.Instance.FadeAll();
        yield return new WaitUntil(() => LoadingFadeEffect.s_canLoad);
        NetworkManager.Singleton.StartClient();

    }

}
