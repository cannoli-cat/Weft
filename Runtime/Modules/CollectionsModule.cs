using System.Collections.Generic;
using System.Linq;
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
                    return dict.GetValueOrDefault(key);
                }

                if (args[0] is string str && args[1] is double si) {
                    var i = (int)si;
                    if (i < 0 || i >= str.Length)
                        throw new System.Exception($"String index {i} out of range (length {str.Length}).");
                    
                    return str[i].ToString();
                }
                
                throw new System.Exception("Index access requires an array, object, or string.");
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
                    dict[args[1]?.ToString() ?? string.Empty] = args[2];
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
                    return dict.GetValueOrDefault(memberName);
                }

                if (args[0] is string str) {
                    if (memberName == "length") return (double)str.Length;
                    throw new System.Exception($"Unknown string property '{memberName}'.");
                }
                
                throw new System.Exception($"Cannot access member '{memberName}' on {args[0]?.GetType().Name ?? "null"}.");
            });

            registrar.Bind("__member_set", (_, args) => {
                var memberName = args[1]?.ToString() ?? string.Empty;

                if (args[0] is Dictionary<string, object> dict) {
                    dict[memberName] = args[2];
                    return null;
                }

                throw new System.Exception($"Cannot set member '{memberName}' on {args[0]?.GetType().Name ?? "null"}.");
            });
            
            registrar.Bind("__object_new", (_, args) => {
                var dict = new Dictionary<string, object>();
                
                for (var i = 0; i + 1 < args.Length; i += 2)
                    dict[args[i].ToString()] = args[i + 1];
                
                return dict;
            });
            
            registrar.Bind("__indexOf", (_, args) => {
                if (args[0] is not string s)
                    throw new System.Exception("indexOf() requires a string.");
                
                var sub = args[1]?.ToString() ?? "";
                return (double)s.IndexOf(sub, System.StringComparison.Ordinal);
            });

            registrar.Bind("__slice", (_, args) => {
                if (args[0] is not string s)
                    throw new System.Exception("slice() requires a string.");

                var start = (int)(double)args[1];
                if (start < 0) start = System.Math.Max(s.Length + start, 0);
                if (start >= s.Length) return "";

                if (args.Length > 2 && args[2] is double de) {
                    var end = (int)de;
                    if (end < 0) end = s.Length + end;
                    
                    end = System.Math.Clamp(end, 0, s.Length);
                    return start >= end ? "" : s.Substring(start, end - start);
                }

                return s[start..];
            });

            registrar.Bind("__split", (_, args) => {
                if (args[0] is not string s)
                    throw new System.Exception("split() requires a string.");
                
                var delim = args.Length > 1 ? args[1]?.ToString() ?? "" : "";
                var parts = s.Split(new[] { delim }, System.StringSplitOptions.None);
                var list = new List<object>(parts.Length);
                
                list.AddRange(parts);
                return list;
            });

            registrar.Bind("__replace", (_, args) => {
                if (args[0] is not string s)
                    throw new System.Exception("replace() requires a string.");
                
                var old = args[1]?.ToString() ?? "";
                var rep = args[2]?.ToString() ?? "";
                
                return s.Replace(old, rep);
            });

            registrar.Bind("__toUpper", (_, args) => {
                if (args[0] is not string s)
                    throw new System.Exception("toUpper() requires a string.");
                
                return s.ToUpperInvariant();
            });

            registrar.Bind("__toLower", (_, args) => {
                if (args[0] is not string s)
                    throw new System.Exception("toLower() requires a string.");
                
                return s.ToLowerInvariant();
            });

            registrar.Bind("__trim", (_, args) => {
                if (args[0] is not string s)
                    throw new System.Exception("trim() requires a string.");
                
                return s.Trim();
            });

            registrar.Bind("__contains", (_, args) => {
                if (args[0] is not string s)
                    throw new System.Exception("contains() requires a string.");
                
                var sub = args[1]?.ToString() ?? "";
                return s.Contains(sub);
            });

            registrar.Bind("__startsWith", (_, args) => {
                if (args[0] is not string s)
                    throw new System.Exception("startsWith() requires a string.");
                
                return s.StartsWith(args[1]?.ToString() ?? "", System.StringComparison.Ordinal);
            });

            registrar.Bind("__endsWith", (_, args) => {
                if (args[0] is not string s)
                    throw new System.Exception("endsWith() requires a string.");
                
                return s.EndsWith(args[1]?.ToString() ?? "", System.StringComparison.Ordinal);
            });
        }

        public void Setup(ScriptContext ctx) { }
    }
}