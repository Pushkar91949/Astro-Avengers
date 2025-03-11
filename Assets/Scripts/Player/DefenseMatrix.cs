using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class DefenseMatrix : NetworkBehaviour, IDamagable
{
    public bool isShieldActive { get; private set; } = false;
    public GameObject shield;
    private CircleCollider2D m_circleCollider2D;

    private void Start()
    {
        shield.SetActive(true);
        m_circleCollider2D = gameObject.GetComponentInChildren<CircleCollider2D>();
        shield.SetActive(false);
    }

    public void Hit(int damage)
    {
        print("we got a hit on the shield");
        TurnOffShieldClientRpc();
    }

    public void TurnOnShield()
    {
        isShieldActive = true;

        shield.SetActive(true);
        m_circleCollider2D.enabled = true;
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (!IsServer)
            return;

        if (collider.TryGetComponent(out IDamagable damagable))
        {
            damagable.Hit(1);
            TurnOffShieldClientRpc();
        }
    }

    [ClientRpc]
    void TurnOffShieldClientRpc()
    {
        print("we disalbing the shield");

        isShieldActive = false;

        shield.SetActive(false);
        m_circleCollider2D.enabled = false;

    }

    IEnumerator IDamagable.HitEffect()
    {
        return null;
    }
}