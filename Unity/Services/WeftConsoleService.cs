namespace Weft.Unity.Services {
    public sealed class WeftConsoleService {
        private readonly System.Action<string, bool> write;

        public WeftConsoleService(System.Action<string, bool> writer) => write = writer;
        
        public void Print(string m) => write?.Invoke(m, false);
        
        public void Clear() => write?.Invoke("__CLEAR__", false);
        
        public void Report(string m, bool isError) => write?.Invoke(m, isError);
    }
}