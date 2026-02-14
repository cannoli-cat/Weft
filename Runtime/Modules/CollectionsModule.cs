using System.Collections.Generic;
using Weft.Runtime.Binding;
using Weft.Unity.Engine;

namespace Weft.Runtime.Modules {
    public sealed class CollectionsModule : IWeftModule {
        public string Id => "collections";
        public LanguageFeatures ParserFeatures => LanguageFeatures.Collections;

        public void Register(IBindingRegistrar registrar) {
            registrar.Bind("__push", (_, args) => {
                if (args[0] is List<object> list) { list.Add(args[1]); return null; }
                throw new System.Exception("push() requires an array.");
            });

            registrar.Bind("__pop", (_, args) => {
                if (args[0] is List<object> list) {
                    if (list.Count == 0) throw new System.Exception("pop() on empty array.");
                    var val = list[^1];
                    list.RemoveAt(list.Count - 1);
                    return val;
                }
                throw new System.Exception("pop() requires an array.");
            });

            registrar.Bind("__remove", (_, args) => {
                if (args[0] is List<object> list && args[1] is double d) {
                    var i = (int)d;
                    if (i < 0 || i >= list.Count) throw new System.Exception($"Index {i} out of range.");
                    list.RemoveAt(i);
                    return null;
                }
                throw new System.Exception("remove() requires an array and index.");
            });
        }

        public void Setup(ScriptContext ctx) { }
    }
}