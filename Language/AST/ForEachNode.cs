namespace Weft.Language.AST {
    public class ForEachNode : AstNode {
        public AstNode Collection { get; }
        public AstNode Callback { get; }
        
        public ForEachNode(AstNode collection, AstNode callback) {
            Collection = collection;
            Callback = callback;
        }
    }
}