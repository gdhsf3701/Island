using System;
using System.Collections.Generic;
using System.Linq;
using _00.Work._01.Scripts.Interface;
using UnityEngine;
using DG.Tweening;

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
        
        // Public 프로퍼티로 접근 제공
        public ResourceManager ResourceManager => resourceManager;
        public GameObject[] BlockPrefabs => blockPrefabs;
        public bool IsBuildingMode => isBuildingMode;
        
        void Start()
        {
            InitializeComponents();
            SetupEventHandlers();
            
            if (buildingCamera == null)
                buildingCamera = Camera.main;
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
        }
        
        void OnDestroy()
        {
            inputHandler.OnBuildingModeToggled -= ToggleBuildingMode;
            inputHandler.OnBlockSelected -= SelectBlock;
            inputHandler.OnBuildingAttempted -= AttemptBuilding;
            
            DayManager.OnNewDayStarted -= resourceManager.RefillResources;
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
                Vector3 hitPoint = hit.Value.point;
                Vector3 hitNormal = hit.Value.normal;
                Vector3 buildPosition = CalculateBuildPosition(hitPoint, hitNormal);
                Vector3Int targetCell = mapGrid.WorldToCell(buildPosition);
                Vector3 cellCenter = mapGrid.GetCellCenterWorld(targetCell);
                
                var selectedBlock = blockPrefabs[selectedBlockIndex].GetComponent<IBlock>();
                bool hasResources = resourceManager.HasEnoughResources(selectedBlock);
                bool canPlace = placementHandler.CanPlaceAt(targetCell, cellCenter, occupiedCells);
                
                previewHandler.ShowPreview(cellCenter, selectedBlock, hasResources, canPlace);
            }
            else
            {
                previewHandler.HidePreview();
            }
        }
        
        Vector3 CalculateBuildPosition(Vector3 hitPoint, Vector3 hitNormal)
        {
            float cellSize = mapGrid.cellSize.x;
            Vector3 offset = hitNormal * (cellSize * 0.5f);
            return hitPoint + offset;
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
            }
        }
        
        void AttemptBuilding()
        {
            if (!isBuildingMode) return;
            
            var mousePos = inputHandler.GetMousePosition();
            var hit = raycastHandler.GetBuildableHit(mousePos);
            
            if (!hit.HasValue) return;
            
            Vector3 hitPoint = hit.Value.point;
            Vector3 hitNormal = hit.Value.normal;
            Vector3 buildPosition = CalculateBuildPosition(hitPoint, hitNormal);
            Vector3Int targetCell = mapGrid.WorldToCell(buildPosition);
            Vector3 cellCenter = mapGrid.GetCellCenterWorld(targetCell);
            
            var selectedBlock = blockPrefabs[selectedBlockIndex].GetComponent<IBlock>();
            
            if (resourceManager.HasEnoughResources(selectedBlock) && 
                placementHandler.CanPlaceAt(targetCell, cellCenter, occupiedCells))
            {
                PlaceBlock(targetCell, selectedBlock);
            }
        }
        
        void PlaceBlock(Vector3Int position, IBlock blockData)
        {
            resourceManager.ConsumeResources(blockData);
            
            Vector3 worldPos = mapGrid.GetCellCenterWorld(position);
            GameObject newBlock = Instantiate(blockPrefabs[selectedBlockIndex], worldPos, Quaternion.identity);
            
            // DOTween 애니메이션 - 설치 시 스케일 애니메이션
            newBlock.transform.localScale = Vector3.zero;
            newBlock.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
            
            var blockComponent = newBlock.GetComponent<IBlock>();
            blockComponent.OnPlaced(position);
            
            occupiedCells.Add(position);
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

        public int SelectedBlockIndex => selectedBlockIndex;

        public void SetBuildingMode(bool buildingMode)
        {
            isBuildingMode = buildingMode;
        }

        public void SetSelectedBlockIndex(int index)
        {
            if (index >= 0 && index < BlockPrefabs.Length)
            {
                selectedBlockIndex = index;
            }
        }

        public GameObject GetSelectedBlockPrefab()
        {
            if (selectedBlockIndex >= 0 && selectedBlockIndex < BlockPrefabs.Length)
            {
                return BlockPrefabs[selectedBlockIndex];
            }
            return null;
        }
    }
}