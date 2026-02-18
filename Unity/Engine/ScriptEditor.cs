using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Weft.Language.Lexing;
using Weft.Runtime.Services;
using Weft.Unity.Engine;
using Weft.Unity.Services;

namespace Weft.Unity.Engine {
    [UxmlElement]
    public partial class ScriptEditor : VisualElement {
        private readonly TextField inputField;
        private readonly TextElement highlightOverlay;
        private readonly TextElement gutter;
        private string plainText = "";

        private Button runButton, pauseButton;
        private Label consoleLabel;

        private const string KeywordColor = "#C678DD";
        private const string IdentifierColor = "#E06C75";
        private const string NumberColor = "#D19A66";
        private const string StringColor = "#98C379";
        private const string OperatorColor = "#56B6C2";
        private const string CommentColor = "#9c9c9c";

        private const int IndentSize = 4;

        private int? runningPid;
        
        public GameObject ScriptTarget { get; set; }

        public ScriptEditor() {
            style.flexGrow = 1;
            style.flexDirection = FlexDirection.Column;
            style.position = Position.Relative;

            var container = new VisualElement {
                style = {
                    flexDirection = FlexDirection.Row,
                    flexGrow = 1,
                    height = Length.Percent(100),
                }
            };
            Add(container);

            gutter = new TextElement {
                name = "LineNumbers",
                enableRichText = false,
                pickingMode = PickingMode.Ignore,
                style = {
                    unityTextAlign = TextAnchor.UpperRight,
                    marginRight = 15,
                    color = Color.gray,
                    flexShrink = 0,
                    width = 30,
                }
            };
            container.Add(gutter);

            var editorContainer = new VisualElement {
                style = {
                    flexGrow = 1,
                    position = Position.Relative
                }
            };
            container.Add(editorContainer);

            highlightOverlay = new TextElement {
                name = "HighlightOverlay",
                enableRichText = true,
                pickingMode = PickingMode.Ignore,
                style = {
                    position = Position.Absolute,
                    whiteSpace = WhiteSpace.Normal,
                    color = Color.white,
                    marginLeft = 2
                }
            };
            highlightOverlay.AddToClassList("NoPaddingNoMargin");
            highlightOverlay.AddToClassList("NoBorderNoBackground");
            editorContainer.Add(highlightOverlay);

            inputField = new TextField {
                multiline = true,
                style = {
                    flexGrow = 1,
                    unityTextAlign = TextAnchor.UpperLeft,
                    color = new Color(1f, 1f, 1f, 0.01f),
                }
            };
            inputField.AddToClassList("NoPaddingNoMargin");
            inputField.AddToClassList("NoBorderNoBackground");

            var unityTextInput1 = inputField.Q("unity-text-input");
            unityTextInput1.AddToClassList("NoPaddingNoMargin");
            unityTextInput1.AddToClassList("NoBorderNoBackground");

            editorContainer.Add(inputField);

            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachedFromPanel);

            UpdateGutter();
        }

        private void OnAttachedToPanel(AttachToPanelEvent evt) {
            inputField.RegisterValueChangedCallback(HandleChangeEvent);

            var buttonContainer = panel?.visualTree.Q("ScriptButtonContainer");
            if (buttonContainer != null) {
                runButton = buttonContainer.Q<Button>("RunButton");
                pauseButton = buttonContainer.Q<Button>("PauseButton");

                runButton?.RegisterCallback<ClickEvent>(RunScript);
                pauseButton?.RegisterCallback<ClickEvent>(PauseScript);
            }

            var consoleContainer = panel?.visualTree.Q("ConsoleContainer");
            if (consoleContainer != null) {
                consoleContainer.pickingMode = PickingMode.Ignore;
                consoleLabel = consoleContainer.Q<Label>("Output");
            }

            evt.StopPropagation();
        }

        private void OnDetachedFromPanel(DetachFromPanelEvent evt) {
            inputField.UnregisterValueChangedCallback(HandleChangeEvent);
            runButton?.UnregisterCallback<ClickEvent>(RunScript);
            pauseButton?.UnregisterCallback<ClickEvent>(PauseScript);
            evt.StopPropagation();
        }

        private void HandleChangeEvent(ChangeEvent<string> evt) {
            UpdateIndentation(evt);
            plainText = inputField.value;
            UpdateHighlighting(plainText);
            UpdateGutter();
        }

        private void UpdateHighlighting(string text) {
            plainText = text;

            var tokens = Lexer.Tokenize(plainText, true).Tokens;
            var highlightedText = "";
            var currentIndex = 0;

            foreach (var token in tokens) {
                var raw = token.RawValue;
                var tokenIndex = plainText.IndexOf(raw, currentIndex, System.StringComparison.Ordinal);

                if (tokenIndex < 0) continue;

                if (tokenIndex > currentIndex) {
                    highlightedText += plainText.Substring(currentIndex, tokenIndex - currentIndex);
                }

                var color = token.Type switch {
                    TokenType.Keyword => KeywordColor,
                    TokenType.Identifier => IdentifierColor,
                    TokenType.Number => NumberColor,
                    TokenType.String => StringColor,
                    TokenType.Operator => OperatorColor,
                    TokenType.Comment => CommentColor,
                    _ => "#56B6C2"
                };

                highlightedText += $"<color={color}>{raw}</color>";
                currentIndex = tokenIndex + raw.Length;
            }

            if (currentIndex < plainText.Length) {
                highlightedText += plainText[currentIndex..];
            }

            highlightOverlay.text = highlightedText;
        }

        private void UpdateIndentation(ChangeEvent<string> evt) {
            var newText = evt.newValue;
            var oldText = evt.previousValue;
            if (newText.Length - oldText.Length != 1) return;

            var pos = inputField.cursorIndex;
            if (pos < 1) return;

            switch (newText[pos - 1]) {
                case '\n' when pos < newText.Length && newText[pos] == '}': {
                    var lineStart = pos - 1;
                    while (lineStart > 0 && newText[lineStart - 1] != '\n')
                        lineStart--;

                    var depth = GetDepth(newText, lineStart);
                    var newLineIndent = new string(' ', depth * IndentSize);
                    var braceIndent = new string(' ', Mathf.Max(depth - 1, 0) * IndentSize);

                    var braceLineStart = pos;
                    while (braceLineStart > 0 && newText[braceLineStart - 1] != '\n')
                        braceLineStart--;

                    var braceLineEnd = braceLineStart;
                    while (braceLineEnd < newText.Length &&
                           (newText[braceLineEnd] == ' ' || newText[braceLineEnd] == '\t'))
                        braceLineEnd++;

                    var finalText = newText[..lineStart] +
                                    newLineIndent +
                                    newText.Substring(lineStart, braceLineStart - lineStart) +
                                    braceIndent +
                                    newText[braceLineEnd..];

                    inputField.SetValueWithoutNotify(finalText);
                    plainText = finalText;
                    inputField.SelectRange(lineStart + newLineIndent.Length, lineStart + newLineIndent.Length);
                    break;
                }
                case '\n':
                    InsertIndentAt(newText, pos, subtractOne: false);
                    break;
                case '}': {
                    var lineStart = pos;
                    while (lineStart > 0 && newText[lineStart - 1] != '\n')
                        lineStart--;

                    InsertIndentAt(newText, lineStart, subtractOne: true);
                    break;
                }
            }
        }

        private void InsertIndentAt(string text, int insertPos, bool subtractOne, bool setCursor = true) {
            if (insertPos < 0 || insertPos > text.Length) return;

            var depth = GetDepth(text, insertPos);
            if (subtractOne) depth = Mathf.Max(depth - 1, 0);

            var ws = 0;
            while (insertPos + ws < text.Length
                   && (text[insertPos + ws] == ' ' || text[insertPos + ws] == '\t'))
                ws++;
            text = text.Remove(insertPos, ws);

            var indent = new string(' ', depth * IndentSize);
            text = text.Insert(insertPos, indent);

            inputField.SetValueWithoutNotify(text);
            plainText = text;

            if (setCursor) {
                inputField.SelectRange(insertPos + indent.Length,
                    insertPos + indent.Length);
            }
        }

        private static int GetDepth(string text, int insertPos) {
            var opens = text.Take(insertPos).Count(c => c == '{');
            var closes = text.Take(insertPos).Count(c => c == '}');
            return opens - closes;
        }

        private void UpdateGutter() {
            var lines = plainText.Split('\n');
            gutter.text = "";
            for (var i = 0; i < lines.Length; i++) {
                gutter.text += (i + 1) + "\n";
            }
        }

        private void RunScript(ClickEvent evt) {
            consoleLabel.text = "";
            var script = plainText;

            if (string.IsNullOrWhiteSpace(script)) {
                PrintToConsole("No script to run.", true);
                return;
            }

            var consoleService = new WeftConsoleService((msg, isErr) => {
                if (msg == "__CLEAR__") { consoleLabel.text = string.Empty; return; }
                PrintToConsole(msg, isErr);
            });

            var services = new WeftServiceProvider().Add(consoleService);
            var ctx = new ScriptContext(WeftEngine.Instance.Options.Capabilities) {
                GameObject = ScriptTarget,
                Services = services
            };

            var (pid, error) = WeftEngine.TryRun(script, ctx);
            if (error != null) {
                PrintToConsole(error, true); 
                return;
            }
            
            runningPid = pid;
            
            PrintToConsole($"spawned pid {runningPid}");
        }

        private void PauseScript(ClickEvent evt) {
            if (runningPid.HasValue && WeftEngine.Instance != null) {
                if (WeftEngine.Instance.Kill(runningPid.Value))
                    PrintToConsole($"killed pid {runningPid.Value}");
                else
                    PrintToConsole($"no process {runningPid.Value} to kill", true);
                runningPid = null;
            }
            else {
                PrintToConsole("no running process", true);
            }
        }

        public void SetScript(string code) {
            inputField.value = code;
            plainText = code;
            UpdateHighlighting(plainText);
            UpdateGutter();
        }

        private void PrintToConsole(string message, bool isError = false) {
            if (consoleLabel != null) {
                var formatted = isError
                    ? $"<color=#FF0000>{message}</color>"
                    : $"<color=#FFFFFF>{message}</color>";
                consoleLabel.text += formatted + "\n";
            }
            else {
                Debug.LogWarning("Console label not found.");
            }
        }
    }
}
