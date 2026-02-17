using System;
using System.Reflection;
using UnityEngine;
using Weft.Runtime.Binding;

namespace Weft.Unity.Engine {
    public abstract class WeftBindings : MonoBehaviour {
        public virtual void Setup() { }

        internal void RegisterAttributes(IBindingRegistrar r) {
            var methods = GetType().GetMethods(
                BindingFlags.Instance | BindingFlags.Public |
                BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

            foreach (var method in methods) {
                var attr = method.GetCustomAttribute<WeftFunctionAttribute>();
                if (attr == null) continue;

                var parameters = method.GetParameters();
                HostFunc func;

                if (parameters.Length == 1)
                    func = (ctx, _) => method.Invoke(this, new object[] { ctx });
                else if (parameters.Length == 2 && parameters[1].ParameterType == typeof(object[]))
                    func = (ctx, args) => method.Invoke(this, new object[] { ctx, args });
                else
                    func = BuildTypedDelegate(method, parameters);

                r.Bind(attr.Name, func);
            }
        }

        private HostFunc BuildTypedDelegate(MethodInfo method, ParameterInfo[] parameters) {
            var paramTypes = new Type[parameters.Length - 1];
            for (var i = 1; i < parameters.Length; i++)
                paramTypes[i - 1] = parameters[i].ParameterType;

            return (ctx, args) => {
                var invokeArgs = new object[parameters.Length];
                invokeArgs[0] = ctx;

                for (var i = 0; i < paramTypes.Length; i++) {
                    if (i >= args.Length) {
                        invokeArgs[i + 1] = paramTypes[i].IsValueType
                            ? Activator.CreateInstance(paramTypes[i])
                            : null;
                        continue;
                    }

                    invokeArgs[i + 1] = CastArg(args[i], paramTypes[i], i);
                }

                return method.Invoke(this, invokeArgs);
            };
        }

        private static object CastArg(object arg, Type target, int index) {
            if (arg == null)
                return target.IsValueType ? Activator.CreateInstance(target) : null;

            if (target == typeof(string)) return arg.ToString();
            if (target == typeof(double)) return Convert.ToDouble(arg);
            if (target == typeof(int)) return (int)Convert.ToDouble(arg);
            if (target == typeof(float)) return (float)Convert.ToDouble(arg);
            if (target == typeof(bool)) return arg is bool b ? b : Convert.ToBoolean(arg);
            
            return target == typeof(object) ? arg : throw new ArgumentException($"Cannot convert argument {index} ({arg.GetType().Name}) to {target.Name}");
        }
    }
}