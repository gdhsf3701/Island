using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using _00.Work._01.Scripts.Interface;

namespace _00.Work._01.Scripts.UI
{
    public class BuildingUIManager : MonoBehaviour
    {
        [Header("UI Panels")]
        [SerializeField] private GameObject buildingPanel;
        [SerializeField] private GameObject resourcePanel;
        [SerializeField] private GameObject notificationPanel;
        
        [Header("Building UI")]
        [SerializeField] private Transform blockButtonContainer;
        [SerializeField] private Button blockButtonPrefab;
        [SerializeField] private TextMeshProUGUI buildingModeText;
        [SerializeField] private Button toggleBuildingModeButton;
        
        [Header("Resource UI")]
        [SerializeField] private TextMeshProUGUI resourceCountText;
        [SerializeField] private Slider resourceSlider;
        
        [Header("Notifications")]
        [SerializeField] private TextMeshProUGUI notificationText;
        [SerializeField] private Image notificationBG;
        
        [Header("Block Selection UI")]
        [SerializeField] private Image selectedBlockIcon;
        [SerializeField] private TextMeshProUGUI selectedBlockName;
        [SerializeField] private TextMeshProUGUI selectedBlockCost;
        
        [Header("Default Icons")]
        [SerializeField] private Sprite defaultBlockIcon;
        
        [Header("UI 설정")]
        [SerializeField] private TMP_FontAsset customFont;
        [SerializeField] private Vector2 buttonSize = new Vector2(120, 80);
        [SerializeField] private float buttonSpacing = 8f;
        
        private SimpleBlockManager blockManager;
        private ResourceManager resourceManager;
        private List<Button> blockButtons = new List<Button>();
        private int currentSelectedIndex = 0;
        
        private Sequence notificationSequence;
        
        // 한국어 블록 이름 매핑
        private Dictionary<string, string> blockNameMapping = new Dictionary<string, string>
        {
            {"Bamboo", "대나무"},
            {"Concrete", "콘크리트"},
            {"Fiberglass", "유리섬유"},
            {"Metal", "금속"},
            {"Mud", "진흙"},
            {"Stone", "돌"},
            {"Tarp", "방수포"}
        };
        
        void Start()
        {
            blockManager = FindObjectOfType<SimpleBlockManager>();
            InitializeUI();
            SetupResourceEvents();
        }
        
        void InitializeUI()
        {
            bool initialBuildingMode = blockManager?.IsBuildingMode ?? false;
            
            // 건축 모드가 OFF면 건축 관련 UI 모두 숨김
            if (buildingPanel != null)
                buildingPanel.SetActive(false);
            
            if (resourcePanel != null)
                resourcePanel.SetActive(true);
            
            if (notificationPanel != null)
                notificationPanel.SetActive(false);
            
            // 개별 건축 UI 요소들도 숨김
            if (blockButtonContainer != null)
                blockButtonContainer.gameObject.SetActive(false);
            
            if (selectedBlockIcon != null)
                selectedBlockIcon.gameObject.SetActive(false);
                
            if (selectedBlockName != null)
                selectedBlockName.gameObject.SetActive(false);
                
            if (selectedBlockCost != null)
                selectedBlockCost.gameObject.SetActive(false);
            
            if (toggleBuildingModeButton != null)
            {
                toggleBuildingModeButton.onClick.RemoveAllListeners();
                toggleBuildingModeButton.onClick.AddListener(ToggleBuildingMode);
            }
            
            CreateBlockButtons();
            UpdateResourceUI();
            UpdateBuildingModeUI();
            ApplyCustomFont();
        }
        
        void ApplyCustomFont()
        {
            if (customFont == null) return;
            
            // 모든 TextMeshProUGUI 컴포넌트에 폰트 적용
            var allTexts = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var text in allTexts)
            {
                text.font = customFont;
            }
        }
        
        void SetupResourceEvents()
        {
            if (blockManager?.ResourceManager != null)
            {
                resourceManager = blockManager.ResourceManager;
                resourceManager.OnResourcesChanged += OnResourcesChanged;
                resourceManager.OnResourcesLow += OnResourcesLow;
                resourceManager.OnResourceAvailabilityChanged += OnResourceAvailabilityChanged;
            }
        }
        
        void CreateBlockButtons()
        {
            if (blockManager?.BlockPrefabs == null || blockButtonContainer == null || blockButtonPrefab == null) 
                return;
            
            ClearBlockButtons();
            
            // GridLayoutGroup 설정 (있다면)
            var gridLayout = blockButtonContainer.GetComponent<GridLayoutGroup>();
            if (gridLayout != null)
            {
                gridLayout.cellSize = buttonSize;
                gridLayout.spacing = new Vector2(buttonSpacing, buttonSpacing);
            }
            
            for (int i = 0; i < blockManager.BlockPrefabs.Length; i++)
            {
                var blockPrefab = blockManager.BlockPrefabs[i];
                if (blockPrefab == null) continue;
                
                var blockData = blockPrefab.GetComponent<IBlock>();
                if (blockData == null) continue;
                
                CreateBlockButton(blockData, i);
            }
            
            if (blockButtons.Count > 0)
                SelectBlock(0);
        }
        
        void ClearBlockButtons()
        {
            foreach (var button in blockButtons)
            {
                if (button != null)
                    DestroyImmediate(button.gameObject);
            }
            blockButtons.Clear();
        }
        
        void CreateBlockButton(IBlock blockData, int index)
        {
            Button buttonObj = Instantiate(blockButtonPrefab, blockButtonContainer);
            
            // 버튼 크기 조정
            var rectTransform = buttonObj.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.sizeDelta = buttonSize;
            }
            
            // 한국어 이름 가져오기
            string koreanName = GetKoreanBlockName(blockData.BlockName);
            
            var buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = $"{koreanName}\n{blockData.ResourceCost}";
                buttonText.fontSize = 14f; // 중간 크기로 설정
                
                if (customFont != null)
                    buttonText.font = customFont;
            }
            
            int buttonIndex = index;
            buttonObj.onClick.RemoveAllListeners();
            buttonObj.onClick.AddListener(() => SelectBlock(buttonIndex));
            
            blockButtons.Add(buttonObj);
        }
        
        string GetKoreanBlockName(string englishName)
        {
            return blockNameMapping.ContainsKey(englishName) ? blockNameMapping[englishName] : englishName;
        }
        
        void SelectBlock(int index)
        {
            if (index < 0 || index >= blockButtons.Count || index >= blockManager.BlockPrefabs.Length) 
                return;
            
            // 이전 선택 해제
            if (currentSelectedIndex < blockButtons.Count && blockButtons[currentSelectedIndex] != null)
            {
                var prevButton = blockButtons[currentSelectedIndex];
                var prevImage = prevButton.GetComponent<Image>();
                if (prevImage != null)
                    prevImage.color = Color.white;
            }
            
            // 새로운 선택
            currentSelectedIndex = index;
            var currentButton = blockButtons[currentSelectedIndex];
            var currentImage = currentButton.GetComponent<Image>();
            if (currentImage != null)
                currentImage.color = Color.yellow;
            
            blockManager.SetSelectedBlockIndex(currentSelectedIndex);
            UpdateSelectedBlockUI();
            
            var blockData = blockManager.BlockPrefabs[index].GetComponent<IBlock>();
            string koreanName = GetKoreanBlockName(blockData.BlockName);
            ShowNotification($"{koreanName} 선택됨", Color.blue);
        }
        
        void UpdateSelectedBlockUI()
        {
            if (currentSelectedIndex >= blockManager.BlockPrefabs.Length || resourceManager == null) 
                return;
            
            var blockData = blockManager.BlockPrefabs[currentSelectedIndex].GetComponent<IBlock>();
            if (blockData == null) return;
            
            string englishName = blockData.BlockName;
            string koreanName = GetKoreanBlockName(englishName);
            int cost = blockData.ResourceCost;
            int available = resourceManager.GetResourceAmount(englishName);
            
            if (selectedBlockName != null)
                selectedBlockName.text = koreanName;
            
            if (selectedBlockCost != null)
            {
                selectedBlockCost.text = $"필요: {cost} (보유: {available})";
                selectedBlockCost.color = available >= cost ? Color.white : Color.red;
            }
            
            if (selectedBlockIcon != null)
            {
                Sprite iconToUse = blockData.BlockIcon != null ? blockData.BlockIcon : defaultBlockIcon;
                selectedBlockIcon.sprite = iconToUse;
            }
            
            UpdateButtonStates();
        }
        
        void UpdateButtonStates()
        {
            if (resourceManager == null) return;
            
            for (int i = 0; i < blockButtons.Count && i < blockManager.BlockPrefabs.Length; i++)
            {
                var blockData = blockManager.BlockPrefabs[i].GetComponent<IBlock>();
                if (blockData == null) continue;
                
                var button = blockButtons[i];
                if (button == null) continue;
                
                int available = resourceManager.GetResourceAmount(blockData.BlockName);
                bool hasEnoughResources = available >= blockData.ResourceCost;
                
                // !! 중요: 자원이 충분해도 건축 모드가 꺼져있으면 버튼 비활성화하지 않기
                // 버튼은 항상 클릭 가능하게 하고, 실제 건축 시점에서 조건 체크
                button.interactable = true;
                
                var buttonImage = button.GetComponent<Image>();
                if (buttonImage != null && i != currentSelectedIndex)
                {
                    // 자원 부족시에만 회색으로 표시 (버튼은 여전히 클릭 가능)
                    buttonImage.color = hasEnoughResources ? Color.white : new Color(1f, 1f, 1f, 0.6f);
                }
            }
        }
        
        void ToggleBuildingMode()
        {
            if (blockManager == null) return;
            
            bool newMode = !blockManager.IsBuildingMode;
            blockManager.SetBuildingMode(newMode);
            
            UpdateBuildingModeUI();
            
            if (newMode)
            {
                ShowNotification("건축 모드 활성화 - 이제 블록을 설치할 수 있습니다!", Color.green);
            }
            else
            {
                ShowNotification("건축 모드 비활성화 - 블록 설치가 불가능합니다", Color.red);
            }
        }
        
        void UpdateBuildingModeUI()
        {
            if (blockManager == null) return;
            
            bool isBuildingMode = blockManager.IsBuildingMode;
            
            if (buildingModeText != null)
            {
                if (isBuildingMode)
                {
                    buildingModeText.text = "건축 모드 ON";
                    buildingModeText.color = Color.green;
                }
                else
                {
                    buildingModeText.text = "건축 모드 OFF";
                    buildingModeText.color = Color.red;
                }
            }
            
            if (buildingPanel != null)
            {
                buildingPanel.SetActive(isBuildingMode);
            }
            else
            {
                if (blockButtonContainer != null)
                    blockButtonContainer.gameObject.SetActive(isBuildingMode);
                
                if (selectedBlockIcon != null)
                    selectedBlockIcon.gameObject.SetActive(isBuildingMode);
                    
                if (selectedBlockName != null)
                    selectedBlockName.gameObject.SetActive(isBuildingMode);
                    
                if (selectedBlockCost != null)
                    selectedBlockCost.gameObject.SetActive(isBuildingMode);
            }
            
            // 버튼 상태도 업데이트
            UpdateButtonStates();
        }
        
        void UpdateResourceUI()
        {
            if (resourceManager == null) return;
            
            int currentResources = resourceManager.GetTotalResources();
            int maxResources = resourceManager.GetTotalMaxResources();
            float ratio = maxResources > 0 ? (float)currentResources / maxResources : 0f;
            
            if (resourceCountText != null)
            {
                resourceCountText.text = $"{currentResources} / {maxResources}";
                
                if (ratio < 0.3f)
                    resourceCountText.color = Color.red;
                else if (ratio < 0.5f)
                    resourceCountText.color = Color.yellow;
                else
                    resourceCountText.color = Color.white;
            }
            
            if (resourceSlider != null)
            {
                resourceSlider.value = ratio;
                
                var fillImage = resourceSlider.fillRect?.GetComponent<Image>();
                if (fillImage != null)
                {
                    fillImage.color = ratio < 0.3f ? Color.red : 
                                     ratio < 0.5f ? Color.yellow : Color.green;
                }
            }
        }
        
        void ShowNotification(string message, Color color)
        {
            if (notificationPanel == null || notificationText == null) return;
            
            notificationSequence?.Kill();
            
            notificationText.text = message;
            
            if (notificationBG != null)
                notificationBG.color = color;
            
            notificationPanel.SetActive(true);
            
            notificationPanel.transform.localScale = Vector3.one;
            
            notificationSequence = DOTween.Sequence()
                .AppendInterval(2f)
                .OnComplete(() => {
                    if (notificationPanel != null)
                        notificationPanel.SetActive(false);
                });
        }
        
        // 이벤트 핸들러들
        void OnResourcesChanged(int current, int max)
        {
            UpdateResourceUI();
            UpdateSelectedBlockUI();
        }
        
        void OnResourcesLow(string blockName)
        {
            string koreanName = GetKoreanBlockName(blockName);
            ShowNotification($"⚠️ {koreanName} 자원 부족!", Color.red);
        }
        
        void OnResourceAvailabilityChanged(string blockName, bool available)
        {
            UpdateButtonStates();
        }
        
        public void OnBuildingSuccess(string blockName, Vector3Int position)
        {
            string koreanName = GetKoreanBlockName(blockName);
            ShowNotification($"✅ {koreanName} 건축 완료!", Color.green);
            UpdateResourceUI();
        }
        
        public void OnBuildingFailed(string reason)
        {
            // 실패 이유별 상세 메시지
            string detailedMessage = reason;
            switch (reason.ToLower())
            {
                case "building mode off":
                case "건축 모드 꺼짐":
                    detailedMessage = "❌ 건축 실패: Build 버튼을 눌러 건축 모드를 켜세요!";
                    break;
                case "insufficient resources":
                case "자원 부족":
                    detailedMessage = "❌ 건축 실패: 자원이 부족합니다";
                    break;
                case "invalid position":
                case "잘못된 위치":
                    detailedMessage = "❌ 건축 실패: 이 위치에는 설치할 수 없습니다";
                    break;
                default:
                    detailedMessage = $"❌ 건축 실패: {reason}";
                    break;
            }
            
            ShowNotification(detailedMessage, Color.red);
        }
        
        void Update()
        {
            HandleKeyboardInput();
        }
        
        void HandleKeyboardInput()
        {
            for (int i = 0; i < Mathf.Min(9, blockButtons.Count); i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    SelectBlock(i);
                }
            }
            
            if (Input.GetKeyDown(KeyCode.B))
            {
                ToggleBuildingMode();
            }
        }
        
        void OnDestroy()
        {
            if (resourceManager != null)
            {
                resourceManager.OnResourcesChanged -= OnResourcesChanged;
                resourceManager.OnResourcesLow -= OnResourcesLow;
                resourceManager.OnResourceAvailabilityChanged -= OnResourceAvailabilityChanged;
            }
            
            notificationSequence?.Kill();
            DOTween.KillAll();
        }
    }
}