using Weft.Language.AST;

namespace Weft.Language.AST {
    public class MemberAssignNode : AstNode {
        public AstNode Target { get; }
        public string Member { get; }
        public AstNode Value { get; }

        public MemberAssignNode(AstNode target, string member, AstNode value) {
            Target = target;
            Member = member;
            Value = value;
        }
    }
}