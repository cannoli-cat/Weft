namespace Weft.Language.AST {
    public class ForNode : AstNode {
        public AstNode Init { get; }
        public AstNode Condition { get; }
        public AstNode Step { get; }
        public BlockNode Body { get; }
        
        public ForNode(AstNode init, AstNode condition, AstNode step, BlockNode body) {
            Init = init;
            Condition = condition;
            Step = step;
            Body = body;
        }
    }
}