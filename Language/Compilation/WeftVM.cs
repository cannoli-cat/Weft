using System;
using Weft.Language.Runtime;
using Weft.Runtime.Binding;
using Weft.Runtime.Scheduling;
using Weft.Runtime.Services;
using Weft.Unity.Engine;

namespace Weft.Language.Compilation {
    public class WeftVM {
        private readonly object[] stack = new object[512];
        private int sp;
        private int pc;
        private WeftChunk chunk;
        private ScriptContext context;
        private CallFrame[] frames = new CallFrame[64];
        private int frameCount;

        public bool Completed { get; private set; }

        private struct CallFrame {
            public int returnPc;
            public int baseSlot;
        }

        public void Load(WeftChunk compiled, ScriptContext ctx) {
            chunk = compiled;
            context = ctx;
            pc = 0;
            sp = 0;
            Completed = false;
            frameCount = 1;
            frames[0] = new CallFrame { returnPc = -1, baseSlot = 0};
        }

        /// <summary>
        /// Run the VM with a gas budget. Returns when gas runs out, a yield
        /// occurs, an error happens, or the program halts.
        /// Call again to resume after a yield.
        /// </summary>
        public ExecutionResult Step(int gasBudget) {
            if (Completed)
                return ExecutionResult.SuccessResult();

            var code = chunk.code;
            var constants = chunk.constants;
            var gas = gasBudget;

            while (pc < code.Count) {
                if (--gas <= 0)
                    return ExecutionResult.ErrorResult($"[Line: {chunk.lines[pc]}] Gas limit exceeded.");

                var instrPc = pc; 
                var op = (Op)code[pc++];

                switch (op) {
                    case Op.CallFunc:
                        var startPc = code[pc++];
                        var arity = code[pc++];
                        
                        frames[frameCount++] = new CallFrame {
                            returnPc = pc,
                            baseSlot = sp - arity
                        };
                        
                        pc = startPc;
                        break;
                    
                    case Op.Return:
                        var retVal = stack[--sp];
                        var frame = frames[--frameCount];
                        
                        sp = frame.baseSlot;
                        pc = frame.returnPc;
                        
                        stack[sp++] = retVal;
                        break;
                    
                    case Op.Const:
                        stack[sp++] = constants[code[pc++]];
                        break;

                    case Op.Pop:
                        sp--;
                        break;

                    case Op.LoadLocal:
                        stack[sp++] = stack[frames[frameCount - 1].baseSlot + code[pc++]];
                        break;

                    case Op.StoreLocal:
                        stack[frames[frameCount - 1].baseSlot + code[pc++]] = stack[sp - 1];
                        break;

                    case Op.Add: {
                        var b = stack[--sp];
                        var a = stack[--sp];
                        if (a is double da && b is double db)
                            stack[sp++] = da + db;
                        else
                            stack[sp++] = (a?.ToString() ?? "null") + (b?.ToString() ?? "null");
                        break;
                    }
                    case Op.Sub: {
                        var b = (double)stack[--sp];
                        var a = (double)stack[--sp];
                        stack[sp++] = a - b;
                        break;
                    }
                    case Op.Mul: {
                        var b = (double)stack[--sp];
                        var a = (double)stack[--sp];
                        stack[sp++] = a * b;
                        break;
                    }
                    case Op.Div: {
                        var b = (double)stack[--sp];
                        var a = (double)stack[--sp];
                        if (b == 0) return ExecutionResult.ErrorResult($"[Line: {chunk.lines[instrPc]}] Division by zero.");
                        stack[sp++] = a / b;
                        break;
                    }
                    case Op.Mod: {
                        var b = (double)stack[--sp];
                        var a = (double)stack[--sp];
                        stack[sp++] = a % b;
                        break;
                    }
                    case Op.Negate:
                        stack[sp - 1] = -(double)stack[sp - 1];
                        break;

                    case Op.Eq: {
                        var b = stack[--sp];
                        var a = stack[--sp];
                        stack[sp++] = Equals(a, b);
                        break;
                    }
                    case Op.Neq: {
                        var b = stack[--sp];
                        var a = stack[--sp];
                        stack[sp++] = !Equals(a, b);
                        break;
                    }
                    case Op.Lt: {
                        var b = (double)stack[--sp];
                        var a = (double)stack[--sp];
                        stack[sp++] = a < b;
                        break;
                    }
                    case Op.Gt: {
                        var b = (double)stack[--sp];
                        var a = (double)stack[--sp];
                        stack[sp++] = a > b;
                        break;
                    }
                    case Op.Lte: {
                        var b = (double)stack[--sp];
                        var a = (double)stack[--sp];
                        stack[sp++] = a <= b;
                        break;
                    }
                    case Op.Gte: {
                        var b = (double)stack[--sp];
                        var a = (double)stack[--sp];
                        stack[sp++] = a >= b;
                        break;
                    }
                    case Op.Not:
                        stack[sp - 1] = !(bool)stack[sp - 1];
                        break;

                    case Op.Jump:
                        pc = code[pc];
                        break;

                    case Op.JumpIfFalse: {
                        var target = code[pc++];
                        var val = stack[--sp];
                        if (val is bool bv && !bv)
                            pc = target;
                        break;
                    }

                    case Op.JumpIfTrue: {
                        var target = code[pc++];
                        var val = stack[--sp];
                        if (val is bool bv && bv)
                            pc = target;
                        break;
                    }

                    case Op.Call: {
                        var nameIdx = code[pc++];
                        var argc = code[pc++];
                        var funcName = (string)constants[nameIdx];
                        
                        var args = new object[argc];
                        for (var i = argc - 1; i >= 0; i--)
                            args[i] = stack[--sp];

                        if (!WeftRegistry.TryGet(funcName, out var hostFunc))
                            return ExecutionResult.ErrorResult($"[Line: {chunk.lines[instrPc]}] Unknown function: {funcName}");

                        try {
                            var ret = hostFunc(context, args);

                            // handle yield requests
                            if (ret is IYieldRequest y) {
                                switch (y) {
                                    case YieldForSeconds ys: {
                                        var time = context.Resolve<ITimeService>();
                                        stack[sp++] = null;
                                        return ExecutionResult.YieldUntil(time.Now + ys.Seconds);
                                    }
                                    case YieldForProcess yp:
                                        stack[sp++] = null;
                                        return ExecutionResult.YieldUntilPid(yp.TargetPid);
                                }
                            }

                            stack[sp++] = ret;
                        }
                        catch (Exception ex) {
                            return ExecutionResult.ErrorResult($"[Line: {chunk.lines[instrPc]}] Function '{funcName}' failed: {ex.Message}");
                        }
                        break;
                    }

                    case Op.Halt:
                        Completed = true;
                        return ExecutionResult.SuccessResult();

                    default:
                        return ExecutionResult.ErrorResult($"[Line: {chunk.lines[instrPc]}] Unknown opcode: {op}");
                }
            }
            
            Completed = true;
            return ExecutionResult.SuccessResult();
        }

        private new static bool Equals(object a, object b) {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;
            return a.Equals(b);
        }
    }
}