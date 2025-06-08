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
    [SerializeField] private GameObject[] blockPrefabs;  // 각 블록 프리팹들 (IBlock 구현체들)
    [SerializeField] private Grid mapGrid;
    [SerializeField] private LayerMask buildableLayer;
    
    [Header("Materials")]
    [SerializeField] private Material validPreviewMaterial;
    [SerializeField] private Material invalidPreviewMaterial;
    [SerializeField] private Material noResourcePreviewMaterial;
    
    [Header("UI")]
    [SerializeField] private UnityEngine.UI.Text resourceDisplayText;
    
    // 자원 관리 (각 블록 타입별로)
    private Dictionary<string, int> resources = new Dictionary<string, int>();
    private Dictionary<string, int> dailyUsage = new Dictionary<string, int>();
    
    // 건축 상태
    private GameObject currentPreview;
    private bool isBuildingMode = false;
    private int selectedBlockIndex = 0;
    private HashSet<Vector3Int> occupiedCells = new HashSet<Vector3Int>();
    
    void Start()
    {
        InitializeResources();
        UpdateResourceDisplay();
        
        // 매일 자원 보충 (테스트용으로 15초마다)
        InvokeRepeating(nameof(DailyResourceRefill), 15f, 15f);
    }
    
    void InitializeResources()
    {
        // 각 블록 타입별로 초기 자원 설정
        foreach (var prefab in blockPrefabs)
        {
            var block = prefab.GetComponent<IBlock>();
            if (block != null)
            {
                resources[block.BlockName] = 10;
                dailyUsage[block.BlockName] = 0;
            }
        }
    }
    
    void Update()
    {
        HandleInput();
        
        if (isBuildingMode)
        {
            HandleBuildingPreview();
        }
    }
    
    void HandleInput()
    {
        // B키로 건축 모드 토글
        if (Keyboard.current.bKey.wasPressedThisFrame)
        {
            ToggleBuildingMode();
        }
        
        // 숫자 키로 블록 선택
        for (int i = 0; i < blockPrefabs.Length && i < 9; i++)
        {
            if (Keyboard.current[(Key)(Key.Digit1 + i)].wasPressedThisFrame)
            {
                SelectBlock(i);
            }
        }
        
        // 마우스 휠로 블록 선택
        if (isBuildingMode && Mouse.current.scroll.ReadValue().y != 0)
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
        }
    }
    
    void CreatePreview()
    {
        DestroyPreview();
        
        currentPreview = Instantiate(blockPrefabs[selectedBlockIndex]);
        
        // 콜라이더 비활성화
        var colliders = currentPreview.GetComponentsInChildren<Collider>();
        foreach (var col in colliders)
        {
            col.enabled = false;
        }
        
        currentPreview.tag = "Preview";
    }
    
    void DestroyPreview()
    {
        if (currentPreview != null)
        {
            Destroy(currentPreview);
            currentPreview = null;
        }
    }
    
    void HandleBuildingPreview()
    {
        if (currentPreview == null) return;
        
        Vector3 mousePos = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(mousePos);
        
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, buildableLayer))
        {
            Vector3Int baseCell = mapGrid.WorldToCell(hit.point);
            Vector3Int targetCell = GetValidBuildPosition(baseCell, hit.normal);
            Vector3 cellCenter = mapGrid.GetCellCenterWorld(targetCell);
            
            currentPreview.transform.position = cellCenter;
            
            var blockComponent = blockPrefabs[selectedBlockIndex].GetComponent<IBlock>();
            bool hasResources = HasEnoughResources(blockComponent);
            bool canPlace = !occupiedCells.Contains(targetCell);
            
            SetPreviewMaterial(hasResources, canPlace);
            
            // 클릭으로 건축
            if (Mouse.current.leftButton.wasPressedThisFrame && canPlace && hasResources)
            {
                PlaceBlock(targetCell, blockComponent);
            }
        }
    }
    
    Vector3Int GetValidBuildPosition(Vector3Int baseCell, Vector3 hitNormal)
    {
        // 히트 노멀을 기반으로 인접한 셀 찾기
        Vector3Int adjacentCell = baseCell;
        
        // 노멀 벡터를 기반으로 방향 결정
        if (Mathf.Abs(hitNormal.x) > 0.5f)
        {
            // X축 방향 (좌우)
            adjacentCell.x += hitNormal.x > 0 ? 1 : -1;
        }
        else if (Mathf.Abs(hitNormal.y) > 0.5f)
        {
            // Y축 방향 (위아래)
            adjacentCell.y += hitNormal.y > 0 ? 1 : -1;
        }
        else if (Mathf.Abs(hitNormal.z) > 0.5f)
        {
            // Z축 방향 (앞뒤)
            adjacentCell.z += hitNormal.z > 0 ? 1 : -1;
        }
        
        // 해당 위치가 비어있으면 그대로 반환
        if (!occupiedCells.Contains(adjacentCell))
        {
            return adjacentCell;
        }
        
        // 만약 해당 위치도 차있다면, 기존 로직처럼 위로 쌓기
        Vector3Int targetCell = adjacentCell;
        while (occupiedCells.Contains(targetCell))
        {
            targetCell.y += 1;
        }
        
        return targetCell;
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
        // 자원 소모
        resources[blockData.BlockName] -= blockData.ResourceCost;
        dailyUsage[blockData.BlockName] += blockData.ResourceCost;
        
        // 블록 생성
        Vector3 worldPos = mapGrid.GetCellCenterWorld(position);
        GameObject newBlock = Instantiate(blockPrefabs[selectedBlockIndex], worldPos, Quaternion.identity);
        
        // 블록 초기화
        var blockComponent = newBlock.GetComponent<IBlock>();
        blockComponent.OnPlaced(position);
        
        // 셀 점유 표시
        occupiedCells.Add(position);
        
        UpdateResourceDisplay();
    }
    
    void DailyResourceRefill()
    {
        foreach (var usage in dailyUsage.ToList())
        {
            if (usage.Value > 0)
            {
                int refillAmount = Mathf.CeilToInt(usage.Value * 1.3f);
                
                // 랜덤하게 자원 타입들에 분배
                var availableTypes = resources.Keys.ToList();
                
                for (int i = 0; i < refillAmount; i++)
                {
                    string randomType = availableTypes[Random.Range(0, availableTypes.Count)];
                    resources[randomType]++;
                }
            }
        }
        
        // 일일 사용량 초기화
        foreach (var key in dailyUsage.Keys.ToList())
        {
            dailyUsage[key] = 0;
        }
        
        UpdateResourceDisplay();
        Debug.Log("📦 일일 자원 보충 완료!");
    }
    
    void UpdateResourceDisplay()
    {
        if (resourceDisplayText == null) return;
        
        string displayText = "=== 보유 자원 ===\n";
        
        foreach (var resource in resources)
        {
            displayText += $"{resource.Key}: {resource.Value}개\n";
        }
        
        if (isBuildingMode && selectedBlockIndex < blockPrefabs.Length)
        {
            var selectedBlock = blockPrefabs[selectedBlockIndex].GetComponent<IBlock>();
            displayText += $"\n선택된 블록: {selectedBlock.BlockName}";
            displayText += $"\n필요 자원: {selectedBlock.ResourceCost}개";
            
            bool canBuild = HasEnoughResources(selectedBlock);
            displayText += canBuild ? "\n✅ 건축 가능" : "\n❌ 자원 부족";
        }
        
        resourceDisplayText.text = displayText;
    }
    
    // 블록 제거 시 셀 해제
    public void RemoveBlock(Vector3Int position)
    {
        occupiedCells.Remove(position);
    }
    
    // 모든 블록에 재해 피해 적용
    public void ApplyDisasterToAllBlocks(DisasterType disaster, float baseDamage = 25f)
    {
        var allBlocks = FindObjectsOfType<MonoBehaviour>().OfType<IBlock>();
        
        foreach (var block in allBlocks)
        {
            block.TakeDamage(disaster, baseDamage);
        }
        
        Debug.Log($"🌪️ {GetKoreanDisasterName(disaster)} 발생! 모든 블록이 피해를 받았습니다.");
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
    
    // 테스트용 재해 발생 메서드들
    [ContextMenu("산성비 발생")]
    public void TriggerAcidRain() => ApplyDisasterToAllBlocks(DisasterType.AcidRain);
    
    [ContextMenu("지진 발생")]
    public void TriggerEarthquake() => ApplyDisasterToAllBlocks(DisasterType.Earthquake);
    
    [ContextMenu("벼락 발생")]
    public void TriggerLightning() => ApplyDisasterToAllBlocks(DisasterType.Lightning);
}

    
}