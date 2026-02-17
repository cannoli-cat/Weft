using System.Collections.Generic;
using UnityEngine;

namespace Weft.Demos.ScriptsDemo.Scripts {
    public class DemoInventory : MonoBehaviour {
        private readonly List<(string name, int qty)> items = new();

        public IReadOnlyList<(string name, int qty)> Items => items;

        public void Add(string name, int qty) {
            for (var i = 0; i < items.Count; i++) {
                if (items[i].name == name) {
                    items[i] = (name, items[i].qty + qty);
                    return;
                }
            }
            items.Add((name, qty));
        }

        public bool Has(string name) => items.Exists(i => i.name == name);

        public bool Remove(string name, int qty) {
            for (var i = 0; i < items.Count; i++) {
                if (items[i].name != name) continue;
                if (items[i].qty < qty) return false;
                var remaining = items[i].qty - qty;
                if (remaining == 0) items.RemoveAt(i);
                else items[i] = (name, remaining);
                return true;
            }
            return false;
        }
    }
}