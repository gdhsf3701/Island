using System.Linq;
using AYellowpaper.SerializedCollections;
using Plugins.SerializedFinder.RunTime.Dependencies;
using Plugins.SerializedFinder.RunTime.Finder;
using Plugins.SerializedFinder.RunTime.Serializable;
using UnityEngine;

namespace Plugins.SerializedFinder.RunTime.Manager
{
    [DefaultExecutionOrder(-1)]
    public class FinderManager : MonoBehaviour
    {
        [SerializeField,Inject] private SerializedDictionary<SerializableType, MonoBehaviour> _components;
        [SerializeField] private ScriptFinderSO[] finders;

        private void Awake()
        {
            foreach (ScriptFinderSO finder in finders)
            {
                SerializableType Key = finder.KeyType;
                MonoBehaviour Value = _components[Key];;
                if (Value == null)
                {
                    Debug.LogWarning($"[FinderManager] Could not find an object in the scene corresponding to the type {finder.KeyType.Type.Name}.");
                    continue;
                }

                finder.SetTarget(Value);
            }
        }
    }
}