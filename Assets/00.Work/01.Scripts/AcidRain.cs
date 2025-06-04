using UnityEngine;

namespace _00.Work._01.Scripts
{
    public class AcidRain : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Block"))
            {
                BlockHealth bh = other.GetComponent<BlockHealth>();
                if (bh != null) bh.TakeDamage();
            }
            else if (other.CompareTag("Player"))
            {
                Debug.Log("플레이어가 산성비에 맞아 죽었습니다.");
                Destroy(other.gameObject);
            }

            Destroy(gameObject);
        }
    }
}