using System;
using UnityEngine;

namespace Plugins.SerializedFinder.RunTime.Serializable
{
    [Serializable]
    public class SerializableType : ISerializationCallbackReceiver, IEquatable<SerializableType>
    {
        [SerializeField]
        private string typeName;

        public Type Type
        {
            get => string.IsNullOrEmpty(typeName) ? null : System.Type.GetType(typeName);
            set => typeName = value?.AssemblyQualifiedName;
        }

        public SerializableType(Type type)
        {
            Type = type;
        }

        public static implicit operator SerializableType(Type type) => new SerializableType(type);
        public static implicit operator Type(SerializableType serializableType) => serializableType?.Type;

        public void OnBeforeSerialize()
        {
            typeName = Type?.AssemblyQualifiedName;
        }

        public void OnAfterDeserialize()
        {
            if (!string.IsNullOrEmpty(typeName))
                Type = System.Type.GetType(typeName);
        }

        public static bool operator ==(SerializableType a, SerializableType b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a is null || b is null) return false;
            return a.Equals(b);
        }

        public static bool operator !=(SerializableType a, SerializableType b)
            => !(a == b);

        public bool Equals(SerializableType other)
        {
            if (other is null) return false;
            if (Type == null && other.Type == null) return true;
            if (Type == null || other.Type == null) return false;
            return Type.AssemblyQualifiedName == other.Type.AssemblyQualifiedName;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as SerializableType);
        }

        public override int GetHashCode()
        {
            return typeName?.GetHashCode() ?? 0;
        }
    }
}
