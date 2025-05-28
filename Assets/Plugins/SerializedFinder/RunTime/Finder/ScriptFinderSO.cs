using Plugins.SerializedFinder.RunTime.Serializable;
using UnityEngine;

namespace Plugins.SerializedFinder.RunTime.Finder
{
    [CreateAssetMenu(fileName = "ScriptFinder",menuName = "SO/ScriptFinder", order = 0)]
    public class ScriptFinderSO : ScriptableObject
    {
        [Header("MonoBehaviour type to find in the scene")]
        [SerializeField]
        private SerializableType _keyType;

        private MonoBehaviour _target;
        public SerializableType KeyType => _keyType;
        public void SetTarget(MonoBehaviour target)
        {
            _target = target;
        }
        public T GetTarget<T>() where T : MonoBehaviour
        {
            if (_target is T t) return t;
            Debug.LogError($"TypeScriptFinderSO: The assigned target is not of type {{typeof(T).Name}}.");
            return null;
        }
    }
}