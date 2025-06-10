using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _00.Work._01.Scripts
{
    [System.Serializable]
    public class BuildingInputHandler
    {
        public event Action OnBuildingModeToggled;
        public event Action<int> OnBlockSelected;
        public event Action OnBuildingAttempted;
        
        private Vector2 mousePosition;
        
        public void HandleInput(bool isBuildingMode, int blockCount)
        {
            UpdateMousePosition();
            HandleModeToggle();
            HandleBlockSelection(blockCount);
            HandleScrollSelection(isBuildingMode, blockCount);
            HandleBuildingInput(isBuildingMode);
        }
        
        void UpdateMousePosition()
        {
            if (Mouse.current != null)
            {
                mousePosition = Mouse.current.position.ReadValue();
            }
        }
        
        void HandleModeToggle()
        {
            if (Keyboard.current.bKey.wasPressedThisFrame)
            {
                OnBuildingModeToggled?.Invoke();
            }
        }
        
        void HandleBlockSelection(int blockCount)
        {
            for (int i = 0; i < blockCount && i < 9; i++)
            {
                if (Keyboard.current[(Key)(Key.Digit1 + i)].wasPressedThisFrame)
                {
                    OnBlockSelected?.Invoke(i);
                }
            }
        }
        
        void HandleScrollSelection(bool isBuildingMode, int blockCount)
        {
            if (!isBuildingMode || Mouse.current == null) return;
            
            float scrollValue = Mouse.current.scroll.ReadValue().y;
            if (scrollValue != 0)
            {
                int direction = scrollValue > 0 ? -1 : 1;
                // 현재 선택된 인덱스를 전달받아야 하므로 이벤트로 처리
                // 또는 현재 인덱스를 저장하여 관리
            }
        }
        
        void HandleBuildingInput(bool isBuildingMode)
        {
            if (isBuildingMode && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                OnBuildingAttempted?.Invoke();
            }
        }
        
        public Vector2 GetMousePosition()
        {
            return mousePosition;
        }
    }
}