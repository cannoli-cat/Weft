using System.Collections.Generic;

namespace Weft.Language.Compilation {
    public class WeftChunk {
        public readonly List<int> code = new();
        public readonly List<object> constants = new();
        public readonly List<int> lines = new();
        public readonly Dictionary<int, string> funcNames = new(); // pc -> name
        private readonly Dictionary<object, int> constantMap = new();
        
        public int[] Code { get; private set; }
        public object[] Constants { get; private set; }
        public int[] Lines { get; private set; }
        
        public int AddConstant(object value) {
            if (value != null && constantMap.TryGetValue(value, out var idx))
                return idx;

            var i = constants.Count;
            constants.Add(value);
            if (value != null) constantMap[value] = i;
            return i;
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