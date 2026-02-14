namespace Weft.Language.AST {
    public sealed class UnaryNode : AstNode {
        public string Operator { get; }
        public AstNode Operand { get; }
        
        public UnaryNode(string op, AstNode operand) { Operator = op; Operand = operand; }
    }
}