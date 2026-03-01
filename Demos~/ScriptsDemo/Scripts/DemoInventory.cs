using System.Collections.Generic;
using UnityEngine;

namespace Weft.Demos.ScriptsDemo.Scripts {
    public class DemoInventory : MonoBehaviour {
        private readonly List<DemoItem> items = new();

        public IReadOnlyList<DemoItem> Items => items;

        public void Add(string name, int qty) {
            var item = items.Find(i => i.Name == name);
            if (item != null) {
                item.AddQuantity(qty);
            } else {
                items.Add(new DemoItem(name, qty));
            }
        }

        public bool Has(string name) => items.Exists(i => i.Name == name);

        public DemoItem GetItem(string name) {
            return items.Find(i => i.Name == name);
        }

        public bool Remove(string name, int qty) {
            var item = items.Find(i => i.Name == name);
            if (item == null || item.Quantity < qty) return false;
            
            item.RemoveQuantity(qty);
            if (item.Quantity == 0) items.Remove(item);
            return true;
        }
    }
}