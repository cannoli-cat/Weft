using System;
using System.Collections.Generic;

namespace Weft.Runtime.Services {
    public sealed class WeftServiceProvider : IServiceProvider {
        private readonly Dictionary<Type, object> map = new();
        public object GetService(Type t) => map.GetValueOrDefault(t);
        public WeftServiceProvider Add<T>(T instance) {
            map[typeof(T)] = instance;
            return this;
        }
        
        public bool TryGet<T>(out T instance) {
            if (map.TryGetValue(typeof(T), out var obj) && obj is T t) {
                instance = t;
                return true;
            }
            
            instance = default;
            return false;
        }
    }
}