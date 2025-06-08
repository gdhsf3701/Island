using System.Collections.Generic;
using UnityEngine;

namespace _00.Work._01.Scripts.Block
{
    public class ConcreteBlock : BaseBlock
    {
        public override Dictionary<DisasterType, ResistanceLevel> DisasterResistance => new()
        {
            {DisasterType.AcidRain, ResistanceLevel.Weak},     // 산성비에 부식됨!
            {DisasterType.StrongWind, ResistanceLevel.Normal},
            {DisasterType.Earthquake, ResistanceLevel.Strong},
            {DisasterType.Wildfire, ResistanceLevel.Normal},
            {DisasterType.Tsunami, ResistanceLevel.Strong},
            {DisasterType.Sandstorm, ResistanceLevel.Normal},
            {DisasterType.Lightning, ResistanceLevel.Normal}
        };
    }
}