using System.Collections.Generic;

namespace Weft.Language.Compilation {
    public class WeftChunk {
        public readonly List<int> code = new();
        public readonly List<object> constants = new();
        public readonly List<int> lines = new();

        public int AddConstant(object value) {
            for (var i = 0; i < constants.Count; i++) {
                if (Equals(constants[i], value))
                    return i;
            }
            
            constants.Add(value);
            return constants.Count - 1;
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
    }
}