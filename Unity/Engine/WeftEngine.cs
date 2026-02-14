using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Weft.Language.AST;
using Weft.Language.Lexing;
using Weft.Language.Parsing;
using Weft.Runtime;
using Weft.Runtime.Binding;
using Weft.Runtime.Scheduling;
using Weft.Runtime.Services;

namespace Weft.Unity.Engine {
    public class WeftEngine : MonoBehaviour {
        public static WeftEngine Instance { get; private set; }

        [SerializeField] private WeftOptionsSO optionsAsset;
        [SerializeField] private bool rebindOnAwake = true;
        [SerializeField] private int maxCachedScripts = 100;

        public WeftOptions Options { get; private set; }
        private readonly WeftScheduler scheduler = new();

        private readonly Dictionary<string, CompiledScript> scriptCache = new();
        
        private struct CompiledScript {
            public List<AstNode> nodes;
            public LanguageFeatures features;
        }

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
        /// Compiles and runs a Weft script. Throws on lex/parse errors.
        /// </summary>
        /// <param name="source">Weft source code to execute.</param>
        /// <param name="target">Optional GameObject whose components and WeftBindings
        /// will be available to the script via service resolution.</param>
        /// <returns>The process ID of the spawned script.</returns>
        public static int Run(string source, GameObject target = null) {
            var nodes = Instance.CompileOrCache(source, out var error);
            if (error != null) throw new System.Exception($"Compilation error: {error}");

            ScriptContext ctx = null;
            if (target) {
                ctx = new ScriptContext(Instance.Options.Capabilities) {
                    GameObject = target
                };
            
                foreach (var module in Instance.Options.Modules)
                    module.Setup(ctx);
            
                foreach (var binding in target.GetComponents<WeftBindings>())
                    binding.Setup();
            }
        
            return Instance.Spawn(nodes, ctx);
        }

        /// <summary>
        /// Compiles and runs a Weft script with a pre-built context. Throws on lex/parse errors.
        /// Use this when you need custom services or capabilities beyond what a target GameObject provides.
        /// </summary>
        /// <param name="source">Weft source code to execute.</param>
        /// <param name="ctx">A pre-configured ScriptContext with services, capabilities, etc.</param>
        /// <returns>The process ID of the spawned script.</returns>
        public static int Run(string source, ScriptContext ctx) {
            var nodes = Instance.CompileOrCache(source, out var error);
            
            if (error != null) 
                throw new System.Exception($"Compilation error: {error}");
            
            return Instance.Spawn(nodes, ctx);
        }

        /// <summary>
        /// Compiles and runs a Weft script, returning errors instead of throwing.
        /// Preferred for player-facing editors where lex/parse errors should be displayed.
        /// </summary>
        /// <param name="source">Weft source code to execute.</param>
        /// <param name="ctx">Optional pre-configured ScriptContext.</param>
        /// <returns>The process ID (or -1 on failure) and an error message if compilation failed.</returns>
        public static (int pid, string error) TryRun(string source, ScriptContext ctx = null) {
            var nodes = Instance.CompileOrCache(source, out var error);
            if (error != null) return (-1, error);
        
            var pid = Instance.Spawn(nodes, ctx);
            return (pid, null);
        }

        /// <summary>
        /// Clears the registry and re-registers all bindings from the
        /// current set of enabled modules. 
        /// </summary>
        public void Rebind() {
            WeftRegistry.Clear();
            var registrar = new BindingRegistrar(Options.WeftLimits);
        
            foreach (var module in Options.Modules)
                module.Register(registrar);
        
            foreach (var binding in FindObjectsByType<WeftBindings>(FindObjectsSortMode.None))
                binding.Register(registrar);

            // clear cache when rebinding since features may have changed
            ClearScriptCache();
        
            Debug.Log($"[Weft] Bound: {string.Join(", ", WeftRegistry.Names())}");
        }

        public int Spawn(List<AstNode> program, ScriptContext ctx) {
            ctx ??= new ScriptContext(Options.Capabilities);

            var provider = ctx.Services ?? new WeftServiceProvider();
            ctx.Services = provider;

            foreach (var module in Options.Modules)
                module.Setup(ctx);

            if (ctx.GameObject) {
                foreach (var binding in ctx.GameObject.GetComponents<WeftBindings>())
                    binding.Setup();
            }

            return scheduler.Spawn(program, ctx, Options.WeftLimits.GasPerStep);
        }

        public void SetOptions(WeftOptions options) {
            Options = options;
            Rebind();
        }

        public bool Kill(int pid) => scheduler.Kill(pid);
        public IReadOnlyList<WeftProcess> Ps() => scheduler.Ps();
        
        /// <summary>
        /// Compiles source to AST nodes, using cache if available.
        /// </summary>
        private List<AstNode> CompileOrCache(string source, out string error) {
            error = null;

            if (scriptCache.TryGetValue(source, out var cached)) {
                if (cached.features == Options.Features) {
                    return cached.nodes;
                }

                scriptCache.Remove(source);
            }
            
            var lex = Lexer.Tokenize(source);
            if (lex.HasError) {
                error = lex.Error;
                return null;
            }
        
            var parse = new Parser(Options.Features).Parse(lex.Tokens);
            if (parse.HasError) {
                error = parse.Error;
                return null;
            }
            
            if (scriptCache.Count >= maxCachedScripts) {
                var firstKey = scriptCache.Keys.First();
                scriptCache.Remove(firstKey);
            }
        
            scriptCache[source] = new CompiledScript {
                nodes = parse.Nodes,
                features = Options.Features
            };
        
            return parse.Nodes;
        }
        
        /// <summary>
        /// Clears the script compilation cache.
        /// Call this when you change WeftOptions or reload modules.
        /// </summary>
        public void ClearScriptCache() {
            scriptCache.Clear();
            Debug.Log("[Weft] Script cache cleared");
        }
    }
}
