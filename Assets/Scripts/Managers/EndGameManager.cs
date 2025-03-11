using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SocialPlatforms.Impl;

public class EndGameManager : SingletonNetwork<EndGameManager>
{
    enum EndGameStatus
    {
        victory,
        defeat,
    };

    [SerializeField]
    private EndGameStatus m_status;                     // Set the scene status to now if we are on victory o defeat scene

    [SerializeField]
    private CharacterDataSO[] m_charactersData;         // The characters data use to take some data from there

    [SerializeField]
    private Transform[] m_shipsPositions;               // The final positions of the ships 

    [SerializeField]
    private AudioClip m_endGameClip;                    // The audio clip to reproduce when the scene start

    private int m_shipPositionindex;                    // Var to move every player to different position

    private PlayerShipScore m_bestPlayer;               // Catch who is the best player -> only on server

    private List<ulong> m_connectedClients = new List<ulong>();

    private void Start()
    {
        AudioManager.Instance.StopMusic();
        AudioManager.Instance.PlaySoundEffect(m_endGameClip, 1f);

    }

    public void ServerSceneInit(ulong clientId)
    {
        //save the clients
        m_connectedClients.Add(clientId);

        //Check if tge last client
        if (m_connectedClients.Count < NetworkManager.Singleton.ConnectedClients.Count)
            return;

        // We do this only one time when all clients are connected so they sync correctly
        // Tell all clients instance to set the UI base on the server characters data
        int bestScore = -1;
        for (int i = 0; i < m_charactersData.Length; i++)
        {
            if (m_charactersData[i].isSelected)
            {
                GameObject playerScoreResult = Instantiate(
                    m_charactersData[i].spaceshipScorePrefab,
                    m_shipsPositions[m_shipPositionindex].position,
                    Quaternion.identity);
                playerScoreResult.GetComponent<NetworkObject>().Spawn(true);

                // Check who has the best score
                // The score is calculated base on the enemies destroyed minus the power-ups the player used
                // Feel free to modify these values
                int enemyDestroyedScore = (m_charactersData[i].enemiesDestroyed * 100);
                int powerUpsUsedScore = (m_charactersData[i].powerUpsUsed * 50);
                int currentFinalScore = enemyDestroyedScore - powerUpsUsedScore;

                var playerShipScore = playerScoreResult.GetComponent<PlayerShipScore>();

                if (currentFinalScore > bestScore)
                {
                    m_bestPlayer = playerShipScore;
                    bestScore = currentFinalScore;
                }
                // Victory or defeat so turn on the appropriate vfx
                bool isVictorious = m_status == EndGameStatus.victory;
                playerShipScore.SetShip(
                    isVictorious,
                    m_charactersData[i].enemiesDestroyed,
                    m_charactersData[i].powerUpsUsed,
                    currentFinalScore);
                //set the values of the score on every instance
                SetShipDataClientRpc(
                    m_charactersData[i].enemiesDestroyed,
                    m_charactersData[i].powerUpsUsed,
                    currentFinalScore,
                    playerScoreResult.name);

                m_shipPositionindex++;
            }
        }


    }

    // When the button is pressed, start the shutdown process
    public void GoToMenu()
    {
        if (IsServer)
            StartCoroutine(HostShutdown());
        else
        {
            Shutdown();
        }

        LoadingSceneManager.Instance.LoadScene(SceneName.Menu);
    }

    private IEnumerator HostShutdown()
    {
        // Tell the clients to shutdown
        ShutdownClientRpc();

        // Wait some time for the message to get to clients
        yield return new WaitForSeconds(0.5f);

        // Shutdown server/host
        Shutdown();
    }

    private void Shutdown()
    {
        NetworkManager.Singleton.Shutdown();
        LoadingSceneManager.Instance.LoadScene(SceneName.Menu, false);
    }

    [ClientRpc]
    private void ShutdownClientRpc()
    {
        if (IsServer)
            return;

        Shutdown();
    }


    [ClientRpc]
    private void SetShipDataClientRpc(
        int enemiesDestroyed,
        int powerUpsUsed,
        int score,
        string spaceShipScoreName)
    {

        GameObject spaceShipScore = GameObject.Find(spaceShipScoreName);

        bool isVictorious = m_status == EndGameStatus.victory;
        spaceShipScore.GetComponent<PlayerShipScore>().SetShip(
            isVictorious,
            enemiesDestroyed,
            powerUpsUsed,
            score);
    }


}