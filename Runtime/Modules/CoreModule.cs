using System.Collections.Generic;
using System.Linq;
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
            registrar.Bind("assert", (_, args) => {
                var condition = args.Length > 0 && IsTruthy(args[0]);
                if (!condition) {
                    var msg = args.Length > 1 ? args[1]?.ToString() ?? "Assertion failed" : "Assertion failed";
                    throw new System.Exception(msg);
                }
                return null;
            });
            
            registrar.Bind("print", (ctx, args) => {
                var console = ctx.Resolve<WeftConsoleService>();
                console?.Print(args.Length > 0 ? FormatValue(args[0]) : "");
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
                return FormatValue(args[0]);
            });
            
            registrar.Bind("type", (_, args) => {
                if (args.Length != 1) throw new System.Exception("type() requires exactly one argument.");
                return args[0] switch {
                    null => "null",
                    double => "number",
                    string => "string",
                    bool => "bool",
                    List<object> => "array",
                    Dictionary<string, object> => "object",
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
        
        private static string FormatValue(object value, HashSet<object> visited = null) {
            if (value == null) return "null";
            if (value is bool b) return b ? "true" : "false";
            if (value is double d) return d.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (value is string s) return s;

            visited ??= new HashSet<object>();
            
            if (!visited.Add(value)) {
                return value is List<object> ? "[...]" : "{...}";
            }

            try {
                if (value is List<object> list) {
                    var elements = list.Select(v => FormatValue(v, visited));
                    return "[" + string.Join(", ", elements) + "]";
                }

                if (value is Dictionary<string, object> dict) {
                    var elements = dict.Select(kv => kv.Key + ": " + FormatValue(kv.Value, visited));
                    return "{" + string.Join(", ", elements) + "}";
                }

                return value.ToString();
            } 
            finally {
                visited.Remove(value);
            }
        }
        
        private static bool IsTruthy(object val) => val switch {
            null => false,
            bool b => b,
            double d => d != 0,
            _ => true
        };
    }
}