namespace Weft.Language.AST {
    public sealed class IncDecNode : AstNode {
        public AstNode Target { get; }
        public bool IsIncrement { get; }
        public bool IsPrefix { get; }

        public IncDecNode(AstNode target, bool isIncrement, bool isPrefix) {
            Target = target;
            IsIncrement = isIncrement;
            IsPrefix = isPrefix;
        }
    }
}