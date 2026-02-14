namespace Weft.Language.AST {
    public class WhileNode : AstNode {
        public AstNode Condition { get; }
        public BlockNode Body { get; }

        public WhileNode(AstNode condition, BlockNode body) {
            Condition = condition;
            Body = body;
        }
    }
}