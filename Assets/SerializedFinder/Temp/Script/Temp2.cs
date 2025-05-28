using Plugins.SerializedFinder.RunTime.Finder;
using UnityEngine;

namespace SerializedFinder.Temp.Script
{
    public class Temp2 : MonoBehaviour
    {
        [SerializeField] private ScriptFinderSO temp1Finder;
        
        private void Awake()
        {
            temp1Finder.GetTarget<Temp1>().Test();
        }
    }
}
