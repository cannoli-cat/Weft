using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Weft.Unity.Engine;

namespace Weft.Samples.RobotDemo.Scripts {
    /// <summary>
    /// Scene host for the Robot demo.
    /// Sets up the grid, wires the script editor to target the robot GameObject,
    /// and binds the grid visualizer.
    ///
    /// Scene setup:
    ///   1. "Weft" GameObject with WeftEngine (already exists)
    ///   2. "Robot" GameObject with RobotGrid + RobotBindings
    ///   3. "RobotUI" GameObject with UIDocument + RobotDemoHost (this script)
    ///
    /// That's it. The RobotBindings MonoBehaviour is what registers all the
    /// robot functions with Weft. No ScriptableObject module needed.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class RobotDemoHost : MonoBehaviour {
        [Tooltip("The GameObject with RobotGrid + RobotBindings")]
        public GameObject robotObject;

        private DropdownField dropdown;
        private ScriptEditor editor;
        private RobotGridView gridView;
        private Label statusLabel;

        private RobotGrid grid;

        private void OnEnable() {
            if (robotObject != null) {
                grid = robotObject.GetComponent<RobotGrid>();
                grid?.BuildDefaultLevel();
            }

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
            editor = root.Q<ScriptEditor>();
            dropdown = root.Q<DropdownField>("ExampleDropdown");
            statusLabel = root.Q<Label>("StatusLabel");

            // create grid view from code and add to container
            var container = root.Q("GridViewContainer");
            if (container != null && grid != null) {
                gridView = new RobotGridView();
                container.Add(gridView);
                gridView.Bind(grid);
            }

            // point the editor at the robot so scripts run with its bindings
            if (editor != null && robotObject != null)
                editor.ScriptTarget = robotObject;

            // populate examples dropdown
            if (dropdown != null) {
                dropdown.choices = RobotDemoScripts.All.Select(e => e.name).ToList();
                dropdown.RegisterValueChangedCallback(OnExampleChanged);

                if (RobotDemoScripts.All.Length > 0) {
                    dropdown.SetValueWithoutNotify(RobotDemoScripts.All[0].name);
                    LoadScript(RobotDemoScripts.All[0].code);
                }
            }

            root.schedule.Execute(UpdateStatus).Every(100);
        }

        private void OnExampleChanged(ChangeEvent<string> evt) {
            var entry = RobotDemoScripts.All.FirstOrDefault(e => e.name == evt.newValue);
            if (!string.IsNullOrEmpty(entry.code))
                LoadScript(entry.code);

            // reset grid when switching examples
            grid?.BuildDefaultLevel();
        }

        private void LoadScript(string code) {
            editor?.SetScript(code);
        }

        private void UpdateStatus() {
            if (statusLabel == null || grid == null) return;
            statusLabel.text = $"Pos: ({grid.Position.x},{grid.Position.y})  " +
                               $"Facing: {grid.Facing}  " +
                               $"Gems: {grid.GemsCollected}/{grid.TotalGems}  " +
                               $"Remaining: {grid.GemsRemaining}";
        }
    }
}