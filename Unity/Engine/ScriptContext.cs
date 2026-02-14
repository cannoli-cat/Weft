using System;
using UnityEngine;
using Weft.Runtime;

namespace Weft.Unity.Engine {
    public class ScriptContext {
        public GameObject gameObject { get; set; }
        public Transform transform => gameObject.transform;
        public WeftCapabilities Caps { get; private set; } = WeftCapabilities.None();
        public int Pid { get; set; }
        public IServiceProvider Services { get; set; }

        public T Resolve<T>() => (T)Services?.GetService(typeof(T));
        
        public ScriptContext(WeftCapabilities caps) {
            Caps = caps ?? WeftCapabilities.None();
        }
    }
}
