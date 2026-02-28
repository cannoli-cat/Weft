using System.Collections.Generic;

namespace Weft.Language.AST {
    public class FunctionCallNode : AstNode {
        public string FunctionName { get; }
        public AstNode Target { get; }
        public List<AstNode> Arguments { get; }

        public FunctionCallNode(string functionName, List<AstNode> arguments) {
            FunctionName = functionName;
            Arguments = arguments;
        }

        public FunctionCallNode(AstNode target, List<AstNode> arguments) {
            Target = target;
            Arguments = arguments;
        }
    }
}