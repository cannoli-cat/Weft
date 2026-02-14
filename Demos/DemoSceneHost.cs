using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Weft.Unity.Engine;

namespace Weft.Demos {
    [RequireComponent(typeof(UIDocument))]
    public class DemoSceneHost : MonoBehaviour {
        private DropdownField dropdown;
        private ScriptEditor editor;
        private GameObject scriptTarget;

        private void OnEnable() {
            CreateScriptTarget();
            
            var doc = GetComponent<UIDocument>();
            var root = doc.rootVisualElement;
            root.schedule.Execute(() => Init(root));
        }
        
        private static void PreCacheDemoScripts() {
            foreach (var demo in DemoScripts.All) {
                WeftEngine.TryRun(demo.code, new ScriptContext(WeftEngine.Instance.Options.Capabilities));
            }
        
            Debug.Log($"[Weft Demo] Pre-cached {DemoScripts.All.Length} scripts");
        }

        private void Init(VisualElement root) {
            editor = root.Q<ScriptEditor>();
            dropdown = root.Q<DropdownField>("ExampleDropdown");

            if (editor != null)
                editor.ScriptTarget = scriptTarget;

            if (dropdown == null) return;

            dropdown.choices = DemoScripts.All.Select(e => e.name).ToList();
            dropdown.RegisterValueChangedCallback(OnExampleChanged);

            if (DemoScripts.All.Length > 0) {
                dropdown.SetValueWithoutNotify(DemoScripts.All[0].name);
                LoadScript(DemoScripts.All[0].code);
            }
            
            PreCacheDemoScripts();
        }

        private void OnExampleChanged(ChangeEvent<string> evt) {
            var entry = DemoScripts.All.FirstOrDefault(e => e.name == evt.newValue);
            if (!string.IsNullOrEmpty(entry.code))
                LoadScript(entry.code);
        }

        private void LoadScript(string code) {
            editor?.SetScript(code);
        }

        /// <summary>
        /// Creates a GameObject that represents the "thing" scripts run on.
        /// In a real game this would be the NPC/player/object, here it's a
        /// demo stand-in with all the components scripts might need.
        /// </summary>
        private void CreateScriptTarget() {
            if (scriptTarget != null) return;
            scriptTarget = new GameObject("[ScriptTarget]");
            scriptTarget.AddComponent<DemoInventory>();
            // add more demo components here as you add modules
        }

        private void OnDestroy() {
            if (scriptTarget != null) Destroy(scriptTarget);
        }
    }
}