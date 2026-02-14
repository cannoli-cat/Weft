using System;
using System.Collections.Generic;
using UnityEngine;
using Weft.Runtime;
using Weft.Runtime.Services;

namespace Weft.Unity.Engine {
    public class ScriptContext {
        public GameObject GameObject { get; set; }
        public Transform Transform => GameObject.transform;
        public WeftCapabilities Caps { get; private set; }
        public int Pid { get; set; }
        public WeftServiceProvider Services { get; set; }

        private readonly Dictionary<Type, object> serviceCache = new();

        public T Resolve<T>() {
            var type = typeof(T);

            if (serviceCache.TryGetValue(type, out var cached)) 
                return (T)cached;
            
            var service = (T)Services?.GetService(type);
            if (service != null)
                serviceCache[type] = service;
            
            return service;
        }
        
        /// <summary>
        /// Clears the service cache. Call this if you dynamically add/remove services.
        /// </summary>
        public void ClearServiceCache() => serviceCache.Clear();
        
        public ScriptContext(WeftCapabilities caps) {
            Caps = caps ?? WeftCapabilities.None();
        }
    }
}
