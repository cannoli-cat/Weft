using System;

namespace Weft.Runtime.Binding {
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class WeftFunctionAttribute : Attribute {
        public string Name { get; }
        public WeftFunctionAttribute(string name) => Name = name;
    }
}