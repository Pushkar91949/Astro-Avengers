using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

// Important: the names in the enum value should be the same as the scene you're trying to load
public enum SceneName : byte
{
    Bootstrap,
    Menu,
    CharacterSelection,
    Controls,
    Gameplay,
    Victory,
    Defeat,

};

public class LoadingSceneManager : SingletonPersistent<LoadingSceneManager>
{
    public SceneName SceneActive => m_sceneActive;

    private SceneName m_sceneActive;

    //we subscribe to these events due to the fact when netwrok session ends it can't listen to them.

    public void Init()
    {
        //NetworkManager.Singleton.SceneManager.OnLoadComplete -= OnLoadComplete;
        NetworkManager.Singleton.SceneManager.OnLoadComplete += OnLoadComplete;
    }

    private void OnLoadComplete(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
    {
        if (!NetworkManager.Singleton.IsServer) return;

        Enum.TryParse(sceneName, out m_sceneActive);

        if (!ClientConnectionManager.Instance.CanClientConnect(clientId)) return;


        switch (m_sceneActive)
        {
            case SceneName.CharacterSelection:
                CharacterSelectionManager.Instance.ServerSceneInit(clientId);
                break;
            case SceneName.Gameplay:
                GameplayManager.Instance.ServerSceneInit(clientId);
                break;
            case SceneName.Victory:
            case SceneName.Defeat:
                EndGameManager.Instance.ServerSceneInit(clientId);
                break;
        }
    }

    public void LoadScene(SceneName sceneToLoad, bool isNetworkSessionActive = true)
    {
        StartCoroutine(Loading(sceneToLoad, isNetworkSessionActive));

    }

    // Coroutine for the loading effect. It use an alpha in out effect
    private IEnumerator Loading(SceneName sceneToLoad, bool isNetworkSessionActive)
    {
        LoadingFadeEffect.Instance.FadeIn();

        // Here the player still sees the black screen
        yield return new WaitUntil(() => LoadingFadeEffect.s_canLoad);
        if(isNetworkSessionActive) 
        {
            if (NetworkManager.Singleton.IsServer)
                LoadSceneNetwork(sceneToLoad);
        }
        else
        {
            LoadSceneLocal(sceneToLoad);
        }


        yield return new WaitForSeconds(1f);

        LoadingFadeEffect.Instance.FadeOut();
    }

    private void LoadSceneNetwork (SceneName sceneToLoad)
    {
        NetworkManager.Singleton.SceneManager.LoadScene(sceneToLoad.ToString(), LoadSceneMode.Single);
    }



    private void LoadSceneLocal(SceneName sceneToLoad)
    {
        SceneManager.LoadScene(sceneToLoad.ToString());

    }



}