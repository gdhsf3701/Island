using System.Collections;
using UnityEngine;

namespace _00.Work._01.Scripts
{
    public class AcidRainManager : MonoBehaviour
    {
        [SerializeField] private GameObject acidRainPrefab;
        [SerializeField] private float rainDuration = 60f;
        [SerializeField] private Transform rainSpawnArea;
        [SerializeField] private SkyBoxChanger skyboxChanger;

        private bool isRaining = false;

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha3) && !isRaining)
            {
                StartAcidRain();
            }
        }

        private void StartAcidRain()
        {
            isRaining = true;
            skyboxChanger.ToggleSkybox(); // Skybox 바로 변경
            StartCoroutine(RainRoutine());
        }

        private IEnumerator RainRoutine()
        {
            InvokeRepeating(nameof(SpawnAcidRain), 0f, 0.1f);

            yield return new WaitForSeconds(rainDuration);

            CancelInvoke(nameof(SpawnAcidRain));
            isRaining = false;

            skyboxChanger.ToggleSkybox(); // 다시 Skybox 원래대로
            Debug.Log("산성비가 멈췄습니다. 다음 날로 넘어갑니다.");
        }

        private void SpawnAcidRain()
        {
            Vector3 spawnPos = new Vector3(
                Random.Range(rainSpawnArea.position.x - 10f, rainSpawnArea.position.x + 10f),
                rainSpawnArea.position.y,
                Random.Range(rainSpawnArea.position.z - 10f, rainSpawnArea.position.z + 10f)
            );

            Instantiate(acidRainPrefab, spawnPos, Quaternion.identity);
        }
    }
}