using System.Collections.Generic;
using UnityEngine;

namespace _00.Work._01.Scripts.Block
{
    public class FiberglassBlock : BaseBlock
    {
        public override Dictionary<DisasterType, ResistanceLevel> DisasterResistance => new()
        {
            {DisasterType.AcidRain, ResistanceLevel.Strong},   // 화학저항성 강함
            {DisasterType.StrongWind, ResistanceLevel.Normal},
            {DisasterType.Earthquake, ResistanceLevel.Normal},
            {DisasterType.Wildfire, ResistanceLevel.Strong},
            {DisasterType.Tsunami, ResistanceLevel.Weak},
            {DisasterType.Sandstorm, ResistanceLevel.Normal},
            {DisasterType.Lightning, ResistanceLevel.Strong}
        };
    }
}