namespace Weft.Language.AST {
    public sealed class IncDecNode : AstNode {
        public string Name { get; }
        public bool IsIncrement { get; }
        public bool IsPrefix { get; }
        
        public IncDecNode(string name, bool isIncrement, bool isPrefix) {
            Name = name; IsIncrement = isIncrement; IsPrefix = isPrefix;
        }
    }
}