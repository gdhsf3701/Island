using System.Collections.Generic;
using UnityEngine;

namespace _00.Work._01.Scripts.Block
{
    public class StoneBlock : BaseBlock
    {
        public override Dictionary<DisasterType, ResistanceLevel> DisasterResistance => new()
        {
            {DisasterType.AcidRain, ResistanceLevel.Normal},
            {DisasterType.StrongWind, ResistanceLevel.Weak},
            {DisasterType.Earthquake, ResistanceLevel.Strong},
            {DisasterType.Wildfire, ResistanceLevel.Normal},
            {DisasterType.Tsunami, ResistanceLevel.Normal},
            {DisasterType.Sandstorm, ResistanceLevel.Normal},
            {DisasterType.Lightning, ResistanceLevel.Normal}
        };
    }
}