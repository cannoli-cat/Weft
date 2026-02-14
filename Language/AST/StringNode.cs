namespace Weft.Language.AST {
    public class StringNode : AstNode {
        public string Value { get; }

        public StringNode(string value) {
            Value = value;
        }
    }
}