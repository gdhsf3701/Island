using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AYellowpaper.SerializedCollections;
using Plugins.SerializedFinder.RunTime.Serializable;
using UnityEngine;

namespace Plugins.SerializedFinder.RunTime.Dependencies
{
    [DefaultExecutionOrder(-10)]
    public class Injector : MonoBehaviour
    {
        private const BindingFlags _bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private readonly Dictionary<Type, object> _registry = new Dictionary<Type, object>();

        private void Awake()
        {
            IEnumerable<IDependencyProvider> providers = GetMonoBehaviours().OfType<IDependencyProvider>();
            foreach (var provider in providers)
            {
                RegisterProvider(provider);
            }

            IEnumerable<MonoBehaviour> injectables = GetMonoBehaviours().Where(IsInjectable);
            foreach (var injectable in injectables)
            {
                Inject(injectable);
            }
        }

        private void Inject(MonoBehaviour injectableMono)
        {
            Type type = injectableMono.GetType();
            
            IEnumerable<FieldInfo> injectableFields = type.GetFields(_bindingFlags)
                .Where(f => Attribute.IsDefined(f, typeof(InjectAttribute)));

            foreach (var field in injectableFields)
            {
                var fieldType = field.FieldType;

                if (fieldType.IsGenericType
                    && fieldType.GetGenericTypeDefinition() == typeof(SerializedDictionary<,>)
                    && fieldType.GetGenericArguments()[0] == typeof(SerializableType)
                    && fieldType.GetGenericArguments()[1] == typeof(MonoBehaviour))
                {
                    var dict = (SerializedDictionary<SerializableType, MonoBehaviour>)field.GetValue(injectableMono);
                    foreach (var key in dict.Keys.ToList())
                    {
                        Type scriptType = key;
                        var instance = Resolve(scriptType);
                        Debug.Assert(instance != null, $"Inject instance not found for {scriptType.Name}");
                        dict[key] = (MonoBehaviour)instance;
                    }
                    field.SetValue(injectableMono, dict);
                    continue;
                }

                var resolved = Resolve(fieldType);
                Debug.Assert(resolved != null, $"Inject instance not found for {fieldType.Name}");
                field.SetValue(injectableMono, resolved);
            }

            IEnumerable<MethodInfo> injectableMethods = type.GetMethods(_bindingFlags)
                .Where(m => Attribute.IsDefined(m, typeof(InjectAttribute)));

            foreach (var method in injectableMethods)
            {
                var paramTypes = method.GetParameters().Select(p => p.ParameterType).ToArray();
                var paramValues = paramTypes.Select(Resolve).ToArray();
                method.Invoke(injectableMono, paramValues);
            }
        }

        private object Resolve(Type type)
        {
            _registry.TryGetValue(type, out object instance);
            return instance;
        }

        private bool IsInjectable(MonoBehaviour mono)
        {
            var members = mono.GetType().GetMembers(_bindingFlags);
            return members.Any(member => Attribute.IsDefined(member, typeof(InjectAttribute)));
        }

        private void RegisterProvider(IDependencyProvider provider)
        {
            if (Attribute.IsDefined(provider.GetType(), typeof(ProvideAttribute)))
            {
                _registry[provider.GetType()] = provider;
                return;
            }

            foreach (MethodInfo method in provider.GetType().GetMethods(_bindingFlags))
            {
                if (!Attribute.IsDefined(method, typeof(ProvideAttribute))) continue;
                var returnType = method.ReturnType;
                var instance = method.Invoke(provider, null);
                Debug.Assert(instance != null, $"Provided method {method.Name} returned null.");
                _registry[returnType] = instance;
            }
        }

        private IEnumerable<MonoBehaviour> GetMonoBehaviours()
        {
            return FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        }
    }
}
