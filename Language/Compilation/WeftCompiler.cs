using System.Collections.Generic;
using Weft.Language.AST;

namespace Weft.Language.Compilation {
    public class WeftCompiler {
        public WeftError Error { get; private set; }

        private WeftChunk chunk;
        private FuncScope current;
        private int currentLine, globalCount;

        private readonly Stack<LoopContext> loopStack = new();
        private readonly Dictionary<string, int> globals = new();

        private struct Local {
            public string name;
            public int depth;
            public bool isCaptured;
        }

        private struct UpvalueEntry {
            public int index;
            public bool isLocal;
        }

        private class FuncScope {
            public readonly List<Local> locals = new();
            public readonly List<UpvalueEntry> upvalues = new();
            public int scopeDepth;
            public readonly FuncScope enclosing;

            public FuncScope(FuncScope enclosing = null) {
                this.enclosing = enclosing;
            }
        }

        private struct LoopContext {
            public int continueTarget;
            public List<int> breakJumps;
        }

        public WeftChunk Compile(List<AstNode> program) {
            chunk = new WeftChunk();
            globals.Clear();
            globalCount = 0;
            loopStack.Clear();
            current = new FuncScope();

            foreach (var node in program) {
                if (node is FuncDeclNode fd)
                    globals[fd.Name] = globalCount++;
                else if (node is VarNode v)
                    globals[v.Name] = globalCount++;
            }

            foreach (var node in program)
                if (node is FuncDeclNode fd)
                    CompileFuncDecl(fd);

            foreach (var node in program)
                if (node is not FuncDeclNode)
                    CompileStatement(node);

            Emit(Op.Halt);
            return chunk;
        }

        private void CompileStatement(AstNode node) {
            currentLine = node.Line;

            switch (node) {
                case FuncDeclNode fd:
                    CompileFuncDecl(fd);
                    break;

                case ReturnNode ret:
                    if (ret.Value != null) CompileExpression(ret.Value);
                    else Emit(Op.Const, chunk.AddConstant(null));

                    Emit(Op.Return);
                    break;

                case VarNode v:
                    CompileExpression(v.Value);

                    if (current.enclosing == null && current.scopeDepth == 0) {
                        if (!globals.TryGetValue(v.Name, out var slot)) {
                            slot = globalCount++;
                            globals[v.Name] = slot;
                        }

                        Emit(Op.StoreGlobal, slot);
                        Emit(Op.Pop);
                    }
                    else {
                        current.locals.Add(new Local { name = v.Name, depth = current.scopeDepth });
                    }

                    break;

                case AssignmentNode a:
                    CompileExpression(a.Value);
                    ResolveVariable(a.Name, isStore: true);
                    Emit(Op.Pop);
                    break;

                case ExprStmtNode es:
                    CompileExpression(es.Expr);
                    Emit(Op.Pop);
                    break;

                case IfNode ifn:
                    CompileIf(ifn);
                    break;

                case WhileNode w:
                    CompileWhile(w);
                    break;

                case ForNode f:
                    CompileFor(f);
                    break;

                case DoWhileNode dw:
                    CompileDoWhile(dw);
                    break;

                case BreakNode:
                    CompileBreak();
                    break;

                case ContinueNode:
                    CompileContinue();
                    break;

                case BlockNode block:
                    CompileBlock(block);
                    break;

                case IndexAssignNode idx:
                    CompileExpression(idx.Target);
                    CompileExpression(idx.Index);
                    CompileExpression(idx.Value);

                    var setIdx = chunk.AddConstant("__index_set");

                    Emit(Op.Call, setIdx, 3);
                    Emit(Op.Pop);

                    break;

                case FunctionCallNode call:
                    CompileExpression(call);
                    Emit(Op.Pop);
                    break;

                default:
                    SetError($"Compiler: unhandled statement node {node.GetType().Name}");
                    break;
            }
        }

        private void CompileExpression(AstNode node) {
            currentLine = node.Line;

            switch (node) {
                case FuncDeclNode fd:
                    EmitClosureBody(fd);
                    break;
                
                case IfNode ifn:
                    CompileExpression(ifn.Condition);

                    var jumpToElse = EmitJump(Op.JumpIfFalse);
                    CompileExpression(ifn.TrueBranch);

                    var jumpOverElse = EmitJump(Op.Jump);
                    PatchJump(jumpToElse);

                    CompileExpression(ifn.FalseBranch);
                    PatchJump(jumpOverElse);
                    break;

                case NullNode:
                    Emit(Op.Const, chunk.AddConstant(null));
                    break;

                case ObjectLiteralNode obj:
                    foreach (var (key, value) in obj.Entries) {
                        CompileExpression(key);
                        CompileExpression(value);
                    }

                    var objIdx = chunk.AddConstant("__object_new");
                    Emit(Op.Call, objIdx, obj.Entries.Count * 2);

                    break;

                case NumberNode n:
                    Emit(Op.Const, chunk.AddConstant(n.Value));
                    break;

                case StringNode s:
                    Emit(Op.Const, chunk.AddConstant(s.Value));
                    break;

                case BoolNode b:
                    Emit(Op.Const, chunk.AddConstant(b.Value));
                    break;

                case IdentifierNode id:
                    ResolveVariable(id.Name, isStore: false);
                    break;

                case UnaryNode u:
                    CompileExpression(u.Operand);
                    switch (u.Operator) {
                        case "-":
                            Emit(Op.Negate);
                            break;
                        case "!":
                            Emit(Op.Not);
                            break;
                        default:
                            SetError($"Unknown unary operator '{u.Operator}'");
                            break;
                    }

                    break;

                case IncDecNode inc:
                    CompileIncDec(inc);
                    break;

                case BinaryOperationNode bin:
                    CompileBinary(bin);
                    break;

                case FunctionCallNode call:
                    CompileCall(call);
                    break;

                case ArrayLiteralNode arr:
                    foreach (var elem in arr.Elements)
                        CompileExpression(elem);

                    var arrIdx = chunk.AddConstant("__array_new");
                    Emit(Op.Call, arrIdx, arr.Elements.Count);

                    break;

                case IndexAccessNode idx:
                    CompileExpression(idx.Target);
                    CompileExpression(idx.Index);

                    var getIdx = chunk.AddConstant("__index_get");
                    Emit(Op.Call, getIdx, 2);

                    break;

                case MemberAccessNode mem:
                    CompileExpression(mem.Target);
                    Emit(Op.Const, chunk.AddConstant(mem.Member));

                    var memIdx = chunk.AddConstant("__member_get");
                    Emit(Op.Call, memIdx, 2);

                    break;

                default:
                    SetError($"Compiler: unhandled expression node {node.GetType().Name}");
                    break;
            }
        }

        private void CompileBinary(BinaryOperationNode bin) {
            if (bin.Operator == "&&") {
                CompileExpression(bin.Left);
                var jumpToFalse = EmitJump(Op.JumpIfFalse);
                CompileExpression(bin.Right);
                var jumpToEnd = EmitJump(Op.Jump);
                PatchJump(jumpToFalse);
                Emit(Op.Const, chunk.AddConstant(false));
                PatchJump(jumpToEnd);
                return;
            }

            if (bin.Operator == "||") {
                CompileExpression(bin.Left);
                var jumpToTrue = EmitJump(Op.JumpIfTrue);
                CompileExpression(bin.Right);
                var jumpToEnd = EmitJump(Op.Jump);
                PatchJump(jumpToTrue);
                Emit(Op.Const, chunk.AddConstant(true));
                PatchJump(jumpToEnd);
                return;
            }

            CompileExpression(bin.Left);
            CompileExpression(bin.Right);

            switch (bin.Operator) {
                case "+":
                    Emit(Op.Add);
                    break;
                case "-":
                    Emit(Op.Sub);
                    break;
                case "*":
                    Emit(Op.Mul);
                    break;
                case "/":
                    Emit(Op.Div);
                    break;
                case "%":
                    Emit(Op.Mod);
                    break;
                case "==":
                    Emit(Op.Eq);
                    break;
                case "!=":
                    Emit(Op.Neq);
                    break;
                case "<":
                    Emit(Op.Lt);
                    break;
                case ">":
                    Emit(Op.Gt);
                    break;
                case "<=":
                    Emit(Op.Lte);
                    break;
                case ">=":
                    Emit(Op.Gte);
                    break;
                default:
                    SetError($"Unknown binary operator '{bin.Operator}'");
                    break;
            }
        }

        private void CompileIncDec(IncDecNode inc) {
            if (inc.IsPrefix) {
                ResolveVariable(inc.Name, isStore: false);

                Emit(Op.Const, chunk.AddConstant(1.0));
                Emit(inc.IsIncrement ? Op.Add : Op.Sub);

                ResolveVariable(inc.Name, isStore: true);
            }
            else {
                ResolveVariable(inc.Name, isStore: false);
                ResolveVariable(inc.Name, isStore: false);

                Emit(Op.Const, chunk.AddConstant(1.0));
                Emit(inc.IsIncrement ? Op.Add : Op.Sub);

                ResolveVariable(inc.Name, isStore: true);
                Emit(Op.Pop);
            }
        }

        private void CompileCall(FunctionCallNode call) {
            foreach (var arg in call.Arguments)
                CompileExpression(arg);

            if (IsKnownVariable(call.FunctionName)) {
                ResolveVariable(call.FunctionName, isStore: false);
                Emit(Op.CallClosure, call.Arguments.Count);
            }
            else {
                var nameIdx = chunk.AddConstant(call.FunctionName);
                Emit(Op.Call, nameIdx, call.Arguments.Count);
            }
        }

        private void CompileIf(IfNode ifn) {
            CompileExpression(ifn.Condition);
            var jumpToElse = EmitJump(Op.JumpIfFalse);

            if (ifn.TrueBranch is BlockNode tb)
                CompileBlock(tb);
            else
                CompileStatement(ifn.TrueBranch);

            if (ifn.FalseBranch != null) {
                var jumpOverElse = EmitJump(Op.Jump);
                PatchJump(jumpToElse);

                if (ifn.FalseBranch is BlockNode fb)
                    CompileBlock(fb);
                else
                    CompileStatement(ifn.FalseBranch);

                PatchJump(jumpOverElse);
            }
            else {
                PatchJump(jumpToElse);
            }
        }

        private void CompileWhile(WhileNode w) {
            var loopStart = chunk.code.Count;
            var ctx = PushLoop(loopStart);

            CompileExpression(w.Condition);
            var exitJump = EmitJump(Op.JumpIfFalse);

            CompileBlock(w.Body);
            Emit(Op.Jump, loopStart);
            PatchJump(exitJump);

            PopLoop(ctx);
        }

        private void CompileFor(ForNode f) {
            BeginScope();

            CompileStatement(f.Init);

            var loopStart = chunk.code.Count;

            var ctx = PushLoop(loopStart);

            CompileExpression(f.Condition);
            var exitJump = EmitJump(Op.JumpIfFalse);

            CompileBlock(f.Body);

            var stepStart = chunk.code.Count;
            ctx.continueTarget = stepStart;

            CompileStatement(f.Step);

            Emit(Op.Jump, loopStart);
            PatchJump(exitJump);

            PopLoop(ctx);
            EndScope();
        }

        private void CompileDoWhile(DoWhileNode dw) {
            var loopStart = chunk.code.Count;
            var ctx = PushLoop(loopStart);

            CompileBlock(dw.Body);

            ctx.continueTarget = chunk.code.Count;

            CompileExpression(dw.Condition);
            var jumpBack = EmitJump(Op.JumpIfTrue);
            PatchJump(jumpBack, loopStart);

            PopLoop(ctx);
        }

        private void CompileFuncDecl(FuncDeclNode fd) {
            EmitClosureBody(fd);

            if (current.enclosing == null && current.scopeDepth == 0) {
                Emit(Op.StoreGlobal, globals[fd.Name]);
                Emit(Op.Pop);
            }
            else {
                current.locals.Add(new Local { name = fd.Name, depth = current.scopeDepth });
            }
        }

        private void EmitClosureBody(FuncDeclNode fd) {
            var skipJump = EmitJump(Op.Jump);
            var funcStart = chunk.code.Count;

            var inner = new FuncScope(current);
            current = inner;

            foreach (var param in fd.Parameters)
                current.locals.Add(new Local { name = param, depth = 0 });

            foreach (var stmt in fd.Body.Statements)
                CompileStatement(stmt);

            Emit(Op.Const, chunk.AddConstant(null));
            Emit(Op.Return);

            var upvalues = new List<UpvalueEntry>(current.upvalues);
            current = current.enclosing;

            chunk.funcNames[funcStart] = fd.Name ?? "<anonymous>";
            PatchJump(skipJump);

            EmitRaw((int)Op.Closure);
            EmitRaw(funcStart);

            EmitRaw(fd.Parameters.Count);
            EmitRaw(upvalues.Count);

            foreach (var up in upvalues) {
                EmitRaw(up.isLocal ? 1 : 0);
                EmitRaw(up.index);
            }
        }

        private void CompileBreak() {
            if (loopStack.Count == 0) {
                SetError("'break' outside of loop");
                return;
            }

            var ctx = loopStack.Peek();
            var breakJump = EmitJump(Op.Jump);
            ctx.breakJumps.Add(breakJump);
        }

        private void CompileContinue() {
            if (loopStack.Count == 0) {
                SetError("'continue' outside of loop");
                return;
            }

            var ctx = loopStack.Peek();
            Emit(Op.Jump, ctx.continueTarget);
        }

        private void CompileBlock(BlockNode block) {
            BeginScope();
            foreach (var stmt in block.Statements)
                CompileStatement(stmt);
            EndScope();
        }

        private void BeginScope() {
            current.scopeDepth++;
        }

        private void EndScope() {
            current.scopeDepth--;

            var localsToRemove = 0;
            var hasCaptured = false;

            for (var i = current.locals.Count - 1; i >= 0; i--) {
                if (current.locals[i].depth <= current.scopeDepth) break;
                if (current.locals[i].isCaptured) hasCaptured = true;
                localsToRemove++;
            }

            if (hasCaptured) {
                var firstSlot = current.locals.Count - localsToRemove;
                Emit(Op.CloseUpvalues, firstSlot);
            }

            for (var i = 0; i < localsToRemove; i++) {
                Emit(Op.Pop);
                current.locals.RemoveAt(current.locals.Count - 1);
            }
        }

        private void ResolveVariable(string name, bool isStore) {
            for (var i = current.locals.Count - 1; i >= 0; i--) {
                if (current.locals[i].name == name) {
                    Emit(isStore ? Op.StoreLocal : Op.LoadLocal, i);
                    return;
                }
            }

            var upIdx = ResolveUpvalue(current, name);
            if (upIdx >= 0) {
                Emit(isStore ? Op.StoreUpvalue : Op.LoadUpvalue, upIdx);
                return;
            }

            if (globals.TryGetValue(name, out var slot)) {
                Emit(isStore ? Op.StoreGlobal : Op.LoadGlobal, slot);
                return;
            }

            SetError($"Undefined variable '{name}'");
        }

        private int EmitJump(Op op) {
            Emit(op, 0xFFFF);
            return chunk.code.Count - 1;
        }

        private void PatchJump(int operandIndex) {
            chunk.code[operandIndex] = chunk.code.Count;
        }

        private void PatchJump(int operandIndex, int target) {
            chunk.code[operandIndex] = target;
        }

        private LoopContext PushLoop(int continueTarget) {
            var ctx = new LoopContext {
                continueTarget = continueTarget,
                breakJumps = new List<int>()
            };
            loopStack.Push(ctx);
            return ctx;
        }

        private void PopLoop(LoopContext ctx) {
            foreach (var jumpIdx in ctx.breakJumps)
                PatchJump(jumpIdx);

            loopStack.Pop();
        }

        private static int ResolveUpvalue(FuncScope scope, string name) {
            if (scope.enclosing == null) return -1;

            for (var i = scope.enclosing.locals.Count - 1; i >= 0; i--) {
                if (scope.enclosing.locals[i].name != name) continue;

                var local = scope.enclosing.locals[i];
                local.isCaptured = true;
                scope.enclosing.locals[i] = local;

                return AddUpvalue(scope, i, true);
            }

            var upIdx = ResolveUpvalue(scope.enclosing, name);
            if (upIdx >= 0)
                return AddUpvalue(scope, upIdx, false);

            return -1;
        }

        private static int AddUpvalue(FuncScope scope, int index, bool isLocal) {
            for (var i = 0; i < scope.upvalues.Count; i++) {
                var up = scope.upvalues[i];
                if (up.index == index && up.isLocal == isLocal)
                    return i;
            }

            scope.upvalues.Add(new UpvalueEntry { index = index, isLocal = isLocal });
            return scope.upvalues.Count - 1;
        }

        private bool IsKnownVariable(string name) {
            for (var i = current.locals.Count - 1; i >= 0; i--)
                if (current.locals[i].name == name)
                    return true;

            if (globals.ContainsKey(name)) return true;

            var scope = current.enclosing;
            while (scope != null) {
                for (var i = scope.locals.Count - 1; i >= 0; i--)
                    if (scope.locals[i].name == name)
                        return true;

                scope = scope.enclosing;
            }

            return false;
        }

        private void SetError(string msg) {
            if (Error == null)
                Error = new WeftError(ErrorPhase.Compile, msg, currentLine);
        }

        private void EmitRaw(int value) {
            chunk.code.Add(value);
            chunk.lines.Add(currentLine);
        }

        private void Emit(Op op) => chunk.Emit(op, currentLine);
        private void Emit(Op op, int operand) => chunk.Emit(op, operand, currentLine);
        private void Emit(Op op, int a, int b) => chunk.Emit(op, a, b, currentLine);
    }
}