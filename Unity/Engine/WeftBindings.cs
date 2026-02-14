using UnityEngine;
using Weft.Runtime.Binding;

namespace Weft.Unity.Engine {
    public abstract class WeftBindings : MonoBehaviour {
        public abstract void Register(IBindingRegistrar r);
        public virtual void Setup() { }
    }
}