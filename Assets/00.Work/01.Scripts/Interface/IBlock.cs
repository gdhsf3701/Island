namespace _00.Work._01.Scripts.Interface
{
    
    using UnityEngine;
    using UnityEngine.InputSystem;
    using System.Collections.Generic;
    using System.Linq;
    public interface IBlock
    {
        string BlockName { get; }
        int ResourceCost { get; }
        Dictionary<DisasterType, ResistanceLevel> DisasterResistance { get; }
        void OnPlaced(Vector3Int gridPosition);
        void OnDestroyed();
        void TakeDamage(DisasterType disaster, float baseDamage);
    }

}