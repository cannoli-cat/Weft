using System.Collections.Generic;
using System.Linq;
using Weft.Runtime;
using Weft.Runtime.Modules;

namespace Weft.Unity.Engine {
    public sealed class WeftOptions {
        public IReadOnlyList<IWeftModule> Modules { get; }
        public LanguageFeatures Features { get; }
        public WeftLimits WeftLimits { get; }

        /// <summary>
        /// Capabilities kept for the binding registrar's per-call checks.
        /// Derived from module IDs: each module ID is itself a capability,
        /// plus every binding a module registers is also a capability.
        /// </summary>
        public WeftCapabilities Capabilities { get; }

        public WeftOptions(IEnumerable<IWeftModule> modules, WeftLimits limits = null,
            WeftCapabilities extraCaps = null) {
            var list = modules.ToList();
            Modules = list;
            WeftLimits = limits ?? WeftLimits.Default;

            // derive parser features from the union of all enabled modules
            var f = LanguageFeatures.None;
            foreach (var m in list) f |= m.ParserFeatures;
            Features = f;

            // derive capabilities from module IDs
            var caps = extraCaps ?? WeftCapabilities.None();
            foreach (var m in list) caps.Add(m.Id);
            Capabilities = caps;
        }

        public static WeftOptions Default() => new(new IWeftModule[] {
            new CoreModule(),
            new ConditionalsModule(),
            new LoopsModule(),
            new AugAssignModule(),
            new TimeModule(),
            new CollectionsModule()
        });

        public WeftOptions With(WeftLimits limits = null,
            IEnumerable<IWeftModule> modules = null,
            WeftCapabilities extraCaps = null)
            => new(modules ?? Modules, limits ?? WeftLimits, extraCaps);
    }
}
