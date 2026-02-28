using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Weft.Language.Compilation;
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

        private readonly Dictionary<int, CachedScript> scriptCache = new();

        private struct CachedScript {
            public WeftChunk chunk;
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
        /// Compiles and runs a Weft script. Throws on lex/parse/compile errors.
        /// </summary>
        public static int Run(string source, GameObject target = null) {
            var (chunk, error) = Instance.CompileOrCache(source);
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

            return Instance.Spawn(chunk, ctx);
        }

        /// <summary>
        /// Compiles and runs a Weft script with a pre-built context. Throws on errors.
        /// </summary>
        public static int Run(string source, ScriptContext ctx) {
            var (chunk, error) = Instance.CompileOrCache(source);
            if (error != null) throw new System.Exception($"Compilation error: {error}");

            return Instance.Spawn(chunk, ctx);
        }

        /// <summary>
        /// Compiles and runs a Weft script, returning errors instead of throwing.
        /// </summary>
        public static (int pid, string error) TryRun(string source, ScriptContext ctx = null) {
            var (chunk, error) = Instance.CompileOrCache(source);
            if (error != null) return (-1, error);

            return (Instance.Spawn(chunk, ctx), null);
        }

        /// <summary>
        /// Compiles a script and caches the result without spawning a process.
        /// Returns null on success, or an error string.
        /// </summary>
        public static string PreCompile(string source) {
            var (_, error) = Instance.CompileOrCache(source);
            return error;
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
                binding.RegisterAttributes(registrar);

            ClearScriptCache();

            Debug.Log($"[Weft] Bound:\n{string.Join(",\n", WeftRegistry.Names())}");
        }

        public void SetOptions(WeftOptions options) {
            Options = options;
            Rebind();
        }

        public bool Kill(int pid) => scheduler.Kill(pid);
        public IReadOnlyList<IWeftProcess> Ps() => scheduler.Ps();

        /// <summary>
        /// Spawn a bytecode process from a compiled chunk.
        /// </summary>
        public int Spawn(WeftChunk chunk, ScriptContext ctx) {
            ctx = PrepareContext(ctx);

            var gas = ctx.GasOverride ?? Options.WeftLimits.GasPerStep;
            var proc = new WeftBytecodeProcess(chunk, ctx, gas);
            
            return scheduler.Spawn(proc);
        }

        private ScriptContext PrepareContext(ScriptContext ctx) {
            ctx ??= new ScriptContext(Options.Capabilities);

            var provider = ctx.Services ?? new WeftServiceProvider();
            ctx.Services = provider;

            foreach (var module in Options.Modules)
                module.Setup(ctx);

            if (ctx.GameObject) {
                foreach (var binding in ctx.GameObject.GetComponents<WeftBindings>())
                    binding.Setup();
            }

            return ctx;
        }

        private (WeftChunk chunk, string error) CompileOrCache(string source) {
            var key = source.GetHashCode();
    
            if (scriptCache.TryGetValue(key, out var cached)) {
                if (cached.features == Options.Features)
                    return (cached.chunk, null);
                scriptCache.Remove(key);
            }

            var lex = Lexer.Tokenize(source);
            if (lex.HasError)
                return (null, lex.Error.ToString());

            var parse = new Parser(Options.Features).Parse(lex.Tokens);
            if (parse.HasError)
                return (null, parse.Error.ToString());
            
            var compiler = new WeftCompiler();
            var chunk = compiler.Compile(parse.Nodes);

            if (compiler.Error != null)
                return (null, compiler.Error.ToString());

            if (scriptCache.Count >= maxCachedScripts) {
                var firstKey = scriptCache.Keys.First();
                scriptCache.Remove(firstKey);
            }
    
            scriptCache[key] = new CachedScript {
                chunk = chunk,
                features = Options.Features
            };
    
            return (chunk, null);
        }

        public void ClearScriptCache() {
            scriptCache.Clear();
            Debug.Log("[Weft] Script cache cleared");
        }
    }
}