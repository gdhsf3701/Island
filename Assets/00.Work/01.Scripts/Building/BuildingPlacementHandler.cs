using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace _00.Work._01.Scripts
{
    [System.Serializable]
    public class BuildingPlacementHandler
    {
        private Grid grid;
        private float sphereRadius;
        private Vector3 boxSize;
        private bool useBoxCollision;
        private LayerMask buildableLayer;
        private LayerMask obstacleLayer;
        
        public BuildingPlacementHandler(Grid mapGrid, float radius, Vector3 boxSize, bool useBox, LayerMask buildable, LayerMask obstacle)
        {
            grid = mapGrid;
            sphereRadius = radius;
            this.boxSize = boxSize;
            useBoxCollision = useBox;
            buildableLayer = buildable;
            obstacleLayer = obstacle;
        }
        
        public bool CanPlaceAt(Vector3Int gridCell, Vector3 worldPosition, HashSet<Vector3Int> occupiedCells)
        {
            Debug.Log($"🔍 배치 검사 시작 - 그리드셀: {gridCell}, 월드위치: {worldPosition}");
            
            // 그리드 셀 점유 체크
            if (occupiedCells.Contains(gridCell))
            {
                Debug.Log($"❌ 셀 이미 점유됨: {gridCell}");
                return false;
            }
            
            // 물리적 충돌 체크
            Collider[] overlapping = GetOverlappingColliders(worldPosition);
            Debug.Log($"🔍 충돌 검사 - 감지된 콜라이더: {overlapping.Length}개");
            
            // 충돌하는 객체 분석
            var conflictingObjects = overlapping.Where(col => 
            {
                bool isPreview = col.CompareTag("Preview");
                bool isBuildable = IsInLayerMask(col.gameObject.layer, buildableLayer);
                
                Debug.Log($"  - {col.name}: Preview={isPreview}, Buildable={isBuildable}, Layer={col.gameObject.layer}");
                
                // Preview는 무시, 건축가능한 레이어도 충돌로 보지 않음
                return !isPreview && !isBuildable;
            }).ToArray();
            
            bool canPlace = conflictingObjects.Length == 0;
            Debug.Log($"🏗️ 최종 판정: {(canPlace ? "건축 가능" : "건축 불가")} (충돌객체: {conflictingObjects.Length}개)");
            
            if (!canPlace)
            {
                foreach (var obj in conflictingObjects)
                {
                    Debug.Log($"  충돌: {obj.name} (레이어: {obj.gameObject.layer})");
                }
            }
            
            return canPlace;
        }
        
        Collider[] GetOverlappingColliders(Vector3 worldPosition)
        {
            if (useBoxCollision)
            {
                return Physics.OverlapBox(worldPosition, boxSize / 2, Quaternion.identity);
            }
            else
            {
                return Physics.OverlapSphere(worldPosition, sphereRadius);
            }
        }
        
        bool IsInLayerMask(int layer, LayerMask layerMask)
        {
            return (layerMask.value & (1 << layer)) != 0;
        }
    }
}