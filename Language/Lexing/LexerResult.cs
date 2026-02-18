using System.Collections.Generic;

namespace Weft.Language.Lexing {
    public class LexerResult {
        public List<Token> Tokens { get; set; } = new();
        public WeftError Error { get; set; }
        public bool HasError => Error != null;
    }
}