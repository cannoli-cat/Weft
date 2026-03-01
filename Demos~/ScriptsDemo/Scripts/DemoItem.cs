using Weft.Runtime.Binding;

namespace Weft.Demos.ScriptsDemo.Scripts {
    public class DemoItem : IWeftObject {
        public string Name { get; private set; }
        public int Quantity { get; private set; }
        
        // This is the only property the script is allowed to modify
        public string Tag { get; set; }

        public DemoItem(string name, int quantity) {
            Name = name;
            Quantity = quantity;
            Tag = "None";
        }

        public void AddQuantity(int amount) {
            Quantity += amount;
        }

        public void RemoveQuantity(int amount) {
            Quantity -= amount;
        }

        // IWeftObject Implementation
        public object GetMember(string name) {
            return name switch {
                "name" => Name,
                "quantity" => (double)Quantity, // Weft expects numbers as doubles
                "tag" => Tag,
                _ => null
            };
        }

        public void SetMember(string name, object value) {
            switch (name) {
                case "name":
                case "quantity":
                    throw new System.Exception($"Security Exception: '{name}' is read-only!");
                case "tag":
                    Tag = value?.ToString();
                    break;
                default:
                    throw new System.Exception($"Cannot set unknown property '{name}'");
            }
        }
    }
}