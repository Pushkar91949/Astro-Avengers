using UnityEngine;
using Unity.Mathematics;
using Unity.Netcode;

public class PlayerShipShootBullet : NetworkBehaviour
{
    [SerializeField]
    int m_fireDamage;

    [SerializeField]
    GameObject m_bulletPrefab;

    [SerializeField]
    Transform m_cannonPosition;

    [SerializeField]
    CharacterDataSO m_characterData;

    [SerializeField]
    GameObject m_shootVfx;

    [SerializeField]
    AudioClip m_shootClip;

    void Update()
    {
        if(!IsOwner) { return; }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            FireNewBulletServerRpc();
        }
    }

    [ServerRpc]
    void FireNewBulletServerRpc()
    {
        SpawnNewBulletVfx();
        GameObject newBullet = GetNewBullet();
        PrepareNewlySpawnedBulltet(newBullet);
        PlayShootBulletSound();
    }

    private void SpawnNewBulletVfx()
    {
        if (m_shootVfx != null)
        {
            GameObject go = Instantiate(m_shootVfx, m_cannonPosition.position, quaternion.identity);
            go.GetComponent<NetworkObject>().Spawn(true);

        }

    }

    private GameObject GetNewBullet()
    {
       GameObject go = Instantiate(
            m_bulletPrefab,
            m_cannonPosition.position,
            quaternion.identity
            );
        go.GetComponent<NetworkObject>().Spawn(true);

        return go;
    }

    private void PrepareNewlySpawnedBulltet(GameObject newBullet)
    {
        BulletController bulletController = newBullet.GetComponent<BulletController>();
        bulletController.damage = m_fireDamage;
        bulletController.characterData = m_characterData;
    }

    void PlayShootBulletSound()
    {
        if (m_shootClip != null)
            AudioManager.Instance?.PlaySoundEffect(m_shootClip);
    }
}
