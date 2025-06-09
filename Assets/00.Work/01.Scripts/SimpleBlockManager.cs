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
        [SerializeField] private bool useSmartObstacleDetection = true;
        [SerializeField] private bool debugRaycast = true;
        
        [Header("Materials")]
        [SerializeField] private Material validPreviewMaterial;
        [SerializeField] private Material invalidPreviewMaterial;
        [SerializeField] private Material noResourcePreviewMaterial;
        
        [Header("UI")]
        [SerializeField] private UnityEngine.UI.Text resourceDisplayText;
        
        [Header("Building Collision")]
        [SerializeField] private float collisionCheckRadius = 0.4f;
        [SerializeField] private bool useBoxCollisionCheck = false;
        [SerializeField] private Vector3 collisionBoxSize = new Vector3(0.9f, 0.9f, 0.9f);
        
        [Header("Camera and Raycast")]
        [SerializeField] private Camera buildingCamera;
        [SerializeField] private float raycastDistance = 100f;
        [SerializeField] private Vector3 gridOffset = Vector3.zero;
        
        // 🔧 개선된 레이캐스트 설정
        [Header("Raycast Improvements")]
        [SerializeField] private float raycastTolerance = 0.1f; // 레이캐스트 허용 오차
        [SerializeField] private bool useClosestHit = true; // 가장 가까운 히트 사용
        [SerializeField] private float gridSnapTolerance = 0.05f; // 그리드 스냅 허용 오차
        
        // 자원 관리
        private Dictionary<string, int> resources = new Dictionary<string, int>();
        private Dictionary<string, int> dailyUsage = new Dictionary<string, int>();
        
        // 건축 상태
        private GameObject currentPreview;
        private bool isBuildingMode = false;
        private int selectedBlockIndex = 0;
        private HashSet<Vector3Int> occupiedCells = new HashSet<Vector3Int>();
        
        // 마우스 입력 관련
        private Vector2 mousePosition;
        private bool isMouseOverBuildable = false;
        
        // 🔧 개선된 상태 추적
        private Vector3Int lastValidCell = Vector3Int.zero;
        private bool hasValidTarget = false;
        
        void Start()
        {
            InitializeResources();
            UpdateResourceDisplay();
            
            if (buildingCamera == null)
                buildingCamera = Camera.main;
            
            DayManager.OnNewDayStarted += RefillResources;
        }
        
        void OnDestroy()
        {
            DayManager.OnNewDayStarted -= RefillResources;
        }
        
        void InitializeResources()
        {
            foreach (var prefab in blockPrefabs)
            {
                var block = prefab.GetComponent<IBlock>();
                if (block != null)
                {
                    resources[block.BlockName] = 5;
                    dailyUsage[block.BlockName] = 0;
                }
            }
        }
        
        void Update()
        {
            UpdateMousePosition();
            HandleInput();
            
            if (isBuildingMode)
            {
                HandleBuildingPreview();
            }
        }
        
        void UpdateMousePosition()
        {
            if (Mouse.current != null)
            {
                mousePosition = Mouse.current.position.ReadValue();
            }
        }
        
        void HandleInput()
        {
            if (Keyboard.current.bKey.wasPressedThisFrame)
            {
                ToggleBuildingMode();
            }
            
            for (int i = 0; i < blockPrefabs.Length && i < 9; i++)
            {
                if (Keyboard.current[(Key)(Key.Digit1 + i)].wasPressedThisFrame)
                {
                    SelectBlock(i);
                }
            }
            
            if (isBuildingMode && Mouse.current != null && Mouse.current.scroll.ReadValue().y != 0)
            {
                int direction = Mouse.current.scroll.ReadValue().y > 0 ? -1 : 1;
                int newIndex = (selectedBlockIndex + direction) % blockPrefabs.Length;
                if (newIndex < 0) newIndex = blockPrefabs.Length - 1;
                SelectBlock(newIndex);
            }
        }
        
        void ToggleBuildingMode()
        {
            isBuildingMode = !isBuildingMode;
            
            if (isBuildingMode)
            {
                CreatePreview();
            }
            else
            {
                DestroyPreview();
            }
            
            Debug.Log($"건축 모드: {(isBuildingMode ? "활성화" : "비활성화")}");
        }
        
        void SelectBlock(int index)
        {
            if (index >= 0 && index < blockPrefabs.Length)
            {
                selectedBlockIndex = index;
                if (isBuildingMode)
                {
                    CreatePreview();
                }
                UpdateResourceDisplay();
                
                var blockName = blockPrefabs[selectedBlockIndex].GetComponent<IBlock>()?.BlockName ?? "Unknown";
                Debug.Log($"블록 선택: {blockName} (인덱스: {index})");
            }
        }
        
        void CreatePreview()
        {
            DestroyPreview();
            
            if (selectedBlockIndex >= 0 && selectedBlockIndex < blockPrefabs.Length)
            {
                currentPreview = Instantiate(blockPrefabs[selectedBlockIndex]);
                
                var colliders = currentPreview.GetComponentsInChildren<Collider>();
                foreach (var col in colliders)
                {
                    col.enabled = false;
                }
                
                currentPreview.tag = "Preview";
                currentPreview.SetActive(false);
            }
        }
        
        void DestroyPreview()
        {
            if (currentPreview != null)
            {
                Destroy(currentPreview);
                currentPreview = null;
            }
        }
        
        // 🔧 개선된 건축 프리뷰 처리
        void HandleBuildingPreview()
        {
            if (currentPreview == null || buildingCamera == null) return;
            
            Ray ray = buildingCamera.ScreenPointToRay(mousePosition);
            
            if (debugRaycast)
            {
                Debug.DrawRay(ray.origin, ray.direction * raycastDistance, Color.red, 0.1f);
            }
            
            // 🔧 다중 레이캐스트 전략 사용
            var validHit = GetBestBuildableHitWithFallback(ray);
            
            if (validHit.HasValue)
            {
                isMouseOverBuildable = true;
                ProcessValidHit(validHit.Value);
            }
            else
            {
                ProcessInvalidHit();
            }
        }
        
        // 🔧 가장 적합한 건축 가능한 히트 찾기 (기본 전략)
        private RaycastHit? GetBestBuildableHit(Ray ray)
        {
            RaycastHit[] hits = Physics.RaycastAll(ray, raycastDistance);
            
            if (hits.Length == 0) return null;
            
            // 건축 가능한 히트들만 필터링
            var buildableHits = FilterBuildableHits(hits);
            
            if (buildableHits.Length == 0) return null;
            
            if (debugRaycast)
            {
                Debug.Log($"발견된 건축 가능한 히트: {buildableHits.Length}개");
                foreach (var hit in buildableHits)
                {
                    Debug.Log($"  - {hit.collider.name} at {hit.point} (거리: {hit.distance:F2})");
                }
            }
            
            return SelectBestHit(buildableHits);
        }
        private RaycastHit? GetBestBuildableHitWithFallback(Ray ray)
        {
            // 전략 1: 기본 레이캐스트
            var hit = GetBestBuildableHit(ray);
            if (hit.HasValue) return hit;
            
            // 전략 2: 확장된 레이캐스트 (더 먼 거리)
            RaycastHit[] extendedHits = Physics.RaycastAll(ray, raycastDistance * 2f);
            var buildableHits = FilterBuildableHits(extendedHits);
            if (buildableHits.Length > 0)
            {
                if (debugRaycast)
                    Debug.Log($"🔄 확장 레이캐스트로 발견: {buildableHits.Length}개");
                return SelectBestHit(buildableHits);
            }
            
            // 전략 3: 스크린 중심에서 아래쪽으로 레이캐스트
            Vector3 screenCenter = new Vector3(Screen.width * 0.5f, Screen.height * 0.3f, 0f);
            Ray downwardRay = buildingCamera.ScreenPointToRay(screenCenter);
            RaycastHit[] downwardHits = Physics.RaycastAll(downwardRay, raycastDistance);
            var downwardBuildable = FilterBuildableHits(downwardHits);
            
            if (downwardBuildable.Length > 0)
            {
                if (debugRaycast)
                    Debug.Log($"🔄 하향 레이캐스트로 발견: {downwardBuildable.Length}개");
                    
                // 마우스 위치와 가장 가까운 히트 선택
                return SelectClosestToMouse(downwardBuildable, mousePosition);
            }
            
            // 전략 4: 구체 캐스트 (더 넓은 범위)
            if (Physics.SphereCast(ray, 0.5f, out RaycastHit sphereHit, raycastDistance))
            {
                if (IsInLayerMask(sphereHit.collider.gameObject.layer, buildableLayer) && 
                    !sphereHit.collider.CompareTag("Preview"))
                {
                    if (debugRaycast)
                        Debug.Log($"🔄 구체 캐스트로 발견: {sphereHit.collider.name}");
                    return sphereHit;
                }
            }
            
            return null;
        }
        
        // 건축 가능한 히트들 필터링
        private RaycastHit[] FilterBuildableHits(RaycastHit[] hits)
        {
            return hits.Where(hit => 
                IsInLayerMask(hit.collider.gameObject.layer, buildableLayer) && 
                !hit.collider.CompareTag("Preview")
            ).ToArray();
        }
        
        // 마우스 위치와 가장 가까운 히트 선택
        private RaycastHit SelectClosestToMouse(RaycastHit[] hits, Vector2 mousePos)
        {
            RaycastHit bestHit = hits[0];
            float bestScore = float.MaxValue;
            
            foreach (var hit in hits)
            {
                Vector3 screenPoint = buildingCamera.WorldToScreenPoint(hit.point);
                float distance = Vector2.Distance(mousePos, new Vector2(screenPoint.x, screenPoint.y));
                
                // 거리와 레이캐스트 거리를 조합한 점수
                float score = distance + (hit.distance * 0.1f);
                
                if (score < bestScore)
                {
                    bestScore = score;
                    bestHit = hit;
                }
            }
            
            return bestHit;
        }
        
        // 최적의 히트 선택
        private RaycastHit SelectBestHit(RaycastHit[] hits)
        {
            if (useClosestHit)
            {
                return hits.OrderBy(h => h.distance).First();
            }
            else
            {
                return hits.OrderBy(h => Vector3.Angle(Vector3.up, h.normal)).First();
            }
        }
        
        // 🔧 유효한 히트 처리
        private void ProcessValidHit(RaycastHit hit)
        {
            Vector3 adjustedHitPoint = hit.point + gridOffset;
            Vector3Int targetCell = mapGrid.WorldToCell(adjustedHitPoint);
            
            // 🔧 그리드 스냅 안정화
            targetCell = StabilizeGridCell(adjustedHitPoint, targetCell);
            
            Vector3 cellCenter = mapGrid.GetCellCenterWorld(targetCell);
            
            // 프리뷰 활성화 및 위치 설정
            if (!currentPreview.activeInHierarchy)
                currentPreview.SetActive(true);
            
            currentPreview.transform.position = cellCenter;
            
            // 상태 업데이트
            lastValidCell = targetCell;
            hasValidTarget = true;
            
            // 건축 가능 여부 체크
            var blockComponent = blockPrefabs[selectedBlockIndex].GetComponent<IBlock>();
            bool hasResources = HasEnoughResources(blockComponent);
            bool canPlace = CanPlaceAtPosition(targetCell, cellCenter);
            
            SetPreviewMaterial(hasResources, canPlace);
            
            // 클릭 시 건축 실행
            if (Mouse.current.leftButton.wasPressedThisFrame && canPlace && hasResources)
            {
                PlaceBlock(targetCell, blockComponent);
            }
            
            if (debugRaycast)
            {
                Debug.Log($"🎯 타겟: {hit.collider.name}, 셀: {targetCell}, 위치: {cellCenter}");
                Debug.Log($"법선: {hit.normal}, 거리: {hit.distance:F2}");
            }
        }
        
        // 🔧 무효한 히트 처리
        private void ProcessInvalidHit()
        {
            isMouseOverBuildable = false;
            hasValidTarget = false;
            
            if (currentPreview.activeInHierarchy)
                currentPreview.SetActive(false);
        }
        
        // 🔧 그리드 셀 안정화 (부동소수점 오차 보정)
        private Vector3Int StabilizeGridCell(Vector3 worldPos, Vector3Int calculatedCell)
        {
            // 현재 계산된 셀이 유효한지 확인
            Vector3 cellCenter = mapGrid.GetCellCenterWorld(calculatedCell);
            float distance = Vector3.Distance(worldPos, cellCenter);
            
            if (distance <= gridSnapTolerance)
            {
                return calculatedCell; // 충분히 가까우면 그대로 사용
            }
            
            // 주변 셀들도 검사해서 가장 가까운 셀 찾기
            Vector3Int bestCell = calculatedCell;
            float bestDistance = distance;
            
            for (int x = -1; x <= 1; x++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    Vector3Int neighborCell = calculatedCell + new Vector3Int(x, 0, z);
                    Vector3 neighborCenter = mapGrid.GetCellCenterWorld(neighborCell);
                    float neighborDistance = Vector3.Distance(worldPos, neighborCenter);
                    
                    if (neighborDistance < bestDistance)
                    {
                        bestDistance = neighborDistance;
                        bestCell = neighborCell;
                    }
                }
            }
            
            if (debugRaycast && bestCell != calculatedCell)
            {
                Debug.Log($"🔧 그리드 셀 보정: {calculatedCell} → {bestCell} (거리: {bestDistance:F3})");
            }
            
            return bestCell;
        }
        
        // 🔧 개선된 충돌 감지
        bool CanPlaceAtPosition(Vector3Int gridCell, Vector3 worldPosition)
        {
            // 1. 그리드 셀 점유 체크
            if (occupiedCells.Contains(gridCell))
            {
                if (debugRaycast)
                    Debug.Log($"❌ 셀 점유됨: {gridCell}");
                return false;
            }
            
            // 2. 물리적 충돌 체크
            Collider[] overlapping = GetOverlappingColliders(worldPosition);
            
            // 3. 충돌 분석
            var conflictingObjects = AnalyzeCollisions(overlapping);
            
            if (conflictingObjects.Count > 0)
            {
                if (debugRaycast)
                {
                    Debug.Log($"❌ 충돌 객체: {string.Join(", ", conflictingObjects.Select(c => c.name))}");
                }
                return false;
            }
            
            if (debugRaycast)
                Debug.Log($"✅ 건축 가능: {worldPosition} (그리드: {gridCell})");
            
            return true;
        }
        
        // 🔧 겹치는 콜라이더 검출
        private Collider[] GetOverlappingColliders(Vector3 worldPosition)
        {
            if (useBoxCollisionCheck)
            {
                return Physics.OverlapBox(worldPosition, collisionBoxSize / 2, Quaternion.identity);
            }
            else
            {
                return Physics.OverlapSphere(worldPosition, collisionCheckRadius);
            }
        }
        
        // 🔧 충돌 분석
        private List<Collider> AnalyzeCollisions(Collider[] overlapping)
        {
            var conflictingObjects = new List<Collider>();
            
            foreach (var col in overlapping)
            {
                // Preview 태그는 무시
                if (col.gameObject.CompareTag("Preview"))
                    continue;
                
                if (useSmartObstacleDetection)
                {
                    // 건축 가능한 레이어가 아닌 것들만 장애물로 판단
                    if (!IsInLayerMask(col.gameObject.layer, buildableLayer))
                    {
                        conflictingObjects.Add(col);
                    }
                }
                else
                {
                    // 건축 가능한 레이어는 무시
                    if (IsInLayerMask(col.gameObject.layer, buildableLayer))
                        continue;
                    
                    // 장애물 레이어에 포함된 것만 체크
                    if (IsInLayerMask(col.gameObject.layer, obstacleLayer))
                    {
                        conflictingObjects.Add(col);
                    }
                }
            }
            
            return conflictingObjects;
        }
        
        bool IsInLayerMask(int layer, LayerMask layerMask)
        {
            return (layerMask.value & (1 << layer)) != 0;
        }
        
        bool HasEnoughResources(IBlock block)
        {
            return resources.ContainsKey(block.BlockName) && resources[block.BlockName] >= block.ResourceCost;
        }
        
        void SetPreviewMaterial(bool hasResources, bool canPlace)
        {
            Material targetMaterial;
            
            if (!hasResources)
            {
                targetMaterial = noResourcePreviewMaterial;
            }
            else if (!canPlace)
            {
                targetMaterial = invalidPreviewMaterial;
            }
            else
            {
                targetMaterial = validPreviewMaterial;
            }
            
            var renderers = currentPreview.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                renderer.material = targetMaterial;
            }
        }
        
        void PlaceBlock(Vector3Int position, IBlock blockData)
        {
            resources[blockData.BlockName] -= blockData.ResourceCost;
            dailyUsage[blockData.BlockName] += blockData.ResourceCost;
            
            Vector3 worldPos = mapGrid.GetCellCenterWorld(position);
            GameObject newBlock = Instantiate(blockPrefabs[selectedBlockIndex], worldPos, Quaternion.identity);
            
            var blockComponent = newBlock.GetComponent<IBlock>();
            blockComponent.OnPlaced(position);
            
            occupiedCells.Add(position);
            
            UpdateResourceDisplay();
            
            Debug.Log($"✅ 블록 설치: {blockData.BlockName} at {position}");
        }
        
        void RefillResources()
        {
            foreach (var usage in dailyUsage.ToList())
            {
                if (usage.Value > 0)
                {
                    int refillAmount = Mathf.CeilToInt(usage.Value * 1.3f);
                    resources[usage.Key] += refillAmount;
                }
            }
            
            foreach (var key in resources.Keys.ToList())
            {
                resources[key] += 5;
            }
            
            foreach (var key in dailyUsage.Keys.ToList())
            {
                dailyUsage[key] = 0;
            }
            
            UpdateResourceDisplay();
            Debug.Log("📦 자원 보충 완료!");
        }
        
        void UpdateResourceDisplay()
        {
            if (resourceDisplayText == null) return;
            
            string displayText = "=== 보유 자원 ===\n";
            
            foreach (var resource in resources)
            {
                int usage = dailyUsage.ContainsKey(resource.Key) ? dailyUsage[resource.Key] : 0;
                displayText += $"{resource.Key}: {resource.Value}개";
                if (usage > 0)
                {
                    displayText += $" (오늘 사용: {usage})";
                }
                displayText += "\n";
            }
            
            if (isBuildingMode && selectedBlockIndex < blockPrefabs.Length)
            {
                var selectedBlock = blockPrefabs[selectedBlockIndex].GetComponent<IBlock>();
                displayText += $"\n선택된 블록: {selectedBlock.BlockName}";
                displayText += $"\n필요 자원: {selectedBlock.ResourceCost}개";
                
                bool canBuild = HasEnoughResources(selectedBlock);
                displayText += canBuild ? "\n✅ 건축 가능" : "\n❌ 자원 부족";
                
                displayText += isMouseOverBuildable ? "\n🎯 건축 영역" : "\n❌ 건축 불가 영역";
                
                // 🔧 추가 디버그 정보
                if (hasValidTarget)
                {
                    displayText += $"\n📍 타겟 셀: {lastValidCell}";
                }
            }
            
            resourceDisplayText.text = displayText;
        }
        
        public void RemoveBlock(Vector3Int position)
        {
            occupiedCells.Remove(position);
        }
        
        public void ApplyDisasterToAllBlocks(DisasterType disaster, float baseDamage = 25f)
        {
            var allBlocks = FindObjectsOfType<MonoBehaviour>().OfType<IBlock>();
            
            foreach (var block in allBlocks)
            {
                block.TakeDamage(disaster, baseDamage);
            }
            
            Debug.Log($"🌪️ {GetKoreanDisasterName(disaster)} 발생!");
        }
        
        string GetKoreanDisasterName(DisasterType type)
        {
            return type switch
            {
                DisasterType.AcidRain => "산성비",
                DisasterType.StrongWind => "강풍",
                DisasterType.Earthquake => "지진",
                DisasterType.Wildfire => "산불",
                DisasterType.Tsunami => "해일",
                DisasterType.Sandstorm => "모래폭풍",
                DisasterType.Lightning => "벼락",
                _ => type.ToString()
            };
        }
        
        [ContextMenu("산성비 발생")]
        public void TriggerAcidRain() => ApplyDisasterToAllBlocks(DisasterType.AcidRain);
        
        [ContextMenu("지진 발생")]
        public void TriggerEarthquake() => ApplyDisasterToAllBlocks(DisasterType.Earthquake);
        
        [ContextMenu("벼락 발생")]
        public void TriggerLightning() => ApplyDisasterToAllBlocks(DisasterType.Lightning);
        
        // 🔧 향상된 디버그 메서드
        [ContextMenu("레이캐스트 상세 분석")]
        public void DetailedRaycastAnalysis()
        {
            if (buildingCamera == null)
            {
                Debug.Log("카메라가 설정되지 않았습니다.");
                return;
            }
            
            Ray ray = buildingCamera.ScreenPointToRay(mousePosition);
            RaycastHit[] hits = Physics.RaycastAll(ray, raycastDistance);
            
            Debug.Log("=== 상세 레이캐스트 분석 ===");
            Debug.Log($"마우스 위치: {mousePosition}");
            Debug.Log($"레이 원점: {ray.origin}");
            Debug.Log($"레이 방향: {ray.direction}");
            Debug.Log($"총 히트 수: {hits.Length}");
            
            var sortedHits = hits.OrderBy(h => h.distance).ToArray();
            
            for (int i = 0; i < sortedHits.Length; i++)
            {
                var hit = sortedHits[i];
                bool isBuildable = IsInLayerMask(hit.collider.gameObject.layer, buildableLayer);
                bool isPreview = hit.collider.CompareTag("Preview");
                
                string status = isPreview ? "[PREVIEW]" : isBuildable ? "[BUILDABLE]" : "[OTHER]";
                
                Debug.Log($"{i+1}. {hit.collider.name} {status}");
                Debug.Log($"   거리: {hit.distance:F3}, 위치: {hit.point}");
                Debug.Log($"   레이어: {hit.collider.gameObject.layer} ({LayerMask.LayerToName(hit.collider.gameObject.layer)})");
                Debug.Log($"   법선: {hit.normal}");
                
                if (isBuildable && !isPreview)
                {
                    Vector3Int cell = mapGrid.WorldToCell(hit.point);
                    Vector3 cellCenter = mapGrid.GetCellCenterWorld(cell);
                    Debug.Log($"   그리드 셀: {cell}, 셀 중심: {cellCenter}");
                    Debug.Log($"   점유 상태: {(occupiedCells.Contains(cell) ? "점유됨" : "비어있음")}");
                }
                
                Debug.Log("");
            }
        }
        
        void OnDrawGizmos()
        {
            if (!isBuildingMode || currentPreview == null) return;
            
            // 충돌 검사 영역 표시
            Gizmos.color = Color.yellow;
            if (useBoxCollisionCheck)
            {
                Gizmos.DrawWireCube(currentPreview.transform.position, collisionBoxSize);
            }
            else
            {
                Gizmos.DrawWireSphere(currentPreview.transform.position, collisionCheckRadius);
            }
            
            // 점유된 셀들 표시
            if (mapGrid != null)
            {
                Gizmos.color = Color.red;
                foreach (var cell in occupiedCells)
                {
                    Vector3 cellCenter = mapGrid.GetCellCenterWorld(cell);
                    Gizmos.DrawWireCube(cellCenter, Vector3.one * 0.9f);
                }
                
                // 🔧 현재 타겟 셀 강조 표시
                if (hasValidTarget)
                {
                    Gizmos.color = Color.green;
                    Vector3 targetCenter = mapGrid.GetCellCenterWorld(lastValidCell);
                    Gizmos.DrawWireCube(targetCenter, Vector3.one * 1.1f);
                }
            }
        }
    }
}