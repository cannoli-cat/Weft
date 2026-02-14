using UnityEngine;
using Weft.Runtime.Modules;

namespace Weft.Unity.Engine {
    /// <summary>
    /// Base class for custom modules created by designers as ScriptableObjects.
    /// Subclass this, implement CreateModule(), and drop it into the
    /// WeftOptionsSO's customModules list in the Inspector.
    ///
    /// Example:
    /// <code>
    /// [CreateAssetMenu(menuName = "Weft/Modules/Inventory")]
    /// public class InventoryModuleSO : WeftModuleSO {
    ///     public override IWeftModule CreateModule() => new InventoryModule();
    /// }
    /// </code>
    /// </summary>
    public abstract class WeftModuleSO : ScriptableObject {
        public abstract IWeftModule CreateModule();
    }
}