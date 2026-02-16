namespace Weft.Language.Lexing {
    public class Token {
        public TokenType Type { get; private set; }
        public string Value { get; private set; }
        public string RawValue { get; private set; }
        public int Line { get; private set; }

        public Token(TokenType type, string value, int line, string rawValue = null) {
            Type = type;
            Value = value;
            Line = line;
            RawValue = rawValue ?? value;
        }
    }
}