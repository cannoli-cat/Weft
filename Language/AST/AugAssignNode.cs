namespace Weft.Language.AST {
    public class AugAssignNode : AstNode {
        public AstNode Target { get; }
        public string Operator { get; }
        public AstNode Value { get; }

        public AugAssignNode(AstNode target, string op, AstNode value) {
            Target = target;
            Operator = op;
            Value = value;
        }
    }
}