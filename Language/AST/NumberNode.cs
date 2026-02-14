namespace Weft.Language.AST {
    public class NumberNode : AstNode {
        public double Value { get; }
        
        public NumberNode(double value) {
            Value = value;
        }
    }
}