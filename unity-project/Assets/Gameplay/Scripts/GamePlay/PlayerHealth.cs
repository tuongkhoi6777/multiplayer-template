using UnityEngine;

namespace GamePlay
{
    public class PlayerHealth : MonoBehaviour
    {
        public float MaxHealth = 100;
        public float CurrentHealth = 100;

        public void Init()
        {
            CurrentHealth = MaxHealth;
        }

        public void TakeDamage(float damage)
        {
            CurrentHealth -= damage;
            if (IsDeath())
            {
                // TODO: handle death
            }
        }

        public bool IsDeath()
        {
            return CurrentHealth <= 0;
        }
    }
}