using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class StartManager : NetworkBehaviour
{
    public AudioClip startSFX;
    public GameObject logo;
    public GameObject clickToStart;
    public GameObject mainMenu;

    [SerializeField] private CharacterDataSO[] characterDataSOs;
    private bool gameStarted;
    
          

    private IEnumerator Start()
    {
        AudioManager.Instance.SwitchMusic(AudioManager.Instance.introMusic);

        foreach (var item in characterDataSOs)
        {
            item.EmptyData();
        }

        yield return new WaitUntil(() => NetworkManager.Singleton.SceneManager != null);

        LoadingSceneManager.Instance.Init();

     }



    void Update()
    {
        if (Input.anyKey && !gameStarted)
        {
            logo.SetActive(false);
            clickToStart.SetActive(false);  
            mainMenu.SetActive(true);
            AudioManager.Instance.PlaySoundEffect(startSFX);
            gameStarted = true;
        }
    }
    
    public void OnClickHost()
    {
        NetworkManager.Singleton.StartHost();
        AudioManager.Instance.PlaySoundEffect(startSFX);
        LoadingSceneManager.Instance.LoadScene(SceneName.CharacterSelection);
    }

    public void OnClickJoin()
    {
        AudioManager.Instance.PlaySoundEffect(startSFX);
        StartCoroutine(Join());
    }

    public void OnClickQuit()
    {
        AudioManager.Instance.PlaySoundEffect(startSFX);
        Application.Quit();
    }

    private IEnumerator Join()
    {
        LoadingFadeEffect.Instance.FadeAll();
        yield return new WaitUntil(() => LoadingFadeEffect.s_canLoad);
        NetworkManager.Singleton.StartClient();

    }



}
