using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerShipController : NetworkBehaviour, IDamagable
{
    public NetworkVariable<int> health = new NetworkVariable<int>(10);


    
    private NetworkVariable<int> m_specials = new NetworkVariable<int>(0);

    [SerializeField]
    int m_maxSpecialPower;

    [SerializeField]
    DefenseMatrix m_defenseShield;

    [SerializeField]
    CharacterDataSO m_characterData;

    [SerializeField]
    GameObject m_explosionVfxPrefab;

    [SerializeField]
    GameObject m_powerupPickupVfxPrefab;

    [SerializeField]
    float m_hitEffectDuration;

    [Header("AudioClips")]
    [SerializeField]
    AudioClip m_hitClip;
    [SerializeField]
    AudioClip m_shieldClip;


    [Header("ShipSprites")]
    [SerializeField]
    SpriteRenderer m_shipRenderer;

    [Header("Runtime set")]
    public PlayerUI playerUI;

    
    public CharacterDataSO characterData;

    public GameplayManager gameplayManager;

    [SerializeField]
    bool m_isPlayerDefeated;

    const string k_hitEffect = "_Hit";

    void Update()
    {
        if (IsOwner)
        {
            if (!m_defenseShield.isShieldActive &&
                (Input.GetKeyDown(KeyCode.K) || Input.GetKeyDown(KeyCode.LeftShift)))
            {
                // Tell the server to activate the shield
                ActivateShieldServerRpc();
            }
        }

    }

    [ServerRpc]
    void ActivateShieldServerRpc()
    {
        // Activate the special in case the ship has available
        if (m_specials.Value > 0)
        {
            // Tell the UI to remove the icon
            playerUI.UpdatePowerUp(m_specials.Value, false);

            // Update the UI on clients, reduce the number of specials available
            m_specials.Value--;

            // Activate the special on clients for sync
            ActivateShieldSClientRpc();

            // Update the power up use for the final score
            characterData.powerUpsUsed++;
        }
    }

    [ClientRpc]
    void ActivateShieldSClientRpc()
    {
        m_defenseShield.TurnOnShield();
        AudioManager.Instance?.PlaySoundEffect(m_shieldClip);
    }



    void PlayShipHitSound()
    {
            AudioManager.Instance?.PlaySoundEffect(m_hitClip);
    }


    void OnTriggerEnter2D(Collider2D collider)
    {
        if (!IsServer) return;
        // If the collider hit a power-up
        
        if (collider.gameObject.CompareTag("PowerUp"))
        {
            // Check if I have space to take the special
            if (m_specials.Value < m_maxSpecialPower)
            {
                // Update var
                m_specials.Value++;

                // Update UI
                playerUI.UpdatePowerUp(m_specials.Value, true);

                // Show Power-up Pickup VFX
                GameObject go = Instantiate(m_powerupPickupVfxPrefab, transform.position, Quaternion.identity);
                go.GetComponent<NetworkObject>().Spawn(true);

                // Remove the power-up
                collider.GetComponent<NetworkObject>().Despawn();
            }
        }
    }


    [ClientRpc]
    void HitClientRpc()
    {
        StopCoroutine(HitEffect());
        StartCoroutine(HitEffect());
    }

    public void Hit(int damage)
    {
        if ((!IsServer) || m_isPlayerDefeated)
            return;

        // Update health var
        health.Value -= damage;

        // Update UI
        playerUI.UpdateHealth(health.Value);

        //Sync on clients
        HitClientRpc();



        if (health.Value  > 0)
        {
            PlayShipHitSound();
        }
        else // (health.Value <= 0)
        {
            m_isPlayerDefeated = true;
            GameObject go = Instantiate(m_explosionVfxPrefab,transform.position,Quaternion.identity);
            go.GetComponent<NetworkObject>().Spawn(true);
            // Tell the Gameplay manager that I've been defeated
            gameplayManager.PlayerDeath(m_characterData.clientId);
            if (NetworkObject != null && NetworkObject.IsSpawned) {
               NetworkObject.Despawn();
            }
            
            print("we despawned the player ");
        }
    }

    // Set the hit animation effect
    public IEnumerator HitEffect()
    {
        bool active = false;
        float timer = 0f;

        while (timer < m_hitEffectDuration)
        {
            active = !active;
            m_shipRenderer.material.SetInt(k_hitEffect, active ? 1 : 0);
            yield return new WaitForEndOfFrame();
            timer += Time.deltaTime;
        }

        m_shipRenderer.material.SetInt(k_hitEffect, 0);
    }
}