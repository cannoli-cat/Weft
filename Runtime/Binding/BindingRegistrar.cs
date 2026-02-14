using System;

namespace Weft.Runtime.Binding {
    public sealed class BindingRegistrar : IBindingRegistrar {
        private readonly WeftRateLimiter limiter = new();
        private readonly WeftLimits weftLimits;

        public BindingRegistrar(WeftLimits weftLimits) => this.weftLimits = weftLimits ?? WeftLimits.Default;

        public void Bind(string name, HostFunc fn, string capability = null,
            double rps = 0, double burst = 0) {
            var useRps = rps > 0 ? rps : weftLimits.DefaultRps;
            var useBurst = burst > 0 ? burst : weftLimits.DefaultBurst;
            
            WeftRegistry.Register(name, (ctx, args) => {
                if (capability != null && (ctx.Caps == null || !ctx.Caps.Has(capability)))
                    throw new Exception($"Missing capability '{capability}' for '{name}'");

                if (useRps > 0 && useBurst > 0 && !limiter.TryConsume(ctx.Pid, name, useRps, useBurst))
                    throw new Exception($"Rate limit exceeded for '{name}'");

                return fn(ctx, args);
            });
        }
    }
}