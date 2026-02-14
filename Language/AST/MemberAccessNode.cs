namespace Weft.Language.AST {
    public class MemberAccessNode : AstNode {
        public AstNode Target { get; }
        public string Member { get; }
        public MemberAccessNode(AstNode target, string member) {
            Target = target; Member = member;
        }
    }
}