namespace Weft.Language.AST {
    public class IdentifierNode : AstNode {
        public string Name { get; }
        
        public IdentifierNode(string name) { Name = name; }
    }
}