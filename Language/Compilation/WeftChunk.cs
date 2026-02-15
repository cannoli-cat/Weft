using System.Collections.Generic;

namespace Weft.Language.Compilation {
    public class WeftChunk {
        public readonly List<int> code = new();
        public readonly List<object> constants = new();

        public int AddConstant(object value) {
            // deduplicate constants to save space
            for (var i = 0; i < constants.Count; i++) {
                if (Equals(constants[i], value))
                    return i;
            }
            constants.Add(value);
            return constants.Count - 1;
        }

        public void Emit(Op op) => code.Add((int)op);

        public void Emit(Op op, int operand) {
            code.Add((int)op);
            code.Add(operand);
        }

        /// <summary>
        /// Emit an opcode with two operands (used for Call: nameIdx + argCount).
        /// </summary>
        public void Emit(Op op, int operand1, int operand2) {
            code.Add((int)op);
            code.Add(operand1);
            code.Add(operand2);
        }
    }
}