using System.Collections.Generic;
using UnityEngine;

namespace _00.Work._01.Scripts.Interface
{
    public interface IBlock
    {
        string BlockName { get; }
        int ResourceCost { get; }
        Sprite BlockIcon { get; } // 새로 추가
        Dictionary<DisasterType, ResistanceLevel> DisasterResistance { get; }
        
        void OnPlaced(Vector3Int gridPos);
        void OnDestroyed();
        void TakeDamage(DisasterType disaster, float baseDamage);
    }
}