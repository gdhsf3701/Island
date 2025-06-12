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
        [SerializeField] protected Sprite blockIcon; // 새로 추가
        [SerializeField] protected float maxHealth = 100f;

        protected float currentHealth;
        protected Vector3Int gridPosition;

        // 인터페이스 구현
        public virtual string BlockName => blockName;
        public virtual int ResourceCost => resourceCost;
        public virtual Sprite BlockIcon => blockIcon; // 새로 추가
        public abstract Dictionary<DisasterType, ResistanceLevel> DisasterResistance { get; }

        protected virtual void Awake()
        {
            currentHealth = maxHealth;
        }

        public virtual void OnPlaced(Vector3Int gridPos)
        {
            gridPosition = gridPos;
        }

        public virtual void OnDestroyed()
        {
            var manager = FindObjectOfType<SimpleBlockManager>();
            manager?.RemoveBlock(gridPosition);
            Destroy(gameObject);
        }

        public virtual void TakeDamage(DisasterType disaster, float baseDamage)
        {
            var resistance = DisasterResistance.GetValueOrDefault(disaster, ResistanceLevel.Normal);
        
            float damageMultiplier = resistance switch
            {
                ResistanceLevel.Strong => 0.3f,
                ResistanceLevel.Normal => 1f,
                ResistanceLevel.Weak => 2f,
                _ => 1f
            };
        
            float actualDamage = baseDamage * damageMultiplier;
            currentHealth -= actualDamage;
        
            if (currentHealth <= 0)
            {
                OnDestroyed();
            }
        }
    }
}