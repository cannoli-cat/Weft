using System;
using System.Collections.Generic;

namespace Weft.Runtime {
    public sealed class WeftRateLimiter {
        private readonly Dictionary<(int pid, string key), (double tokens, double last)> b = new();

        public bool TryConsume(int pid, string key, double rps, double burst, double cost = 1) {
            var now = UnityEngine.Time.realtimeSinceStartupAsDouble;
            
            var k = (pid, key);
            var (tokens, last) = b.TryGetValue(k, out var v) ? v : (burst, now);

            tokens = Math.Min(burst, tokens + (now - last) * rps);
            if (tokens < cost) {
                b[k] = (tokens, now);
                return false;
            }

            b[k] = (tokens - cost, now);
            return true;
        }
    }
}