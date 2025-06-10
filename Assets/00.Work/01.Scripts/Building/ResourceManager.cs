using System;
using System.Collections.Generic;
using System.Linq;
using _00.Work._01.Scripts.Interface;
using UnityEngine;

namespace _00.Work._01.Scripts
{
    [System.Serializable]
    public class ResourceManager
    {
        public event Action OnResourcesChanged;
        
        private Dictionary<string, int> resources = new Dictionary<string, int>();
        private Dictionary<string, int> dailyUsage = new Dictionary<string, int>();
        
        public ResourceManager(GameObject[] blockPrefabs)
        {
            InitializeResources(blockPrefabs);
        }
        
        void InitializeResources(GameObject[] blockPrefabs)
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
        
        public bool HasEnoughResources(IBlock block)
        {
            return resources.ContainsKey(block.BlockName) && 
                   resources[block.BlockName] >= block.ResourceCost;
        }
        
        public void ConsumeResources(IBlock block)
        {
            if (HasEnoughResources(block))
            {
                resources[block.BlockName] -= block.ResourceCost;
                dailyUsage[block.BlockName] += block.ResourceCost;
                OnResourcesChanged?.Invoke();
            }
        }
        
        public void RefillResources()
        {
            // 사용량에 따른 보상 지급
            foreach (var usage in dailyUsage.ToList())
            {
                if (usage.Value > 0)
                {
                    int refillAmount = Mathf.CeilToInt(usage.Value * 1.3f);
                    resources[usage.Key] += refillAmount;
                }
            }
            
            // 기본 보충
            foreach (var key in resources.Keys.ToList())
            {
                resources[key] += 5;
            }
            
            // 사용량 초기화
            foreach (var key in dailyUsage.Keys.ToList())
            {
                dailyUsage[key] = 0;
            }
            
            OnResourcesChanged?.Invoke();
            Debug.Log("📦 자원 보충 완료!");
        }
        
        public string GetResourceDisplayText()
        {
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
            
            return displayText;
        }
    }
}