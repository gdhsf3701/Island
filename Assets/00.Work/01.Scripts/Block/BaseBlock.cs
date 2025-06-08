using _00.Work._01.Scripts.Interface;
using UnityEngine;
using System.Collections.Generic;
namespace _00.Work._01.Scripts.Block
{
    
    public abstract class BaseBlock : MonoBehaviour, IBlock
    {
        [Header("Block Info")]
        [SerializeField] protected string blockName;
        [SerializeField] protected int resourceCost = 1;
        [SerializeField] protected float maxHealth = 100f;
    
        protected float currentHealth;
        protected Vector3Int gridPosition;
    
        // 인터페이스 구현
        public virtual string BlockName => blockName;
        public virtual int ResourceCost => resourceCost;
        public abstract Dictionary<DisasterType, ResistanceLevel> DisasterResistance { get; }
    
        protected virtual void Awake()
        {
            currentHealth = maxHealth;
        }
    
        public virtual void OnPlaced(Vector3Int gridPos)
        {
            gridPosition = gridPos;
            Debug.Log($"{BlockName} 블록이 {gridPos}에 설치되었습니다.");
        }
    
        public virtual void OnDestroyed()
        {
            var manager = FindObjectOfType<SimpleBlockManager>();
            manager?.RemoveBlock(gridPosition);
            Debug.Log($"{BlockName} 블록이 파괴되었습니다!");
            Destroy(gameObject);
        }
    
        public virtual void TakeDamage(DisasterType disaster, float baseDamage)
        {
            var resistance = DisasterResistance.GetValueOrDefault(disaster, ResistanceLevel.Normal);
        
            float damageMultiplier = resistance switch
            {
                ResistanceLevel.Strong => 0.3f,  // 70% 데미지 감소
                ResistanceLevel.Normal => 1f,    // 기본 데미지
                ResistanceLevel.Weak => 2f,      // 2배 데미지
                _ => 1f
            };
        
            float actualDamage = baseDamage * damageMultiplier;
            currentHealth -= actualDamage;
        
            Debug.Log($"{BlockName}이(가) {disaster}로 {actualDamage} 피해! 체력: {currentHealth}/{maxHealth}");
        
            if (currentHealth <= 0)
            {
                OnDestroyed();
            }
        }
    }

}