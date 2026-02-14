using System.Collections.Generic;
using Weft.Language.AST;

namespace Weft.Language.Parsing {
    public class ParseResult {
        public List<AstNode> Nodes { get; set; } = new();
        public string Error { get; set; } = null;
        public bool HasError => !string.IsNullOrEmpty(Error);
    }
}