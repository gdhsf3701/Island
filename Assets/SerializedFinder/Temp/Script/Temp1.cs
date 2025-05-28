using Plugins.SerializedFinder.RunTime.Dependencies;
using UnityEngine;

namespace SerializedFinder.Temp.Script
{
    public class Temp1 : MonoBehaviour , IDependencyProvider
    {
        [Provide]
        public Temp1 ProvideTemp1() => this;
        public void Test()
        {
            Debug.Log("Test");
        }
    }
}
