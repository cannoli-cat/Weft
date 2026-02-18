using System.Collections.Generic;

namespace Weft.Language.Lexing {
    // lexer converts input text into a list of tokens
    public static class Lexer {
        // set of keywords in the language
        private static readonly HashSet<string> Keywords = new() {
            "var", "if", "else", "true", "false",
            "do", "while", "for",
            "return", "break",
            "continue", "function",
            "null"
        };
        
        // tokenizes the input string into a LexerResult containing tokens an dany error
        public static LexerResult Tokenize(string input, bool includeComments = false) {
            var result = new LexerResult {
                Tokens = new List<Token>()
            };

            var i = 0; // current position in input
            var line = 1; // current line number for error reporting
    
            while (i < input.Length) {
                var c = input[i];

                // skip whitespace
                if (char.IsWhiteSpace(c)) {
                    if (c == '\n') line++;
                    i++;
                    continue;
                }

                // handles identifiers and keywords
                if (char.IsLetter(c) || c == '_') {
                    var start = i;
                    while (i < input.Length && (char.IsLetterOrDigit(input[i]) || input[i] == '_')) {
                        i++;
                    }
                    var word = input.Substring(start, i - start);

                    // determines if the word is a keyword or identifier
                    result.Tokens.Add(IsKeyword(word)
                        ? new Token(TokenType.Keyword, word, line)
                        : new Token(TokenType.Identifier, word, line));

                    continue;
                }

                // handles numeric literals
                if (char.IsDigit(c)) {
                    var start = i;
                    var hasDot = false;

                    while (i < input.Length && (char.IsDigit(input[i]) || (!hasDot && input[i] == '.'))) {
                        if (input[i] == '.') hasDot = true;
                        i++;
                    }

                    var numStr = input.Substring(start, i - start);

                    result.Tokens.Add(new Token(TokenType.Number, numStr, line));
                    continue;
                }

                // handles string literals in double quotes (with backslash escapes)
                if (c == '"') {
                    var rawStart = i;
                    i++; // skip opening quote
                    var sb = new System.Text.StringBuilder();

                    while (i < input.Length && input[i] != '"') {
                        if (input[i] == '\n') line++;
                        
                        if (input[i] == '\\' && i + 1 < input.Length) {
                            var esc = input[i + 1];
                            switch (esc) {
                                case '"':  sb.Append('"');  i += 2; continue;
                                case '\\': sb.Append('\\'); i += 2; continue;
                                case 'n':  sb.Append('\n'); i += 2; continue;
                                case 't':  sb.Append('\t'); i += 2; continue;
                            }
                        }
                        
                        sb.Append(input[i]);
                        i++;
                    }

                    if (i < input.Length) {
                        i++; // skip closing quote
                        var raw = input.Substring(rawStart, i - rawStart);
                        result.Tokens.Add(new Token(TokenType.String, sb.ToString(), line, raw));
                    }
                    else
                        result.Error = new WeftError(ErrorPhase.Lex, "Unterminated string literal", line);

                    continue;
                }

                if (c == '/' && i + 1 < input.Length) {
                    var n = input[i + 1];

                    if (n == '/') {
                        var start = i;
                        i += 2;

                        while (i < input.Length && input[i] != '\n' && input[i] != '\r') i++;

                        if (includeComments) {
                            var text = input.Substring(start, i - start);
                            result.Tokens.Add(new Token(TokenType.Comment, text, line));
                        }
                        
                        continue;
                    }

                    if (n == '*') {
                        var start = i;
                        i += 2;
                        var closed = false;

                        while (i < input.Length - 1) {
                            if (input[i] == '*' && input[i + 1] == '/') {
                                i += 2;
                                closed = true;
                                break;
                            }

                            if (input[i] == '\n') line++;
                            
                            i++;
                        }

                        if (!closed) {
                            result.Error = new WeftError(ErrorPhase.Lex, "Unterminated block comment", line);
                            break;
                        }

                        if (includeComments) {
                            var text = input.Substring(start, i - start);
                            result.Tokens.Add(new Token(TokenType.Comment, text, line));
                        }

                        continue;
                    }
                }

                // handle operators like+, -, *, /, ==, !=
                if ("+-*/=<>!&|%".Contains(c)) {
                    var op = c.ToString();

                    // check for any two character operators
                    if (i + 1 < input.Length) {
                        var nextChar = input[i + 1];
                        
                        if ((c == '=' && nextChar == '=') ||
                            (c == '!' && nextChar == '=') ||
                            (c == '<' && nextChar == '=') ||
                            (c == '>' && nextChar == '=') ||
                            (c == '&' && nextChar == '&') ||
                            (c == '|' && nextChar == '|') ||
                            (c == '+' && nextChar == '+') ||
                            (c == '+' && nextChar == '=') ||
                            (c == '-' && nextChar == '-') ||
                            (c == '-' && nextChar == '=') ||
                            (c == '*' && nextChar == '=') ||
                            (c == '/' && nextChar == '=') ||
                            (c == '%' && nextChar == '=')) {
                            op += nextChar;
                            i++; // advance extra for two character operator
                        }
                    }
                    
                    result.Tokens.Add(new Token(TokenType.Operator, op, line));
                    i++;
                    continue;
                }

                // handle symbols
                if ("(){}[];:,.".Contains(c)) {
                    result.Tokens.Add(new Token(TokenType.Symbol, c.ToString(), line));
                    i++;
                    continue;
                }
                
                // if the character is unrecognized, set an error and continue
                result.Error = new WeftError(ErrorPhase.Lex, $"Unexpected character: {c}", line);
                i++;
            }
    
            return result;
        }
        
        // checks if a word is a keyword
        private static bool IsKeyword(string word) {
            return Keywords.Contains(word);
        }
    }
}
