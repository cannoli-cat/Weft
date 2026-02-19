using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Weft.Samples.ScriptsDemo.Scripts {
    [RequireComponent(typeof(UIDocument))]
    public class DemoSceneHost : MonoBehaviour {
        private DropdownField dropdown;
        private Unity.Engine.ScriptEditor editor;
        private GameObject scriptTarget;

        private void OnEnable() {
            CreateScriptTarget();

            var doc = GetComponent<UIDocument>();

            if (doc.panelSettings == null) {
                var ps = ScriptableObject.CreateInstance<PanelSettings>();
                ps.themeStyleSheet = Resources.FindObjectsOfTypeAll<ThemeStyleSheet>()[0];
                ps.scaleMode = PanelScaleMode.ScaleWithScreenSize;
                ps.referenceResolution = new Vector2Int(1920, 1080);
                doc.panelSettings = ps;
            }

            var root = doc.rootVisualElement;
            root.schedule.Execute(() => Init(root));
        }

        private void Init(VisualElement root) {
            editor = root.Q<Unity.Engine.ScriptEditor>();
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
        }

        private void OnExampleChanged(ChangeEvent<string> evt) {
            var entry = DemoScripts.All.FirstOrDefault(e => e.name == evt.newValue);
            if (!string.IsNullOrEmpty(entry.code))
                LoadScript(entry.code);
        }

        private void LoadScript(string code) {
            editor?.SetScript(code);
        }

        private void CreateScriptTarget() {
            if (scriptTarget != null) return;
            scriptTarget = new GameObject("[ScriptTarget]");
            scriptTarget.AddComponent<DemoInventory>();
        }

        private void OnDestroy() {
            if (scriptTarget != null) Destroy(scriptTarget);
        }
    }
}