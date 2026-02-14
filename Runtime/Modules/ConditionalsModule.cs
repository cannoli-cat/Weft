using Weft.Runtime.Binding;
using Weft.Unity.Engine;

namespace Weft.Runtime.Modules {
    /// <summary>
    /// Enables if / else / ternary syntax.
    /// </summary>
    public sealed class ConditionalsModule : IWeftModule {
        public string Id => "conditionals";
        public LanguageFeatures ParserFeatures => LanguageFeatures.Conditionals;
        public void Register(IBindingRegistrar registrar) { }
        public void Setup(ScriptContext ctx) { }
    }
}