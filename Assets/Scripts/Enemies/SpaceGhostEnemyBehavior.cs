using Unity.VisualScripting.FullSerializer;
using UnityEngine;

public class SpaceGhostEnemyBehavior : BaseEnemyBehavior
{
    [SerializeField]
    private AudioClip m_damageClip;


    protected override void UpdateActive()
    {
        MoveEnemy();
    }

    protected override void UpdateDefeatedAnimation()
    {
        m_EnemyState = EnemyState.defeated;
    }

    private void OnTriggerEnter2D(Collider2D otherObject)
    {

        // check if it's collided with a player spaceship
        var spacheshipController = otherObject.gameObject.GetComponent<PlayerShipController>();
        if (spacheshipController != null)
        {
            // tell the spaceship that it's taken damage
            spacheshipController.Hit(1);

            // enemy explodes when it collides with the a player's ship
            m_EnemyState = EnemyState.defeatAnimation;
        }
    }

    private void PlayEnemyDamageSound()
    {
        if(m_damageClip != null)
            AudioManager.Instance.PlaySoundEffect(m_damageClip);

    }

    public override void Hit(int damage)
    {
        base.Hit(damage);
        PlayEnemyDamageSound();
    }
}
