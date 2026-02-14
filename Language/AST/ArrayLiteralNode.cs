using System.Collections.Generic;

namespace Weft.Language.AST {
    public class ArrayLiteralNode : AstNode {
        public List<AstNode> Elements { get; }
        public ArrayLiteralNode(List<AstNode> elements) => Elements = elements;
    }
}