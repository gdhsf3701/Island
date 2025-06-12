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
        public event Action<int, int> OnResourcesChanged; // (current, max)
        public event Action<string> OnResourcesLow; // blockName
        public event Action<string, bool> OnResourceAvailabilityChanged; // (blockName, available)
        
        private Dictionary<string, int> resources = new Dictionary<string, int>();
        private Dictionary<string, int> maxResources = new Dictionary<string, int>();
        private Dictionary<string, int> dailyUsage = new Dictionary<string, int>();
        private Dictionary<string, int> blockCosts = new Dictionary<string, int>();
        
        private const int INITIAL_RESOURCES = 10;
        private const int DAILY_REFILL = 5;
        private const float USAGE_BONUS_MULTIPLIER = 1.5f;
        private const int LOW_RESOURCE_THRESHOLD = 3;
        
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
                    string blockName = block.BlockName;
                    int cost = block.ResourceCost;
                    
                    resources[blockName] = INITIAL_RESOURCES;
                    maxResources[blockName] = INITIAL_RESOURCES * 3; // 최대 3배까지 저장 가능
                    dailyUsage[blockName] = 0;
                    blockCosts[blockName] = cost;
                }
            }
            
            OnResourcesChanged?.Invoke(GetTotalResources(), GetTotalMaxResources());
        }
        
        public bool HasEnoughResources(IBlock block)
        {
            return resources.ContainsKey(block.BlockName) && 
                   resources[block.BlockName] >= block.ResourceCost;
        }
        
        public void ConsumeResources(IBlock block)
        {
            string blockName = block.BlockName;
            int cost = block.ResourceCost;
            
            if (HasEnoughResources(block))
            {
                resources[blockName] -= cost;
                dailyUsage[blockName] += cost;
                
                // 자원 부족 알림
                if (resources[blockName] <= LOW_RESOURCE_THRESHOLD)
                {
                    OnResourcesLow?.Invoke(blockName);
                }
                
                // 자원 가용성 변경 알림
                if (resources[blockName] < cost)
                {
                    OnResourceAvailabilityChanged?.Invoke(blockName, false);
                }
                
                OnResourcesChanged?.Invoke(GetTotalResources(), GetTotalMaxResources());
            }
        }
        
        public void RefillResources()
        {
            foreach (var kvp in resources.ToList())
            {
                string blockName = kvp.Key;
                int currentAmount = kvp.Value;
                int maxAmount = maxResources[blockName];
                
                // 기본 보충
                int refillAmount = DAILY_REFILL;
                
                // 사용량 보너스
                if (dailyUsage.ContainsKey(blockName) && dailyUsage[blockName] > 0)
                {
                    int usageBonus = Mathf.CeilToInt(dailyUsage[blockName] * USAGE_BONUS_MULTIPLIER);
                    refillAmount += usageBonus;
                }
                
                // 최대치 제한
                resources[blockName] = Mathf.Min(currentAmount + refillAmount, maxAmount);
                
                // 자원 가용성 변경 알림
                if (currentAmount < blockCosts[blockName] && resources[blockName] >= blockCosts[blockName])
                {
                    OnResourceAvailabilityChanged?.Invoke(blockName, true);
                }
            }
            
            // 사용량 초기화
            foreach (var key in dailyUsage.Keys.ToList())
            {
                dailyUsage[key] = 0;
            }
            
            OnResourcesChanged?.Invoke(GetTotalResources(), GetTotalMaxResources());
        }
        
        public int GetResourceAmount(string blockName)
        {
            return resources.ContainsKey(blockName) ? resources[blockName] : 0;
        }
        
        public int GetResourceCost(string blockName)
        {
            return blockCosts.ContainsKey(blockName) ? blockCosts[blockName] : 0;
        }
        
        public int GetTotalResources()
        {
            return resources.Sum(kvp => kvp.Value);
        }
        
        public int GetTotalMaxResources()
        {
            return maxResources.Sum(kvp => kvp.Value);
        }
        
        public float GetResourceRatio()
        {
            int total = GetTotalResources();
            int max = GetTotalMaxResources();
            return max > 0 ? (float)total / max : 0f;
        }
        
        public Dictionary<string, ResourceInfo> GetAllResourceInfo()
        {
            var result = new Dictionary<string, ResourceInfo>();
            
            foreach (var kvp in resources)
            {
                string blockName = kvp.Key;
                result[blockName] = new ResourceInfo
                {
                    current = kvp.Value,
                    max = maxResources[blockName],
                    cost = blockCosts[blockName],
                    dailyUsage = dailyUsage[blockName],
                    canAfford = kvp.Value >= blockCosts[blockName]
                };
            }
            
            return result;
        }
        
        public bool IsResourceLow(string blockName)
        {
            return GetResourceAmount(blockName) <= LOW_RESOURCE_THRESHOLD;
        }
        
        public List<string> GetLowResources()
        {
            return resources.Where(kvp => kvp.Value <= LOW_RESOURCE_THRESHOLD).Select(kvp => kvp.Key).ToList();
        }
    }
    
    [System.Serializable]
    public struct ResourceInfo
    {
        public int current;
        public int max;
        public int cost;
        public int dailyUsage;
        public bool canAfford;
        
        public float Ratio => max > 0 ? (float)current / max : 0f;
        public bool IsLow => current <= 3;
    }
}