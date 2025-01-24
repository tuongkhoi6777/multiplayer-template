using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace GamePlay
{
    public class Bullet : NetworkBehaviour
    {
        public float speed = 20f;  // Bullet speed
        public float damage = 0;    // Bullet damage
        private HashSet<PlayerHealth> hitPlayers = new();

        void Start()
        {
            // Destroy the bullet after 5 seconds to clean up
            Destroy(gameObject, 5f);
        }

        public void Init(Vector3 direction, float dmg)
        {
            // Apply velocity to the bullet in the given direction
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = direction * speed;
            }

            damage = dmg;
        }

        void OnTriggerEnter(Collider other)
        {
            if (!isServer) return;
            
            PlayerHealth health = other.GetComponentInParent<PlayerHealth>();

            // Check if the bullet hits the player
            if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
            {

                if (!hitPlayers.Contains(health))
                {
                    hitPlayers.Add(health);

                    switch (other.tag)
                    {
                        // x4 damage
                        case "Player_Head":
                            health.TakeDamage(damage * 4f);
                            break;

                        // x1 damage
                        case "Player_Body":
                            health.TakeDamage(damage);
                            break;

                        // x0.75 damage
                        case "Player_Leg":
                        case "Player_Arms":
                            health.TakeDamage(damage * 0.75f);
                            break;
                    }
                }

            }
        }
    }
}