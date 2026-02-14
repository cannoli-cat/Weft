namespace Weft.Language.AST {
    public class BinaryOperationNode : AstNode {
        public AstNode Left { get; }
        public string Operator { get; }
        public AstNode Right { get; }
        
        public BinaryOperationNode(AstNode left, string op, AstNode right) {
            Left = left;
            Operator = op;
            Right = right;
        }
    }
}