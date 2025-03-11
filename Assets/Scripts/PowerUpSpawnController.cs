using UnityEngine;
using Unity.Netcode;

public class PowerUpSpawnController : SingletonNetwork<PowerUpSpawnController> 
{

    public GameObject[] listOfPowerUps;

    [Tooltip("The probability (in %) that a power up gets spawned")]
    [Range(0, 100)]
    public int probabilityOfPowerUpSpawn;



    public void OnPowerUpSpawn(Vector3 positionToSpawn)
    {
        if (listOfPowerUps == null || listOfPowerUps.Length == 0)
            return;

        int randomPick = Random.Range(1, 100);
        if (randomPick <= probabilityOfPowerUpSpawn)
        {
            var nextPowerUpToSpawn = GetRandomPowerUp();

            var newSpawnedPowerup = Instantiate(nextPowerUpToSpawn);

            newSpawnedPowerup.transform.position = positionToSpawn;
            newSpawnedPowerup.GetComponent<NetworkObject>().Spawn(true);
        }
    }

    private GameObject GetRandomPowerUp()
    {
        int randomPick = Random.Range(0, listOfPowerUps.Length - 1);

        return listOfPowerUps[randomPick];
    }

}