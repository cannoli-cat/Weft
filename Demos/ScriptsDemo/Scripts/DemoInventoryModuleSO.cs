using UnityEngine;
using Weft.Runtime.Modules;
using Weft.Unity.Engine;

namespace Weft.Demos.ScriptsDemo.Scripts {
    [CreateAssetMenu(menuName = "Weft/Modules/Inventory")]
    public class DemoInventoryModuleSO : WeftModuleSO {
        public override IWeftModule CreateModule() => new DemoInventoryModule();
    }
}