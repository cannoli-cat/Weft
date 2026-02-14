namespace Weft.Language.AST {
    public class DoWhileNode : AstNode {
        public BlockNode Body { get; }
        public AstNode Condition { get; }
        
        public DoWhileNode(BlockNode body, AstNode condition) {
            Body = body;
            Condition = condition;
        }
    }
}