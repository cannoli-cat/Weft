using System;
using System.Collections.Generic;
using Weft.Language.Lexing;
using Weft.Language.Parsing;
using Weft.Runtime.Binding;
using Weft.Runtime.Scheduling;
using Weft.Runtime.Services;
using Weft.Unity.Engine;
using Weft.Unity.Services;

namespace Weft.Runtime.Modules {
    /// <summary>
    /// Provides spawn(src), kill(pid), ps(), await_pid(pid).
    /// </summary>
    public sealed class ProcessModule : IWeftModule {
        public string Id => "proc";
        public LanguageFeatures ParserFeatures => LanguageFeatures.None;

        public void Register(IBindingRegistrar registrar) {
            registrar.Bind("spawn", (ctx, args) => {
                if (args.Length < 1)
                    throw new ArgumentException("spawn() requires 1 argument (source string).");

                var source = args[0]?.ToString();
                var engine = WeftEngine.Instance;
                if (engine == null) throw new InvalidOperationException("WeftEngine not available.");

                var lex = Lexer.Tokenize(source);
                if (lex.HasError) throw new Exception($"spawn lex error: {lex.Error}");

                var parse = new Parser(engine.Options.Features).Parse(lex.Tokens);
                if (parse.HasError) throw new Exception($"spawn parse error: {parse.Error}");

                // inherit the caller's console so output goes to the same place
                WeftConsoleService console = null;
                if (ctx.Services is WeftServiceProvider sp)
                    sp.TryGet(out console);

                var time = new WeftUnityTimeService();
                var childServices = new WeftServiceProvider()
                    .Add(console ?? new WeftConsoleService((m, _) => UnityEngine.Debug.Log(m)))
                    .Add(time)
                    .Add<ITimeService>(time);

                var childCtx = new ScriptContext(engine.Options.Capabilities) {
                    Services = childServices
                };

                var pid = engine.Spawn(parse.Nodes, childCtx);
                return (double)pid;
            });

            registrar.Bind("kill", (_, args) => {
                if (args.Length < 1) throw new ArgumentException("kill() requires 1 argument (pid).");
                var pid = (int)Convert.ToDouble(args[0]);
                return WeftEngine.Instance?.Kill(pid) ?? false;
            });

            registrar.Bind("ps", (_, _) => {
                var list = WeftEngine.Instance?.Ps();
                var result = new List<Dictionary<string, object>>();
                if (list != null) {
                    foreach (var p in list)
                        result.Add(new Dictionary<string, object> {
                            ["pid"] = p.Pid,
                            ["pc"] = p.PC,
                            ["done"] = p.Completed
                        });
                }
                return result;
            });

            registrar.Bind("await_pid", (_, args) => {
                if (args.Length < 1) throw new ArgumentException("await_pid() requires 1 argument (pid).");
                return new YieldForProcess((int)Convert.ToDouble(args[0]));
            });
        }

        public void Setup(ScriptContext ctx) { }
    }
}
