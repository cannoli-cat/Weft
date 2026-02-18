using System.Collections.Generic;
using Weft.Language.AST;

namespace Weft.Language.Parsing {
    public class ParseResult {
        public List<AstNode> Nodes { get; set; } = new();
        public WeftError Error { get; set; }
        public bool HasError => Error != null;
    }
}