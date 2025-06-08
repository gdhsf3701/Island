using System.Collections.Generic;
using UnityEngine;

namespace _00.Work._01.Scripts.Block
{
    public class BambooBlock : BaseBlock
    {
        public override Dictionary<DisasterType, ResistanceLevel> DisasterResistance => new()
        {
            {DisasterType.AcidRain, ResistanceLevel.Normal},   // 자연소재라 산성비에 괜찮음
            {DisasterType.StrongWind, ResistanceLevel.Weak},
            {DisasterType.Earthquake, ResistanceLevel.Weak},
            {DisasterType.Wildfire, ResistanceLevel.Weak},
            {DisasterType.Tsunami, ResistanceLevel.Normal},
            {DisasterType.Sandstorm, ResistanceLevel.Normal},
            {DisasterType.Lightning, ResistanceLevel.Normal}
        };
    }
}