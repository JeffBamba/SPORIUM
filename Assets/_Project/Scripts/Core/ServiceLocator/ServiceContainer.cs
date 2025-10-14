using System;
using System.Collections.Generic;

using UnityEngine;

namespace  _Project.Sporae.Core
{
    public class ServiceContainer : MonoBehaviour
    {
        private static ServiceContainer _globalInstance = null;
        private static ServiceContainer _sceneInstance = null;

        public static ServiceContainer Instance => _sceneInstance;
        private Dictionary<Type, object> _services = new();
        
        public event Action<object> OnServiceRegistered;
        
        public static void Init()
        {
            if (!_sceneInstance) 
                // Scene Container
                CreateSceneContainer();
            
            if (_globalInstance)
                return;

            // Global Container
            var globalInstance = new GameObject("GlobalServicesContainer");
            _globalInstance = globalInstance.AddComponent<ServiceContainer>();
            DontDestroyOnLoad(_globalInstance);
        }

        private static void CreateSceneContainer()
        {
            if (_sceneInstance)
                return;

            var sceneInstance = new GameObject("SceneServicesContainer");
            _sceneInstance = sceneInstance.AddComponent<ServiceContainer>();
        }

        private void OnDestroy()
        {
            _sceneInstance = null;
        }

        public void RegisterGlobal<T>(T service)
        {
            _globalInstance._services.Add(typeof(T), service);
            
#if UNITY_EDITOR
            Debug.Log($"Register new global service: {typeof(T)}");
#endif
            
            OnServiceRegistered?.Invoke(service);
        }

        public void Register<T>(T service) 
        {
            _services.Add(typeof(T), service);
            
#if UNITY_EDITOR
            Debug.Log($"Register new local service: {typeof(T)}");
#endif
            
            OnServiceRegistered?.Invoke(service);
        }

        public bool ContainsGlobal(Type type) => _globalInstance._services.ContainsKey(type);
        public bool Contains(Type type) => _services.ContainsKey(type);
        public T Get<T>() => (T)Get(typeof(T));
        public object Get(Type type) => Contains(type) ? _services[type] : _globalInstance._services[type];
    }
}