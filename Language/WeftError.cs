namespace Weft.Language {
    public enum ErrorPhase { Lex, Parse, Compile, Runtime }
    
    public sealed class WeftError {
        public ErrorPhase Phase { get; }
        public string Message { get; }
        public int Line { get; }
        public string[] StackTrace { get; } // runtime only

        public WeftError(ErrorPhase phase, string message, int line, string[] stackTrace = null) {
            Phase = phase;
            Message = message;
            Line = line;
            StackTrace = stackTrace;
        }

        public override string ToString() {
            var prefix = Phase switch {
                ErrorPhase.Lex => "Syntax",
                ErrorPhase.Parse => "Parse",
                ErrorPhase.Compile => "Compile",
                ErrorPhase.Runtime => "Runtime",
                _ => "Error"
            };
            
            var msg = $"{prefix} error (line {Line}): {Message}";
            
            if (StackTrace is { Length: > 0 })
                msg += "\n " + string.Join("\n ", StackTrace);
            
            return msg;
        }
    }
}