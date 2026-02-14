namespace Weft.Language.AST {
    public class IfNode : AstNode {
        public AstNode Condition { get; }
        public AstNode TrueBranch { get; }
        public AstNode FalseBranch { get; }

        public IfNode(AstNode condition, AstNode trueBranch, AstNode falseBranch = null) {
            Condition = condition;
            TrueBranch = trueBranch;
            FalseBranch = falseBranch;
        }
    }
}