namespace Weft.Language.AST {
    public class IndexAssignNode : AstNode {
        public AstNode Target { get; }
        public AstNode Index { get; }
        public AstNode Value { get; }
        public IndexAssignNode(AstNode target, AstNode index, AstNode value) {
            Target = target; Index = index; Value = value;
        }
    }
}