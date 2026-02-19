using Weft.Runtime;
using Weft.Runtime.Binding;
using Weft.Runtime.Modules;
using Weft.Unity.Engine;
using Weft.Unity.Services;

namespace Weft.Demos.ScriptsDemo.Scripts {
    public sealed class DemoInventoryModule : IWeftModule {
        public string Id => "demo-inventory";
        public LanguageFeatures ParserFeatures => LanguageFeatures.None;

        public void Register(IBindingRegistrar registrar) {
            registrar.Bind("addItem", (ctx, args) => {
                ctx.Resolve<DemoInventory>().Add((string)args[0], (int)(double)args[1]);
                return null;
            });

            registrar.Bind("hasItem", (ctx, args) =>
                ctx.Resolve<DemoInventory>().Has((string)args[0]));

            registrar.Bind("removeItem", (ctx, args) =>
                ctx.Resolve<DemoInventory>().Remove((string)args[0], (int)(double)args[1]));

            registrar.Bind("printInventory", (ctx, _) => {
                var inv = ctx.Resolve<DemoInventory>();
                var console = ctx.Resolve<WeftConsoleService>();
                if (inv.Items.Count == 0) {
                    console?.Print("  (empty)");
                    return null;
                }
                foreach (var item in inv.Items)
                    console?.Print($"  {item.name} x{item.qty}");
                return null;
            });
        }

        public void Setup(ScriptContext ctx) {
            if (ctx.GameObject == null) return;
            var inv = ctx.GameObject.GetComponent<DemoInventory>();
            if (inv != null)
                ctx.Services.Add(inv);
        }
    }
}