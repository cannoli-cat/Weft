using System.Collections.Generic;

namespace Weft.Language.Compilation {
    public class WeftChunk {
        public readonly List<int> code = new();
        public readonly List<object> constants = new();
        public readonly List<int> lines = new();
        public readonly Dictionary<int, string> funcNames = new(); // pc -> name
        private readonly Dictionary<double, int> doubleMap = new();
        private readonly Dictionary<string, int> stringMap = new();
        private readonly Dictionary<bool, int> boolMap = new();
        
        public int[] Code { get; private set; }
        public object[] Constants { get; private set; }
        public int[] Lines { get; private set; }
        
        public int AddConstant(object value) {
            switch (value) {
                case double d:
                    if (doubleMap.TryGetValue(d, out var di)) return di;
                    di = constants.Count;
                    constants.Add(d);
                    doubleMap[d] = di;
                    return di;
                case string s:
                    if (stringMap.TryGetValue(s, out var si)) return si;
                    si = constants.Count;
                    constants.Add(s);
                    stringMap[s] = si;
                    return si;
                case bool b:
                    if (boolMap.TryGetValue(b, out var bi)) return bi;
                    bi = constants.Count;
                    constants.Add(b);
                    boolMap[b] = bi;
                    return bi;
                case null:
                    var ni = constants.Count;
                    constants.Add(null);
                    return ni;
                default:
                    var idx = constants.Count;
                    constants.Add(value);
                    return idx;
            }
        }

        public void Emit(Op op, int line) {
            code.Add((int)op);
            lines.Add(line);
        }

        public void Emit(Op op, int operand, int line) {
            code.Add((int)op);
            lines.Add(line);
            
            code.Add(operand);
            lines.Add(line);
        }

        /// <summary>
        /// Emit an opcode with two operands (used for Call: nameIdx + argCount).
        /// </summary>
        public void Emit(Op op, int operand1, int operand2, int line) {
            code.Add((int)op);
            lines.Add(line);
            
            code.Add(operand1);
            lines.Add(line);
            
            code.Add(operand2);
            lines.Add(line);
        }
        
        public void Freeze() {
            Code = code.ToArray();
            Constants = constants.ToArray();
            Lines = lines.ToArray();
        }
    }
}