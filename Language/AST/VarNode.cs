namespace Weft.Language.AST {
    public class VarNode : AstNode {
        public string Name { get; }
        public AstNode Value { get; }
        
        public VarNode(string name, AstNode value) {
            Name = name;
            Value = value;
        }
    }
}