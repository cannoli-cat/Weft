using System.Collections.Generic;

namespace Weft.Language.AST {
    public class BlockNode : AstNode {
        public List<AstNode> Statements { get; }
        
        public BlockNode(List<AstNode> statements) {
            Statements = statements;
        }
    }
}