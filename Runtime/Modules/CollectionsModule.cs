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
            
            registrar.Bind("__array_new", (_, args) => {
                var list = new List<object>(args.Length);
                for (var i = 0; i < args.Length; i++)
                    list.Add(args[i]);
                return list;
            });

            registrar.Bind("__index_get", (_, args) => {
                if (args[0] is List<object> list && args[1] is double d) {
                    var i = (int)d;
                    if (i < 0 || i >= list.Count)
                        throw new System.Exception($"Index {i} out of range (length {list.Count}).");
                    return list[i];
                }
                
                if (args[0] is Dictionary<string, object> dict) {
                    var key = args[1]?.ToString();
                    return dict.TryGetValue(key, out var val) ? val : null;
                }
                
                throw new System.Exception("Index access requires an array/object.");
            });

            registrar.Bind("__index_set", (_, args) => {
                if (args[0] is List<object> list && args[1] is double d) {
                    var i = (int)d;
                    if (i < 0 || i >= list.Count)
                        throw new System.Exception($"Index {i} out of range (length {list.Count}).");
                    list[i] = args[2];
                    return null;
                }
                
                if (args[0] is Dictionary<string, object> dict) {
                    dict[args[1]?.ToString()] = args[2];
                    return null;
                }
                
                throw new System.Exception("Index assignment requires an array/object.");
            });

            registrar.Bind("__member_get", (_, args) => {
                var memberName = (string)args[1];

                if (args[0] is List<object> list) {
                    if (memberName == "length") return (double)list.Count;
                    throw new System.Exception($"Unknown array member '{memberName}'.");
                }
                
                if (args[0] is Dictionary<string, object> dict) {
                    if (memberName == "length") return (double)dict.Count;
                    return dict.TryGetValue(memberName, out var val) ? val : null;
                }
                
                throw new System.Exception($"Unknown member '{memberName}' on {args[0]?.GetType().Name ?? "null"}.");
            });
            
            registrar.Bind("__object_new", (_, args) => {
                var dict = new Dictionary<string, object>();
                for (var i = 0; i + 1 < args.Length; i += 2)
                    dict[args[i].ToString()] = args[i + 1];
                return dict;
            });
        }

        public void Setup(ScriptContext ctx) { }
    }
}