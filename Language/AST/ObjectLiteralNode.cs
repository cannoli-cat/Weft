using System.Collections.Generic;

namespace Weft.Language.AST {
    public class ObjectLiteralNode : AstNode {
        public List<(AstNode Key, AstNode Value)> Entries { get; }
        public ObjectLiteralNode(List<(AstNode Key, AstNode Value)> entries) => Entries = entries;
    }
}