using System;
using System.Collections.Generic;
using Weft.Language.AST;

namespace Weft.Language.Compilation {
    public class WeftCompiler {
        private WeftChunk chunk;
        private readonly List<Local> locals = new();
        private int scopeDepth, currentLine;
        
        private readonly Stack<LoopContext> loopStack = new();
        private readonly Dictionary<string, (int pc, int arity)> funcMetaData = new();

        private struct Local {
            public string name;
            public int depth;
        }

        private struct LoopContext {
            public int continueTarget;       
            public List<int> breakJumps;
        }

        public WeftChunk Compile(List<AstNode> program) {
            chunk = new WeftChunk();
            locals.Clear();
            scopeDepth = 0;
            loopStack.Clear();

            foreach (var node in program)
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
                    AddLocal(v.Name);
                    break;

                case AssignmentNode a:
                    CompileExpression(a.Value);
                    var assignSlot = ResolveLocal(a.Name);
                    
                    Emit(Op.StoreLocal, assignSlot);
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
                    throw new Exception($"Compiler: unhandled statement node {node.GetType().Name}");
            }
        }
        
        private void CompileExpression(AstNode node) {
            currentLine = node.Line;
            
            switch (node) {
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
                    Emit(Op.LoadLocal, ResolveLocal(id.Name));
                    break;

                case UnaryNode u:
                    CompileExpression(u.Operand);
                    switch (u.Operator) {
                        case "-": Emit(Op.Negate); break;
                        case "!": Emit(Op.Not); break;
                        default: throw new Exception($"Unknown unary operator '{u.Operator}'");
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
                    throw new Exception($"Compiler: unhandled expression node {node.GetType().Name}");
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
                case "+":  Emit(Op.Add); break;
                case "-":  Emit(Op.Sub); break;
                case "*":  Emit(Op.Mul); break;
                case "/":  Emit(Op.Div); break;
                case "%":  Emit(Op.Mod); break;
                case "==": Emit(Op.Eq); break;
                case "!=": Emit(Op.Neq); break;
                case "<":  Emit(Op.Lt); break;
                case ">":  Emit(Op.Gt); break;
                case "<=": Emit(Op.Lte); break;
                case ">=": Emit(Op.Gte); break;
                default: throw new Exception($"Unknown binary operator '{bin.Operator}'");
            }
        }
        
        private void CompileIncDec(IncDecNode inc) {
            var slot = ResolveLocal(inc.Name);

            if (inc.IsPrefix) {
                Emit(Op.LoadLocal, slot);
                Emit(Op.Const, chunk.AddConstant(1.0));
                Emit(inc.IsIncrement ? Op.Add : Op.Sub);
                Emit(Op.StoreLocal, slot);
            }
            else {
                Emit(Op.LoadLocal, slot);
                Emit(Op.LoadLocal, slot);
                Emit(Op.Const, chunk.AddConstant(1.0));
                Emit(inc.IsIncrement ? Op.Add : Op.Sub);
                Emit(Op.StoreLocal, slot);
                Emit(Op.Pop);
            }
        }

        private void CompileCall(FunctionCallNode call) {
            foreach (var arg in call.Arguments)
                CompileExpression(arg);

            if (funcMetaData.TryGetValue(call.FunctionName, out var meta)) {
                Emit(Op.CallFunc, meta.pc, meta.arity);
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
            var skipJump = EmitJump(Op.Jump);

            var funcStart = chunk.code.Count;
            funcMetaData[fd.Name] = (funcStart, fd.Parameters.Count);
            
            var outerLocals = new List<Local>(locals);
            var outerDepth = scopeDepth;
            locals.Clear();
            scopeDepth = 0;
            
            foreach (var param in fd.Parameters)
                AddLocal(param);
            
            foreach (var stmt in fd.Body.Statements)
                CompileStatement(stmt);
            
            Emit(Op.Const, chunk.AddConstant(null));
            Emit(Op.Return);
            
            locals.Clear();
            locals.AddRange(outerLocals);
            scopeDepth = outerDepth;

            PatchJump(skipJump);
        }

        private void CompileBreak() {
            if (loopStack.Count == 0)
                throw new Exception("'break' outside of loop");

            var ctx = loopStack.Peek();
            var breakJump = EmitJump(Op.Jump);
            ctx.breakJumps.Add(breakJump);
        }

        private void CompileContinue() {
            if (loopStack.Count == 0)
                throw new Exception("'continue' outside of loop");

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
            scopeDepth++;
        }

        private void EndScope() {
            scopeDepth--;

            while (locals.Count > 0 && locals[^1].depth > scopeDepth) {
                Emit(Op.Pop);
                locals.RemoveAt(locals.Count - 1);
            }
        }
        
        private void AddLocal(string name) {
            locals.Add(new Local { name = name, depth = scopeDepth });
        }

        private int ResolveLocal(string name) {
            for (var i = locals.Count - 1; i >= 0; i--) {
                if (locals[i].name == name)
                    return i;
            }
            throw new Exception($"Undefined variable: '{name}'");
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
        
        private void Emit(Op op) => chunk.Emit(op, currentLine);
        private void Emit(Op op, int operand) => chunk.Emit(op, operand, currentLine);
        private void Emit(Op op, int a, int b) => chunk.Emit(op, a, b, currentLine);
    }
}