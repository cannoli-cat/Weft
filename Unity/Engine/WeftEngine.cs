using System.Collections.Generic;
using UnityEngine;
using Weft.Language.AST;
using Weft.Runtime.Binding;
using Weft.Runtime.Scheduling;
using Weft.Runtime.Services;

namespace Weft.Unity.Engine {
    public class WeftEngine : MonoBehaviour {
        public static WeftEngine Instance { get; private set; }

        [SerializeField] private WeftOptionsSO optionsAsset;
        [SerializeField] private bool rebindOnAwake = true;

        public WeftOptions Options { get; private set; }

        private readonly WeftScheduler scheduler = new();

        private void Awake() {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            Options = optionsAsset != null
                ? optionsAsset.ToRuntime()
                : WeftOptions.Default();

            if (rebindOnAwake) Rebind();
        }

        private void Update() {
            scheduler?.Tick();
        }

        /// <summary>
        /// Clears the registry and re-registers all bindings from the
        /// current set of enabled modules. No assembly scanning.
        /// </summary>
        public void Rebind() {
            WeftRegistry.Clear();
            var registrar = new BindingRegistrar(Options.WeftLimits);
            foreach (var module in Options.Modules)
                module.Register(registrar);

            Debug.Log($"[Weft] Bound: {string.Join(", ", WeftRegistry.Names())}");
        }

        public int Spawn(List<AstNode> program, ScriptContext ctx) {
            ctx ??= new ScriptContext(Options.Capabilities);

            var provider = ctx.Services as WeftServiceProvider ?? new WeftServiceProvider();
            ctx.Services = provider;

            foreach (var module in Options.Modules)
                module.Setup(ctx);

            return scheduler.Spawn(program, ctx, Options.WeftLimits.GasPerStep);
        }

        public void SetOptions(WeftOptions options) {
            Options = options;
            Rebind();
        }

        public bool Kill(int pid) => scheduler.Kill(pid);
        public IReadOnlyList<WeftProcess> Ps() => scheduler.Ps();
    }
}
