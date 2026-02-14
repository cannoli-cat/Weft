using Weft.Runtime.Binding;
using Weft.Unity.Engine;

namespace Weft.Runtime.Modules {
    /// <summary>
    /// Enables augmented assignment operators: += -= *= /= %=
    /// </summary>
    public sealed class AugAssignModule : IWeftModule {
        public string Id => "augassign";
        public LanguageFeatures ParserFeatures => LanguageFeatures.AugAssign;
        public void Register(IBindingRegistrar registrar) { }
        public void Setup(ScriptContext ctx) { }
    }
}