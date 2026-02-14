namespace Weft.Language.AST {
    public class BoolNode : AstNode {
        public bool Value { get; }
        
        public BoolNode(bool value) {
            Value = value;
        }
    }
}