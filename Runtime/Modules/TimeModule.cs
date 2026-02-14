using System;
using Weft.Runtime.Binding;
using Weft.Runtime.Scheduling;
using Weft.Runtime.Services;
using Weft.Unity.Engine;
using Weft.Unity.Services;

namespace Weft.Runtime.Modules {
    /// <summary>
    /// Provides sleep(seconds) for pausing script execution.
    /// </summary>
    public sealed class TimeModule : IWeftModule {
        public string Id => "time";
        public LanguageFeatures ParserFeatures => LanguageFeatures.None;

        public void Register(IBindingRegistrar registrar) {
            registrar.Bind("sleep", (ctx, args) => {
                if (args.Length < 1)
                    throw new ArgumentException("sleep() requires 1 argument.");
                return new YieldForSeconds(Convert.ToDouble(args[0]));
            });
        }

        public void Setup(ScriptContext ctx) {
            if (ctx.Services is not WeftServiceProvider provider) return;

            var time = new WeftUnityTimeService();
            provider.Add(time);
            provider.Add<ITimeService>(time);
        }
    }
}