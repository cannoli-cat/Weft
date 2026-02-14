namespace Weft.Language.AST {
    public sealed class ExprStmtNode : AstNode {
        public AstNode Expr { get; }
        
        public ExprStmtNode(AstNode expr) { Expr = expr; }
    }
}