namespace Weft.Language.AST {
    public class ReturnNode : AstNode {
        public AstNode Value { get; }
        
        public ReturnNode(AstNode value = null) {
            Value = value;
        }
    }
}