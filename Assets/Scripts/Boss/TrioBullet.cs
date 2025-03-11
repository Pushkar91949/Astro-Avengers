using UnityEngine;
using Unity.Netcode;

public class TrioBullet : NetworkBehaviour
{
    [SerializeField]
    private GameObject _smallBulletPrefab;

    [SerializeField]
    private Transform[] _firePositions;

    private void SpawnBullets()
    {
        // Spawn the bullets
        foreach (Transform firePosition in _firePositions)
        {
            GameObject newBullet = Instantiate(
                _smallBulletPrefab,
                firePosition.position,
                firePosition.rotation);
            newBullet.GetComponent<NetworkObject>().Spawn(true);
        }

        GetComponent<NetworkObject>().Despawn();
    }


    public override void OnNetworkSpawn()
    {
        if( IsServer )
        {
            SpawnBullets();
        }
        base.OnNetworkSpawn();

    }
}
