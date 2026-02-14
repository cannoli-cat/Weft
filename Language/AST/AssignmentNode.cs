namespace Weft.Language.AST {
    public class AssignmentNode : AstNode {
        public string Name { get; }
        public AstNode Value { get; }

        public AssignmentNode(string name, AstNode value) {
            Name = name;
            Value = value;
        }
    }
}