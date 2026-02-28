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

        private int sp;
        private int pc;
        private WeftChunk chunk;
        private ScriptContext context;
        private int frameCount;

        private readonly object[] globals = new object[MaxGlobals];
        private readonly object[] stack = new object[MaxStack];
        private readonly CallFrame[] frames = new CallFrame[MaxFrames];
        private readonly List<UpvalueCell> openUpvalues = new();

        public bool Completed { get; private set; }
        
        private static readonly object[][] ArgPool = {
            Array.Empty<object>(),
            new object[1],
            new object[2],
            new object[3],
            new object[4],
            new object[5],
            new object[6],
            new object[7],
            new object[8],
        };

        private struct CallFrame {
            public int returnPc;
            public int baseSlot;
            public int funcStartPc;
            public UpvalueCell[] upvalues;
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

            var code = chunk.Code;
            var constants = chunk.Constants;
            var gas = gasBudget;

            while (pc < code.Length) {
                if (--gas <= 0)
                    return ExecutionResult.YieldUntil(0);

                var instrPc = pc;

                if (sp >= stack.Length)
                    return MakeError("Stack overflow", instrPc);

                var op = (Op)code[pc++];

                switch (op) {
                    case Op.InsertUnder: {
                        var offset = code[pc++];
                        if (offset > 0) {
                            var top = stack[sp - 1];
 
                            for (var i = 1; i <= offset; i++) {
                                stack[sp - i] = stack[sp - i - 1];
                            }

                            stack[sp - offset - 1] = top;
                        }
                        break;
                    }
                    
                    case Op.Dup2: {
                        if (sp < 2) return MakeError("Stack underflow on dup2", instrPc);
                        if (!CheckStack(2, instrPc, out var e)) return e;
                        
                        stack[sp] = stack[sp - 2];
                        stack[sp + 1] = stack[sp - 1];
                        sp += 2;
                        
                        break;
                    }
                    
                    case Op.Dup: {
                        if (sp < 1) return MakeError("Stack underflow on dup", instrPc);
                        stack[sp] = stack[sp - 1];
                        sp++;
                        break;
                    }

                    case Op.Closure: {
                        var funcPc = code[pc++];
                        var arity = code[pc++];
                        var upCount = code[pc++];
                        if (!CheckStack(1, instrPc, out var e)) return e;
                        
                        var ups = new UpvalueCell[upCount];

                        for (var i = 0; i < upCount; i++) {
                            var isLocal = code[pc++] == 1;
                            var index = code[pc++];

                            if (isLocal) {
                                ups[i] = CaptureUpvalue(frames[frameCount - 1].baseSlot + index);
                            } 
                            else {
                                var parentUps = frames[frameCount - 1].upvalues;
                                
                                if (parentUps == null || index < 0 || index >= parentUps.Length)
                                    return MakeError($"Invalid upvalue index {index} in closure", instrPc);
                                
                                ups[i] = parentUps[index];
                            }
                        }

                        stack[sp++] = new WeftClosure(funcPc, arity, ups);
                        break;
                    }

                    case Op.CallClosure: {
                        var argc = code[pc++];
                        var obj = stack[--sp]; // pop closure from top

                        if (obj is not WeftClosure closure)
                            return MakeError("Cannot call non-function value", instrPc);

                        var expectedArity = closure.arity;
                        var baseSlot = sp - argc;
                        
                        if (!CheckStack(expectedArity - argc, instrPc, out var e)) return e;

                        while (argc < expectedArity) {
                            stack[sp++] = null;
                            argc++;
                        }
                        
                        while (argc > expectedArity) {
                            sp--;
                            argc--;
                        }
                        
                        if (frameCount >= frames.Length)
                            return MakeError("Stack overflow: too many nested calls", instrPc);

                        frames[frameCount++] = new CallFrame {
                            returnPc = pc,
                            baseSlot = baseSlot,
                            funcStartPc = closure.funcPc,
                            upvalues = closure.upvalues
                        };

                        pc = closure.funcPc;
                        break;
                    }

                    case Op.LoadUpvalue: {
                        var idx = code[pc++];
                        var upvalues = frames[frameCount - 1].upvalues;
                        
                        if (upvalues == null || idx < 0 || idx >= upvalues.Length)
                            return MakeError($"Invalid upvalue index {idx}", instrPc);
                        
                        var cell = upvalues[idx];
                        stack[sp++] = cell.isClosed ? cell.value : stack[cell.location];
                        break;
                    }

                    case Op.StoreUpvalue: {
                        var idx = code[pc++];
                        var upvalues = frames[frameCount - 1].upvalues;
                        
                        if (upvalues == null || idx < 0 || idx >= upvalues.Length)
                            return MakeError($"Invalid upvalue index {idx}", instrPc);
                        
                        var cell = upvalues[idx];
                        
                        if (cell.isClosed)
                            cell.value = stack[sp - 1];
                        else
                            stack[cell.location] = stack[sp - 1];
                        
                        break;
                    }

                    case Op.CloseUpvalues: {
                        var fromSlot = code[pc++];
                        CloseUpvaluesFrom(frames[frameCount - 1].baseSlot + fromSlot);
                        break;
                    }
                    
                    case Op.Peek: {
                        var offset = code[pc++];
                        if (!CheckStack(1, instrPc, out var e)) return e;
                        
                        var targetSlot = sp - 1 - offset;
                        
                        if (targetSlot < frames[frameCount - 1].baseSlot || targetSlot >= sp)
                            return MakeError("Invalid stack peek", instrPc);
                        
                        stack[sp++] = stack[targetSlot];
                        break;
                    }

                    case Op.Poke: {
                        var offset = code[pc++];
                        var targetSlot = sp - 1 - offset;
                        
                        if (targetSlot < frames[frameCount - 1].baseSlot || targetSlot >= sp)
                            return MakeError("Invalid stack poke", instrPc);
                        
                        stack[targetSlot] = stack[sp - 1];
                        break;
                    }

                    case Op.CallFunc:
                        var startPc = code[pc++];
                        var funcArity = code[pc++];

                        if (frameCount >= frames.Length)
                            return MakeError("Stack overflow: too many nested calls", instrPc);

                        frames[frameCount++] = new CallFrame {
                            returnPc = pc,
                            baseSlot = sp - funcArity,
                            funcStartPc = startPc
                        };

                        pc = startPc;
                        break;

                    case Op.Return:
                        if (sp < 1) return MakeError("Stack underflow on return", instrPc);
                        if (frameCount <= 1) return MakeError("Return outside of function", instrPc);

                        var retVal = stack[--sp];
                        var frame = frames[--frameCount];
                        
                        CloseUpvaluesFrom(frame.baseSlot);

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
                        if (sp < 2) return MakeError("Stack underflow", instrPc);
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
                        if (sp < 2) return MakeError("Stack underflow", instrPc);
                        
                        var b = stack[--sp];
                        var a = stack[--sp];
                        
                        stack[sp++] = Equals(a, b);
                        break;
                    }
                    case Op.Neq: {
                        if (sp < 2) return MakeError("Stack underflow", instrPc);
                        
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

                        stack[sp - 1] = !IsTruthy(stack[sp - 1]);

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

                        var args = argc < ArgPool.Length ? ArgPool[argc] : new object[argc];
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
                                        stack[sp++] = ys.ReturnValue;
                                        return ExecutionResult.YieldUntil(time.Now + ys.Seconds);
                                    }
                                    case YieldForProcess yp:
                                        stack[sp++] = null;
                                        return ExecutionResult.YieldUntilPid(yp.TargetPid);
                                }
                            }

                            if (!CheckStack(1, instrPc, out var stackErr)) return stackErr;
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

            var line = errorPc < chunk.Lines.Length ? chunk.Lines[errorPc] : 0;
            var currentFunc = frameCount > 0 ? frames[frameCount - 1].funcStartPc : -1;
            trace.Add($"at {ResolveFuncName(currentFunc)} (line {line})");

            var remaining = frameCount - 1;
            var show = remaining > maxTraceLines ? maxTraceLines - 1 : remaining;

            for (var i = frameCount - 1; i >= frameCount - show; i--) {
                var retPc = frames[i].returnPc;

                var callerLine = retPc > 0 && retPc < chunk.Lines.Length
                    ? chunk.Lines[retPc - 1]
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
            var line = atPc < chunk.Lines.Length ? chunk.Lines[atPc] : 0;
            var trace = BuildStackTrace(atPc);
            var err = new WeftError(ErrorPhase.Runtime, msg, line, trace);

            return ExecutionResult.ErrorResult(err.ToString());
        }

        private UpvalueCell CaptureUpvalue(int slot) {
            for (var i = 0; i < openUpvalues.Count; i++) {
                if (!openUpvalues[i].isClosed && openUpvalues[i].location == slot)
                    return openUpvalues[i];
            }

            var cell = new UpvalueCell(slot);

            openUpvalues.Add(cell);
            return cell;
        }

        private void CloseUpvaluesFrom(int fromSlot) {
            for (var i = openUpvalues.Count - 1; i >= 0; i--) {
                var cell = openUpvalues[i];
                if (cell.isClosed || cell.location < fromSlot) continue;

                cell.value = stack[cell.location];
                cell.isClosed = true;

                openUpvalues.RemoveAt(i);
            }
        }
        
        private bool CheckStack(int needed, int instrPc, out ExecutionResult err) {
            if (sp + needed <= MaxStack) {
                err = default; 
                return true;
            }
            
            err = MakeError("Stack overflow", instrPc);
            
            return false;
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