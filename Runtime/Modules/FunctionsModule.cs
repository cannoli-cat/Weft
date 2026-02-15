using Weft.Runtime.Binding;
using Weft.Unity.Engine;

namespace Weft.Runtime.Modules {
    public class FunctionsModule : IWeftModule {
        public string Id => "functions";
        public LanguageFeatures ParserFeatures => LanguageFeatures.Functions;
        
        public void Register(IBindingRegistrar registrar) { }
        
        public void Setup(ScriptContext ctx) { }
    }
}