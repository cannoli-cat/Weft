using System;
using System.Collections.Generic;
using Weft.Unity.Engine;

namespace Weft.Runtime.Binding {
    public delegate object HostFunc(ScriptContext ctx, object[] args);
    
    public static class WeftRegistry {
        private static readonly Dictionary<string, HostFunc> Map = new(StringComparer.Ordinal);

        public static void Register(string name, HostFunc fn) => Map[name] = fn;
        public static bool TryGet(string name, out HostFunc fn) => Map.TryGetValue(name, out fn);
        public static void Clear() => Map.Clear();

        public static IEnumerable<string> Names() => Map.Keys;
    }
}