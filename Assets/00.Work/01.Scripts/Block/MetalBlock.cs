using System.Collections.Generic;
using UnityEngine;

namespace _00.Work._01.Scripts.Block
{
    public class MetalBlock : BaseBlock
    {
        public override Dictionary<DisasterType, ResistanceLevel> DisasterResistance => new()
        {
            {DisasterType.AcidRain, ResistanceLevel.Weak},    // 산성비에 부식됨
            {DisasterType.StrongWind, ResistanceLevel.Strong},
            {DisasterType.Earthquake, ResistanceLevel.Normal},
            {DisasterType.Wildfire, ResistanceLevel.Normal},
            {DisasterType.Tsunami, ResistanceLevel.Normal},
            {DisasterType.Sandstorm, ResistanceLevel.Strong},
            {DisasterType.Lightning, ResistanceLevel.Weak}
        };
    }
}