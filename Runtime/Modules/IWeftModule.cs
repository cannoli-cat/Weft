using Weft.Runtime.Binding;
using Weft.Unity.Engine;

namespace Weft.Runtime.Modules {
    public interface IWeftModule {
        string Id { get; }
        LanguageFeatures ParserFeatures { get; }
        void Register(IBindingRegistrar registrar);
        void Setup(ScriptContext ctx);
    }
}