using UnityEngine;

namespace _00.Work._01.Scripts
{
    public class SkyBoxChanger : MonoBehaviour
    {
   
    
        [SerializeField] private Material skyboxA;
        [SerializeField] private Material skyboxB;

        private bool isUsingA = true;

        void Start()
        {
            Debug.Assert(skyboxA != null && skyboxB != null);
            RenderSettings.skybox = skyboxA;
            DynamicGI.UpdateEnvironment();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                ToggleSkybox();
            }
        }

        public void ToggleSkybox()
        {
            Debug.Assert(skyboxA != null && skyboxB != null);

            if (isUsingA)
            {
                RenderSettings.skybox = skyboxB;
                isUsingA = false;
            }
            else
            {
                RenderSettings.skybox = skyboxA;
                isUsingA = true;
            }

            DynamicGI.UpdateEnvironment();
        }
    }

    }
