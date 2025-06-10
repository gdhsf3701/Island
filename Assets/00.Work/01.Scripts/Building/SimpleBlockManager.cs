using System.Collections.Generic;
using System.Linq;
using _00.Work._01.Scripts.Interface;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _00.Work._01.Scripts
{
    public class SimpleBlockManager : MonoBehaviour
    {
        [Header("Building Settings")]
        [SerializeField] private GameObject[] blockPrefabs;
        [SerializeField] private Grid mapGrid;
        [SerializeField] private LayerMask buildableLayer = 1;
        [SerializeField] private LayerMask obstacleLayer = 0;
        
        [Header("Materials")]
        [SerializeField] private Material validPreviewMaterial;
        [SerializeField] private Material invalidPreviewMaterial;
        [SerializeField] private Material noResourcePreviewMaterial;
        
        [Header("UI")]
        [SerializeField] private UnityEngine.UI.Text resourceDisplayText;
        
        [Header("Building Collision")]
        [SerializeField] private float collisionCheckRadius = 0.4f;
        [SerializeField] private Vector3 collisionBoxSize = new Vector3(0.9f, 0.9f, 0.9f);
        [SerializeField] private bool useBoxCollision = false;
        
        [Header("Camera")]
        [SerializeField] private Camera buildingCamera;
        [SerializeField] private float raycastDistance = 100f;
        
        // 컴포넌트들
        private ResourceManager resourceManager;
        private BuildingInputHandler inputHandler;
        private BuildingRaycastHandler raycastHandler;
        private BuildingPreviewHandler previewHandler;
        private BuildingPlacementHandler placementHandler;
        
        // 상태
        private bool isBuildingMode = false;
        private int selectedBlockIndex = 0;
        private HashSet<Vector3Int> occupiedCells = new HashSet<Vector3Int>();
        
        void Start()
        {
            InitializeComponents();
            SetupEventHandlers();
            
            if (buildingCamera == null)
                buildingCamera = Camera.main;
                
            // 🔧 디버그 모드 활성화
            Debug.Log("🚀 SimpleBlockManager 시작!");
            Debug.Log($"📷 카메라: {buildingCamera?.name}");
            Debug.Log($"🏗️ 블록 프리팹 수: {blockPrefabs?.Length}");
            Debug.Log($"🎯 건축가능 레이어: {buildableLayer.value}");
            Debug.Log($"🚫 장애물 레이어: {obstacleLayer.value}");
        }
        
        void InitializeComponents()
        {
            resourceManager = new ResourceManager(blockPrefabs);
            inputHandler = new BuildingInputHandler();
            raycastHandler = new BuildingRaycastHandler(buildingCamera, raycastDistance, buildableLayer);
            previewHandler = new BuildingPreviewHandler(validPreviewMaterial, invalidPreviewMaterial, noResourcePreviewMaterial);
            placementHandler = new BuildingPlacementHandler(mapGrid, collisionCheckRadius, collisionBoxSize, useBoxCollision, buildableLayer, obstacleLayer);
        }
        
        void SetupEventHandlers()
        {
            inputHandler.OnBuildingModeToggled += ToggleBuildingMode;
            inputHandler.OnBlockSelected += SelectBlock;
            inputHandler.OnBuildingAttempted += AttemptBuilding;
            
            DayManager.OnNewDayStarted += resourceManager.RefillResources;
            resourceManager.OnResourcesChanged += UpdateResourceDisplay;
        }
        
        void OnDestroy()
        {
            inputHandler.OnBuildingModeToggled -= ToggleBuildingMode;
            inputHandler.OnBlockSelected -= SelectBlock;
            inputHandler.OnBuildingAttempted -= AttemptBuilding;
            
            DayManager.OnNewDayStarted -= resourceManager.RefillResources;
            resourceManager.OnResourcesChanged -= UpdateResourceDisplay;
        }
        
        void Update()
        {
            inputHandler.HandleInput(isBuildingMode, blockPrefabs.Length);
            
            if (isBuildingMode)
            {
                HandleBuildingPreview();
            }
        }
        
        void HandleBuildingPreview()
        {
            var mousePos = inputHandler.GetMousePosition();
            var hit = raycastHandler.GetBuildableHit(mousePos);
            
            if (hit.HasValue)
            {
                // 🔧 다양한 면에 설치할 수 있도록 개선
                Vector3 hitPoint = hit.Value.point;
                Vector3 hitNormal = hit.Value.normal;
                
                // 법선 벡터에 따라 설치 위치 조정
                Vector3 buildPosition = CalculateBuildPosition(hitPoint, hitNormal);
                Vector3Int targetCell = mapGrid.WorldToCell(buildPosition);
                Vector3 cellCenter = mapGrid.GetCellCenterWorld(targetCell);
                
                var selectedBlock = blockPrefabs[selectedBlockIndex].GetComponent<IBlock>();
                bool hasResources = resourceManager.HasEnoughResources(selectedBlock);
                bool canPlace = placementHandler.CanPlaceAt(targetCell, cellCenter, occupiedCells);
                
                previewHandler.ShowPreview(cellCenter, selectedBlock, hasResources, canPlace);
                
                // 디버그 정보
                Debug.Log($"🔍 히트포인트: {hitPoint}, 법선: {hitNormal}");
                Debug.Log($"🏗️ 건축위치: {buildPosition}, 셀: {targetCell}, 중심: {cellCenter}");
            }
            else
            {
                previewHandler.HidePreview();
                Debug.Log("❌ 건축 가능한 면을 찾을 수 없음");
            }
        }
        
        // 🔧 법선 벡터에 따른 건축 위치 계산
        Vector3 CalculateBuildPosition(Vector3 hitPoint, Vector3 hitNormal)
        {
            // 그리드 셀 크기 (보통 1x1x1)
            float cellSize = mapGrid.cellSize.x;
            
            // 법선 방향으로 반 셀 크기만큼 이동
            Vector3 offset = hitNormal * (cellSize * 0.5f);
            Vector3 buildPosition = hitPoint + offset;
            
            Debug.Log($"🔧 오프셋 계산: {hitPoint} + {offset} = {buildPosition}");
            
            return buildPosition;
        }
        
        void ToggleBuildingMode()
        {
            isBuildingMode = !isBuildingMode;
            
            if (isBuildingMode)
            {
                previewHandler.CreatePreview(blockPrefabs[selectedBlockIndex]);
            }
            else
            {
                previewHandler.DestroyPreview();
            }
            
            UpdateResourceDisplay();
        }
        
        void SelectBlock(int index)
        {
            if (index >= 0 && index < blockPrefabs.Length)
            {
                selectedBlockIndex = index;
                if (isBuildingMode)
                {
                    previewHandler.CreatePreview(blockPrefabs[selectedBlockIndex]);
                }
                UpdateResourceDisplay();
            }
        }
        
        void AttemptBuilding()
        {
            if (!isBuildingMode) return;
            
            var mousePos = inputHandler.GetMousePosition();
            var hit = raycastHandler.GetBuildableHit(mousePos);
            
            if (!hit.HasValue) 
            {
                Debug.Log("❌ 건축할 면을 찾지 못함");
                return;
            }
            
            // 🔧 동일한 위치 계산 로직 사용
            Vector3 hitPoint = hit.Value.point;
            Vector3 hitNormal = hit.Value.normal;
            Vector3 buildPosition = CalculateBuildPosition(hitPoint, hitNormal);
            Vector3Int targetCell = mapGrid.WorldToCell(buildPosition);
            Vector3 cellCenter = mapGrid.GetCellCenterWorld(targetCell);
            
            var selectedBlock = blockPrefabs[selectedBlockIndex].GetComponent<IBlock>();
            
            Debug.Log($"🏗️ 건축 시도 - 셀: {targetCell}, 위치: {cellCenter}");
            
            if (resourceManager.HasEnoughResources(selectedBlock) && 
                placementHandler.CanPlaceAt(targetCell, cellCenter, occupiedCells))
            {
                PlaceBlock(targetCell, selectedBlock);
            }
            else
            {
                if (!resourceManager.HasEnoughResources(selectedBlock))
                    Debug.Log("❌ 자원 부족");
                else
                    Debug.Log("❌ 해당 위치에 건축 불가");
            }
        }
        
        void PlaceBlock(Vector3Int position, IBlock blockData)
        {
            resourceManager.ConsumeResources(blockData);
            
            Vector3 worldPos = mapGrid.GetCellCenterWorld(position);
            GameObject newBlock = Instantiate(blockPrefabs[selectedBlockIndex], worldPos, Quaternion.identity);
            
            var blockComponent = newBlock.GetComponent<IBlock>();
            blockComponent.OnPlaced(position);
            
            occupiedCells.Add(position);
            
            Debug.Log($"✅ 블록 설치: {blockData.BlockName} at {position}");
        }
        
        void UpdateResourceDisplay()
        {
            if (resourceDisplayText == null) return;
            
            string displayText = resourceManager.GetResourceDisplayText();
            
            if (isBuildingMode && selectedBlockIndex < blockPrefabs.Length)
            {
                var selectedBlock = blockPrefabs[selectedBlockIndex].GetComponent<IBlock>();
                displayText += $"\n\n선택된 블록: {selectedBlock.BlockName}";
                displayText += $"\n필요 자원: {selectedBlock.ResourceCost}개";
                displayText += resourceManager.HasEnoughResources(selectedBlock) ? "\n✅ 건축 가능" : "\n❌ 자원 부족";
            }
            
            resourceDisplayText.text = displayText;
        }
        
        public void RemoveBlock(Vector3Int position)
        {
            occupiedCells.Remove(position);
        }
        
        public void ApplyDisaster(DisasterType disaster, float damage = 25f)
        {
            var allBlocks = FindObjectsOfType<MonoBehaviour>().OfType<IBlock>();
            foreach (var block in allBlocks)
            {
                block.TakeDamage(disaster, damage);
            }
        }
        
        void OnDrawGizmos()
        {
            if (!isBuildingMode || mapGrid == null) return;
            
            // 점유된 셀들 표시
            Gizmos.color = Color.red;
            foreach (var cell in occupiedCells)
            {
                Vector3 cellCenter = mapGrid.GetCellCenterWorld(cell);
                Gizmos.DrawWireCube(cellCenter, Vector3.one * 0.9f);
            }
            
            // 🔧 현재 마우스 레이캐스트 시각화
            if (buildingCamera != null)
            {
                var mousePos = inputHandler.GetMousePosition();
                Ray ray = buildingCamera.ScreenPointToRay(mousePos);
                
                Gizmos.color = Color.yellow;
                Gizmos.DrawRay(ray.origin, ray.direction * raycastDistance);
            }
        }
        
        // 🔧 디버그 메서드들
        [ContextMenu("레이어 설정 확인")]
        public void CheckLayerSettings()
        {
            Debug.Log("=== 레이어 설정 확인 ===");
            Debug.Log($"건축가능 레이어마스크: {buildableLayer.value} ({System.Convert.ToString(buildableLayer.value, 2)})");
            Debug.Log($"장애물 레이어마스크: {obstacleLayer.value} ({System.Convert.ToString(obstacleLayer.value, 2)})");
            
            // 씬의 모든 콜라이더 확인
            var allColliders = FindObjectsOfType<Collider>();
            Debug.Log($"씬의 총 콜라이더 수: {allColliders.Length}");
            
            var layerGroups = allColliders.GroupBy(c => c.gameObject.layer);
            foreach (var group in layerGroups)
            {
                string layerName = LayerMask.LayerToName(group.Key);
                Debug.Log($"레이어 {group.Key} ({layerName}): {group.Count()}개 객체");
                
                if (group.Count() <= 5) // 5개 이하면 이름도 출력
                {
                    foreach (var col in group)
                    {
                        Debug.Log($"  - {col.name}");
                    }
                }
            }
        }
        
        [ContextMenu("마우스 위치 레이캐스트 테스트")]
        public void TestMouseRaycast()
        {
            if (buildingCamera == null)
            {
                Debug.Log("❌ 카메라가 설정되지 않음");
                return;
            }
            
            var mousePos = inputHandler.GetMousePosition();
            Debug.Log($"🖱️ 마우스 위치: {mousePos}");
            
            Ray ray = buildingCamera.ScreenPointToRay(mousePos);
            Debug.Log($"📡 레이: 원점={ray.origin}, 방향={ray.direction}");
            
            RaycastHit[] hits = Physics.RaycastAll(ray, raycastDistance);
            Debug.Log($"🎯 총 히트 수: {hits.Length}");
            
            foreach (var hit in hits.OrderBy(h => h.distance))
            {
                bool isBuildable = (buildableLayer.value & (1 << hit.collider.gameObject.layer)) != 0;
                Debug.Log($"  - {hit.collider.name} | 거리: {hit.distance:F2} | 레이어: {hit.collider.gameObject.layer} | 건축가능: {isBuildable}");
            }
        }
    }
}