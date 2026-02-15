using Weft.Runtime.Binding;
using Weft.Runtime.Services;
using Weft.Unity.Engine;
using Weft.Unity.Services;

namespace Weft.Runtime.Modules {
    /// <summary>
    /// Variables, print, clear. The minimum viable scripting environment.
    /// </summary>
    public sealed class CoreModule : IWeftModule {
        public string Id => "core";
        public LanguageFeatures ParserFeatures => LanguageFeatures.None;

        public void Register(IBindingRegistrar registrar) {
            registrar.Bind("print", (ctx, args) => {
                var console = ctx.Resolve<WeftConsoleService>();
                console?.Print(args.Length > 0 ? args[0]?.ToString() ?? "null" : "");
                return null;
            });

            registrar.Bind("clear", (ctx, _) => {
                var console = ctx.Resolve<WeftConsoleService>();
                console?.Clear();
                return null;
            });
        }

        public void Setup(ScriptContext ctx) {
            if (ctx.Services is not WeftServiceProvider provider) return;

            // only add a default console if one wasn't already provided
            if (!provider.TryGet<WeftConsoleService>(out _))
                provider.Add(new WeftConsoleService((m, _) => UnityEngine.Debug.Log(m)));
        }
    }
}