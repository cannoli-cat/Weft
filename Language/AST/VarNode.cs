namespace Weft.Language.AST {
    public class VarNode : AstNode {
        public string Name { get; }
        public AstNode Value { get; }
        public bool IsConst { get; }
        
        public VarNode(string name, AstNode value, bool isConst = false) {
            Name = name;
            Value = value;
            IsConst = isConst;
        }
    }
}