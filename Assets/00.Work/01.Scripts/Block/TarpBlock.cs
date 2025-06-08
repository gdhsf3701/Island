using System.Collections.Generic;
using UnityEngine;

namespace _00.Work._01.Scripts.Block
{
    public class TarpBlock : BaseBlock
    {
        public override Dictionary<DisasterType, ResistanceLevel> DisasterResistance => new()
        {
            {DisasterType.AcidRain, ResistanceLevel.Strong},   // 방수 특성으로 산성비 저항
            {DisasterType.StrongWind, ResistanceLevel.Normal},
            {DisasterType.Earthquake, ResistanceLevel.Normal},
            {DisasterType.Wildfire, ResistanceLevel.Weak},
            {DisasterType.Tsunami, ResistanceLevel.Normal},
            {DisasterType.Sandstorm, ResistanceLevel.Normal},
            {DisasterType.Lightning, ResistanceLevel.Normal}
        };
    }
}