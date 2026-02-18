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
            
            registrar.Bind("number", (_, args) => {
                if (args.Length != 1) throw new System.Exception("number() requires exactly one argument.");
                var str = args[0]?.ToString();
                
                if (double.TryParse(str, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var result)) 
                    return result;
                
                throw new System.Exception($"Cannot convert '{str}' to a number.");
            });
            
            registrar.Bind("string", (_, args) => {
                if (args.Length != 1) throw new System.Exception("string() requires exactly one argument.");
                return args[0]?.ToString() ?? "null";
            });
            
            registrar.Bind("type", (_, args) => {
                if (args.Length != 1) throw new System.Exception("type() requires exactly one argument.");
                return args[0] switch {
                    null => "null",
                    double => "number",
                    string => "string",
                    bool => "bool",
                    System.Collections.Generic.List<object> => "array",
                    System.Collections.Generic.Dictionary<string, object> => "object",
                    _ => "unknown"
                };
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