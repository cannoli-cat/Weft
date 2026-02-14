using System.Collections.Generic;

namespace Weft.Language.Lexing {
    public class LexerResult {
        public List<Token> Tokens { get; set; } = new();
        public string Error { get; set; } = null;
        public bool HasError => !string.IsNullOrEmpty(Error);
    }
}