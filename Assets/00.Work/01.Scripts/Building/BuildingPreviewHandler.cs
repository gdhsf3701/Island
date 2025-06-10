using _00.Work._01.Scripts.Interface;
using UnityEngine;

namespace _00.Work._01.Scripts
{
    [System.Serializable]
    public class BuildingPreviewHandler
    {
        private Material validMaterial;
        private Material invalidMaterial;
        private Material noResourceMaterial;
        private GameObject currentPreview;
        
        public BuildingPreviewHandler(Material valid, Material invalid, Material noResource)
        {
            validMaterial = valid;
            invalidMaterial = invalid;
            noResourceMaterial = noResource;
        }
        
        public void CreatePreview(GameObject blockPrefab)
        {
            DestroyPreview();
            
            if (blockPrefab != null)
            {
                currentPreview = Object.Instantiate(blockPrefab);
                
                var colliders = currentPreview.GetComponentsInChildren<Collider>();
                foreach (var col in colliders)
                {
                    col.enabled = false;
                }
                
                currentPreview.tag = "Preview";
                currentPreview.SetActive(false);
            }
        }
        
        public void DestroyPreview()
        {
            if (currentPreview != null)
            {
                Object.Destroy(currentPreview);
                currentPreview = null;
            }
        }
        
        public void ShowPreview(Vector3 position, IBlock block, bool hasResources, bool canPlace)
        {
            if (currentPreview == null) return;
            
            if (!currentPreview.activeInHierarchy)
                currentPreview.SetActive(true);
            
            currentPreview.transform.position = position;
            
            Material targetMaterial = GetPreviewMaterial(hasResources, canPlace);
            SetPreviewMaterial(targetMaterial);
        }
        
        public void HidePreview()
        {
            if (currentPreview != null && currentPreview.activeInHierarchy)
                currentPreview.SetActive(false);
        }
        
        Material GetPreviewMaterial(bool hasResources, bool canPlace)
        {
            if (!hasResources) return noResourceMaterial;
            if (!canPlace) return invalidMaterial;
            return validMaterial;
        }
        
        void SetPreviewMaterial(Material material)
        {
            if (currentPreview == null || material == null) return;
            
            var renderers = currentPreview.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                renderer.material = material;
            }
        }
    }
}