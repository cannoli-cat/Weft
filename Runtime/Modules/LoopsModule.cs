using Weft.Runtime.Binding;
using Weft.Unity.Engine;

namespace Weft.Runtime.Modules {
    /// <summary>
    /// Enables while / for / do-while / break / continue syntax.
    /// </summary>
    public sealed class LoopsModule : IWeftModule {
        public string Id => "loops";
        public LanguageFeatures ParserFeatures => LanguageFeatures.Loops;
        public void Register(IBindingRegistrar registrar) { }
        public void Setup(ScriptContext ctx) { }
    }
}