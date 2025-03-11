using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
using Unity.Netcode;

/*
    This script set and update the values of the player ship UI
    set: by game manager
*/

public class PlayerUI : NetworkBehaviour
{
    // Struct for a better organization of the health UI 
    [Serializable]
    public struct HealthUI
    {
        public GameObject healthUI;
        public Image playerIconImage;
        public TextMeshProUGUI playerIdText;
        public Slider healthSlider;
        public Image healthImage;
        public HealthColor healthColor;
        public GameObject[] powerUp;
    }
    //Struct for organizing the Death UI
    [Serializable]
    public struct DeathUI
    {
        public GameObject deathUI;
        public Image deathIcon;
        public TextMeshProUGUI deathIdText;
    }


    [SerializeField]
    HealthUI m_healthUI;                // A struct for all the data relate to the health UI

    [SerializeField]
    DeathUI m_deathUI;                // A struct for all the data relate to the death UI

    [SerializeField]
    [Header("Set in runtime")]
    public int maxHealth;               // Max health the player has, use for the conversion to the
                                        // slider and the coloring of the bar
    
    public void UpdateHealth(float currentHealth)
    {
       
        if (!IsServer)
            return;
        
        //don't let health go below
        currentHealth = currentHealth < 0 ? 0 : currentHealth;  

        float convertedHealth = (float)currentHealth / (float)maxHealth;

        m_healthUI.healthSlider.value = convertedHealth;
        m_healthUI.healthImage.color = m_healthUI.healthColor.GetHealthColor(convertedHealth);

        if (currentHealth <= 0f)
        {
            // Turn off lifeUI
            m_healthUI.healthUI.SetActive(false);

            //Turn On deathUI
            m_deathUI.deathUI.SetActive(true);
            print("DeathUI active on Server now ");

        }
        UpdateHealthClientRpc(convertedHealth);
    }

    [ClientRpc]
    void UpdateHealthClientRpc(float currentHealth)
    {
        if(IsServer) return;
        m_healthUI.healthSlider.value = currentHealth;
        m_healthUI.healthImage.color = m_healthUI.healthColor.GetHealthColor(currentHealth);

        if (currentHealth <= 0f)
        {
            // Turn off lifeUI
            m_healthUI.healthUI.SetActive(false);

            //Turn On deathUI
            m_deathUI.deathUI.SetActive(true);
            print("DeathUI active on clients now ");


        }
    }

    // TODO: check if the initial values are set on client
    // Set the initial values of the UI
    public void SetUI(
        int playerId,
        Sprite playerIcon,
        Sprite deathIcon,
        int maxHealth,
        Color color)
    {
        m_healthUI.playerIconImage.sprite = playerIcon;
        m_healthUI.playerIdText.color = color;
        m_healthUI.playerIdText.text = $"P{(playerId + 1)}";

        m_deathUI.deathIcon.color = color;
        m_deathUI.deathIcon.sprite = deathIcon;

        this.maxHealth = maxHealth;
        m_healthUI.healthImage.color = m_healthUI.healthColor.normalColor;

        // Turn on my lifeUI
        m_healthUI.healthUI.SetActive(true);

        //make sure death UI inactive
        m_deathUI.deathUI.SetActive(false);

    }


    // Activate/deactivate the power up icons base on the index pass
    public void UpdatePowerUp(int index, bool hasSpecial)
    {
        m_healthUI.powerUp[index - 1].SetActive(hasSpecial);
        UpdatePowerUpClientRpc(index, hasSpecial);
    }

    [ClientRpc]
    void UpdatePowerUpClientRpc(int index, bool hasSpecial)
    {
        m_healthUI.powerUp[index - 1].SetActive(hasSpecial);
    }
}