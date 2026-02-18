using System;
using System.Collections.Generic;
using Weft.Language.Runtime;
using Weft.Runtime.Binding;
using Weft.Runtime.Scheduling;
using Weft.Runtime.Services;
using Weft.Unity.Engine;

namespace Weft.Language.Compilation {
    public class WeftVM {
        private const int MaxStack = 512;
        private const int MaxFrames = 64;
        private const int MaxGlobals = 256;
        
        private readonly object[] stack = new object[512];
        private int sp;
        private int pc;
        private WeftChunk chunk;
        private ScriptContext context;
        private CallFrame[] frames = new CallFrame[64];
        private int frameCount;
        
        private readonly object[] globals = new object[256];

        public bool Completed { get; private set; }

        private struct CallFrame {
            public int returnPc;
            public int baseSlot;
            public int funcStartPc;
        }

        public void Load(WeftChunk compiled, ScriptContext ctx) {
            chunk = compiled;
            context = ctx;
            pc = 0;
            sp = 0;
            Completed = false;
            frameCount = 1;
            frames[0] = new CallFrame { returnPc = -1, baseSlot = 0, funcStartPc = -1 };
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
                    return MakeError("Gas limit exceeded", pc);

                var instrPc = pc; 
                
                if (sp >= stack.Length)
                    return MakeError("Stack overflow", instrPc);
                
                var op = (Op)code[pc++];

                switch (op) {
                    case Op.CallFunc:
                        var startPc = code[pc++];
                        var arity = code[pc++];
    
                        if (frameCount >= frames.Length)
                            return MakeError("Stack overflow: too many nested calls", instrPc);
    
                        frames[frameCount++] = new CallFrame {
                            returnPc = pc,
                            baseSlot = sp - arity,
                            funcStartPc = startPc
                        };
    
                        pc = startPc;
                        break;
                    
                    case Op.Return:
                        if (sp < 1) return MakeError("Stack underflow on return", instrPc);
                        if (frameCount <= 1) return MakeError("Return outside of function", instrPc);
    
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
                        if (sp < 2) return MakeError("Stack underflow", instrPc);
                        
                        var b = stack[--sp];
                        var a = stack[--sp];
                        
                        if (a is not double da || b is not double db)
                            return MakeError("Cannot subtract non-numeric values", instrPc);
                        
                        stack[sp++] = da - db;
                        
                        break;
                    }
                    case Op.Mul: {
                        if (sp < 2) 
                            return MakeError("Stack underflow", instrPc);
                        
                        var b = stack[--sp];
                        var a = stack[--sp];
                        
                        if (a is not double da || b is not double db)
                            return MakeError("Cannot multiply non-numeric values", instrPc);
                        
                        stack[sp++] = da * db;
                        
                        break;
                    }
                    case Op.Div: {
                        if (sp < 2) return MakeError("Stack underflow", instrPc);
                        
                        var b = stack[--sp];
                        var a = stack[--sp];
                        
                        if (a is not double da || b is not double db)
                            return MakeError("Cannot divide non-numeric values", instrPc);
                        
                        if (db == 0)
                            return MakeError("Division by zero", instrPc);
                        
                        stack[sp++] = da / db;
                        
                        break;
                    }
                    case Op.Mod: {
                        if (sp < 2) return MakeError("Stack underflow", instrPc);
                        
                        var b = stack[--sp];
                        var a = stack[--sp];
                        
                        if (a is not double da || b is not double db)
                            return MakeError("Cannot modulo non-numeric values", instrPc);
                        
                        stack[sp++] = da % db;
                        
                        break;
                    }
                    case Op.Negate: {
                        if (sp < 1) return MakeError("Stack underflow", instrPc);
                        
                        if (stack[sp - 1] is not double d)
                            return MakeError("Cannot negate a non-numeric value", instrPc);
                        
                        stack[sp - 1] = -d;
                        
                        break;
                    }
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
                        if (sp < 2) return MakeError("Stack underflow", instrPc);
                        
                        var b = stack[--sp];
                        var a = stack[--sp];
                        
                        if (a is not double da || b is not double db)
                            return MakeError("Cannot compare non-numeric values with '<'", instrPc);
                        
                        stack[sp++] = da < db;
                        
                        break;
                    }
                    case Op.Gt: {
                        if (sp < 2) return MakeError("Stack underflow", instrPc);
                        
                        var b = stack[--sp];
                        var a = stack[--sp];
                        
                        if (a is not double da || b is not double db)
                            return MakeError("Cannot compare non-numeric values with '>'", instrPc);
                        
                        stack[sp++] = da > db;
                        
                        break;
                    }
                    case Op.Lte: {
                        if (sp < 2) return MakeError("Stack underflow", instrPc);
                        
                        var b = stack[--sp];
                        var a = stack[--sp];
                        
                        if (a is not double da || b is not double db)
                            return MakeError("Cannot compare non-numeric values with '<='", instrPc);
                        
                        stack[sp++] = da <= db;
                        
                        break;
                    }
                    case Op.Gte: {
                        if (sp < 2) return MakeError("Stack underflow", instrPc);
                        
                        var b = stack[--sp];
                        var a = stack[--sp];
                        
                        if (a is not double da || b is not double db)
                            return MakeError("Cannot compare non-numeric values with '>='", instrPc);
                        
                        stack[sp++] = da >= db;
                        
                        break;
                    }
                    case Op.Not: {
                        if (sp < 1) return MakeError("Stack underflow", instrPc);
                        
                        if (stack[sp - 1] is not bool bv)
                            return MakeError("Cannot apply '!' to a non-boolean value", instrPc);
                        
                        stack[sp - 1] = !bv;
                        
                        break;
                    }

                    case Op.Jump:
                        pc = code[pc];
                        break;

                    case Op.JumpIfFalse: {
                        var target = code[pc++];
                        var val = stack[--sp];
                        
                        if (!IsTruthy(val))
                            pc = target;
                        break;
                    }

                    case Op.JumpIfTrue: {
                        var target = code[pc++];
                        var val = stack[--sp];
                        
                        if (IsTruthy(val))
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
                            return MakeError($"Unknown function '{funcName}'", instrPc);

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
                            return MakeError($"Function '{funcName}' failed: {ex.Message}", instrPc);
                        }
                        break;
                    }

                    case Op.Halt:
                        Completed = true;
                        return ExecutionResult.SuccessResult();
                    
                    case Op.LoadGlobal: {
                        var slot = code[pc++];
                        
                        if (slot < 0 || slot >= globals.Length)
                            return MakeError($"Global variable index {slot} out of range", instrPc);
                        
                        stack[sp++] = globals[slot];
                        
                        break;
                    }

                    case Op.StoreGlobal: {
                        var slot = code[pc++];
                        
                        if (slot < 0 || slot >= globals.Length)
                            return MakeError($"Global variable index {slot} out of range", instrPc);
                        
                        globals[slot] = stack[sp - 1];
                        
                        break;
                    }

                    default:
                        return MakeError($"Unknown opcode: {op}", instrPc);
                }
            }
            
            Completed = true;
            return ExecutionResult.SuccessResult();
        }
        
        private string ResolveFuncName(int funcPc) {
            if (funcPc >= 0 && chunk.funcNames.TryGetValue(funcPc, out var name))
                return name;
            return "<script>";
        }

        private string[] BuildStackTrace(int errorPc) {
            var trace = new List<string>();
            const int maxTraceLines = 8;

            var line = errorPc < chunk.lines.Count ? chunk.lines[errorPc] : 0;
            var currentFunc = frameCount > 0 ? frames[frameCount - 1].funcStartPc : -1;
            trace.Add($"at {ResolveFuncName(currentFunc)} (line {line})");

            var remaining = frameCount - 1;
            var show = remaining > maxTraceLines ? maxTraceLines - 1 : remaining;

            for (var i = frameCount - 1; i >= frameCount - show; i--) {
                var retPc = frames[i].returnPc;
                
                var callerLine = retPc > 0 && retPc < chunk.lines.Count
                    ? chunk.lines[retPc - 1]
                    : 0;
                
                var callerFunc = frames[i - 1].funcStartPc;
                
                trace.Add($"at {ResolveFuncName(callerFunc)} (line {callerLine})");
            }

            var omitted = remaining - show;
            if (omitted > 0)
                trace.Add($"... {omitted} more frames");

            return trace.ToArray();
        }

        private ExecutionResult MakeError(string msg, int atPc) {
            var line = atPc < chunk.lines.Count ? chunk.lines[atPc] : 0;
            var trace = BuildStackTrace(atPc);
            var err = new WeftError(ErrorPhase.Runtime, msg, line, trace);
            
            return ExecutionResult.ErrorResult(err.ToString());
        }
        
        private static bool IsTruthy(object val) {
            if (val == null) return false;
            if (val is bool b) return b;
            if (val is double d) return d != 0;
            
            return true; 
        }

        private new static bool Equals(object a, object b) {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;
            return a.Equals(b);
        }
    }
}