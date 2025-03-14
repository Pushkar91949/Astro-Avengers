using UnityEngine;
using Unity.Netcode;

public class SmallBullet : NetworkBehaviour
{
    private int m_damage = 1;

    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (IsServer && collider.TryGetComponent(out IDamagable damagable))
        {
            damagable.Hit(m_damage);

            GetComponent<NetworkObject>().Despawn();
        }
    }
}
