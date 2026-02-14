using System;
using System.Collections.Generic;

namespace Weft.Runtime {
    public sealed class WeftCapabilities {
        private readonly HashSet<string> caps;

        private WeftCapabilities(IEnumerable<string> caps) => this.caps = new HashSet<string>(caps ?? Array.Empty<string>(), StringComparer.Ordinal);

        public bool Has(string cap) => cap == null || caps.Contains(cap);

        public WeftCapabilities Add(string cap) {
            caps.Add(cap);
            return this;
        }

        public WeftCapabilities Remove(string cap) {
            caps.Remove(cap);
            return this;
        }
        
        public static WeftCapabilities None() => new(Array.Empty<string>());
        public static WeftCapabilities Allow(params string[] caps) => new(caps ?? Array.Empty<string>());
    }
}