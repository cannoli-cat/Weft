using System.Collections.Generic;

namespace Weft.Language.AST {
    public class FuncDeclNode : AstNode {
        public string Name { get; }
        public List<string> Parameters { get; }
        public BlockNode Body { get; }
        
        public FuncDeclNode(string name, List<string> parameters, BlockNode body) {
            Name = name;
            Parameters = parameters;
            Body = body;
        }
    }
}