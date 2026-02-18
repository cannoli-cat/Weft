using Weft.Runtime.Binding;
using Weft.Unity.Engine;

namespace Weft.Runtime.Modules {
    public class MathModule : IWeftModule {
        public string Id => "math";
        public LanguageFeatures ParserFeatures => LanguageFeatures.None;
        
        private static readonly System.Random Random = new();

        public void Register(IBindingRegistrar registrar) {
            registrar.Bind("abs", (_, args) => {
                if (args.Length != 1 || args[0] is not double d)
                    throw new System.Exception("abs() requires a single number argument.");

                return System.Math.Abs(d);
            });

            registrar.Bind("sqrt", (_, args) => {
                if (args.Length != 1 || args[0] is not double d)
                    throw new System.Exception("sqrt() requires a single number argument.");

                return System.Math.Sqrt(d);
            });

            registrar.Bind("pow", (_, args) => {
                if (args.Length != 2 || args[0] is not double d || args[1] is not double p)
                    throw new System.Exception("pow() requires two number arguments.");

                return System.Math.Pow(d, p);
            });

            registrar.Bind("floor", (_, args) => {
                if (args.Length != 1 || args[0] is not double d)
                    throw new System.Exception("floor() requires a single number argument.");

                return System.Math.Floor(d);
            });

            registrar.Bind("ceil", (_, args) => {
                if (args.Length != 1 || args[0] is not double d)
                    throw new System.Exception("ceil() requires a single number argument.");

                return System.Math.Ceiling(d);
            });

            registrar.Bind("round", (_, args) => {
                if (args.Length != 1 || args[0] is not double d)
                    throw new System.Exception("round() requires a single number argument.");

                return System.Math.Round(d);
            });
            
            registrar.Bind("min", (_, args) => {
                if (args.Length != 2 || args[0] is not double d1 || args[1] is not double d2)
                    throw new System.Exception("min() requires two number arguments.");

                return System.Math.Min(d1, d2);
            });
            
            registrar.Bind("max", (_, args) => {
                if (args.Length != 2 || args[0] is not double d1 || args[1] is not double d2)
                    throw new System.Exception("max() requires two number arguments.");

                return System.Math.Max(d1, d2);
            });
            
            registrar.Bind("random", (_, args) => {
                if (args.Length != 0)
                    throw new System.Exception("random() takes no arguments.");

                return Random.NextDouble();
            });
            
            registrar.Bind("randomRange", (_, args) => {
                if (args.Length != 2 || args[0] is not double min || args[1] is not double max)
                    throw new System.Exception("randomRange() requires two number arguments.");

                if (min > max)
                    throw new System.Exception("randomRange() requires min to be less than or equal to max.");

                return min + Random.NextDouble() * (max - min);
            });
        }

        public void Setup(ScriptContext ctx) { }
    }
}