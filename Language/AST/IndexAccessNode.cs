namespace Weft.Language.AST {
    public class IndexAccessNode : AstNode {
        public AstNode Target { get; }  // the array expression
        public AstNode Index { get; }   // the index expression
        public IndexAccessNode(AstNode target, AstNode index) {
            Target = target; Index = index;
        }
    }
}