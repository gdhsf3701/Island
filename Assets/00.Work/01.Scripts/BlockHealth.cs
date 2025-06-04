using UnityEngine;

namespace _00.Work._01.Scripts
{
    public class BlockHealth : MonoBehaviour
    {
        [SerializeField] private int health = 3;

        public void TakeDamage()
        {
            health--;
            if (health <= 0)
            {
                Destroy(gameObject);
            }
        }
    }
}