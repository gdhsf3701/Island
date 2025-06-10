using System.Linq;
using UnityEngine;

namespace _00.Work._01.Scripts
{
    [System.Serializable]
    public class BuildingRaycastHandler
    {
        private Camera camera;
        private float raycastDistance;
        private LayerMask buildableLayer;
        private bool debugMode = true;
        
        public BuildingRaycastHandler(Camera cam, float distance, LayerMask buildable)
        {
            camera = cam;
            raycastDistance = distance;
            buildableLayer = buildable;
        }
        
        public RaycastHit? GetBuildableHit(Vector2 mousePosition)
        {
            if (camera == null) return null;
            
            Ray ray = camera.ScreenPointToRay(mousePosition);
            
            if (debugMode)
            {
                Debug.DrawRay(ray.origin, ray.direction * raycastDistance, Color.red, 0.1f);
            }
            
            // 모든 히트 가져오기
            RaycastHit[] hits = Physics.RaycastAll(ray, raycastDistance);
            
            if (debugMode && hits.Length > 0)
            {
                Debug.Log($"🎯 총 히트 수: {hits.Length}");
                foreach (var hit in hits)
                {
                    Debug.Log($"  - {hit.collider.name} (레이어: {hit.collider.gameObject.layer}, 거리: {hit.distance:F2})");
                }
            }
            
            // 건축 가능한 히트들 필터링 (프리뷰 제외)
            var buildableHits = hits.Where(hit => 
                IsInLayerMask(hit.collider.gameObject.layer, buildableLayer) && 
                !hit.collider.CompareTag("Preview")
            ).OrderBy(h => h.distance).ToArray();
            
            if (debugMode)
            {
                Debug.Log($"✅ 건축 가능한 히트: {buildableHits.Length}개");
            }
            
            // 가장 가까운 유효한 히트 반환
            if (buildableHits.Length > 0)
            {
                var bestHit = buildableHits[0];
                if (debugMode)
                {
                    Debug.Log($"🎯 선택된 히트: {bestHit.collider.name} at {bestHit.point}");
                }
                return bestHit;
            }
            
            // 백업: 더 관대한 조건으로 재시도
            var anyHit = hits.Where(hit => !hit.collider.CompareTag("Preview")).FirstOrDefault();
            if (anyHit.collider != null)
            {
                if (debugMode)
                {
                    Debug.Log($"🔄 백업 히트 사용: {anyHit.collider.name}");
                }
                return anyHit;
            }
            
            return null;
        }
        
        bool IsInLayerMask(int layer, LayerMask layerMask)
        {
            return (layerMask.value & (1 << layer)) != 0;
        }
        
        public void SetDebugMode(bool enabled)
        {
            debugMode = enabled;
        }
    }
}