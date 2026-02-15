using System;
using System.Collections.Generic;
using Weft.Language.AST;

namespace Weft.Language.Compilation {
    public class WeftCompiler {
        private WeftChunk chunk;
        private readonly List<Local> locals = new();
        private int scopeDepth;

        // Break/continue support: stack of loop contexts
        private readonly Stack<LoopContext> loopStack = new();

        private struct Local {
            public string name;
            public int depth;
        }

        private struct LoopContext {
            public int continueTarget;        // pc to jump to on 'continue'
            public List<int> breakJumps;      // jump offsets to patch when loop ends
        }

        public WeftChunk Compile(List<AstNode> program) {
            chunk = new WeftChunk();
            locals.Clear();
            scopeDepth = 0;
            loopStack.Clear();

            foreach (var node in program)
                CompileStatement(node);

            chunk.Emit(Op.Halt);
            return chunk;
        }

        // ------------------------------------------------------------------
        //  Statements (don't leave values on stack)
        // ------------------------------------------------------------------

        private void CompileStatement(AstNode node) {
            switch (node) {
                case VarNode v:
                    CompileExpression(v.Value);
                    AddLocal(v.Name);
                    break;

                case AssignmentNode a:
                    CompileExpression(a.Value);
                    var assignSlot = ResolveLocal(a.Name);
                    chunk.Emit(Op.StoreLocal, assignSlot);
                    chunk.Emit(Op.Pop); // statement, discard the peeked value
                    break;

                case ExprStmtNode es:
                    CompileExpression(es.Expr);
                    chunk.Emit(Op.Pop); // discard expression result
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
                    // Use a host call: __index_set(target, index, value)
                    var setIdx = chunk.AddConstant("__index_set");
                    chunk.Emit(Op.Call, setIdx, 3);
                    chunk.Emit(Op.Pop);
                    break;

                // FunctionCallNode as a statement (e.g. print("hi");)
                case FunctionCallNode call:
                    CompileExpression(call); // pushes return value
                    chunk.Emit(Op.Pop);      // discard it
                    break;

                default:
                    throw new Exception($"Compiler: unhandled statement node {node.GetType().Name}");
            }
        }

        // ------------------------------------------------------------------
        //  Expressions (leave exactly one value on stack)
        // ------------------------------------------------------------------

        private void CompileExpression(AstNode node) {
            switch (node) {
                case NumberNode n:
                    chunk.Emit(Op.Const, chunk.AddConstant(n.Value));
                    break;

                case StringNode s:
                    chunk.Emit(Op.Const, chunk.AddConstant(s.Value));
                    break;

                case BoolNode b:
                    chunk.Emit(Op.Const, chunk.AddConstant(b.Value));
                    break;

                case IdentifierNode id:
                    chunk.Emit(Op.LoadLocal, ResolveLocal(id.Name));
                    break;

                case UnaryNode u:
                    CompileExpression(u.Operand);
                    switch (u.Operator) {
                        case "-": chunk.Emit(Op.Negate); break;
                        case "!": chunk.Emit(Op.Not); break;
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
                    // push element count, then each element, then call __array_new
                    foreach (var elem in arr.Elements)
                        CompileExpression(elem);
                    var arrIdx = chunk.AddConstant("__array_new");
                    chunk.Emit(Op.Call, arrIdx, arr.Elements.Count);
                    break;

                case IndexAccessNode idx:
                    CompileExpression(idx.Target);
                    CompileExpression(idx.Index);
                    var getIdx = chunk.AddConstant("__index_get");
                    chunk.Emit(Op.Call, getIdx, 2);
                    break;

                case MemberAccessNode mem:
                    CompileExpression(mem.Target);
                    chunk.Emit(Op.Const, chunk.AddConstant(mem.Member));
                    var memIdx = chunk.AddConstant("__member_get");
                    chunk.Emit(Op.Call, memIdx, 2);
                    break;

                default:
                    throw new Exception($"Compiler: unhandled expression node {node.GetType().Name}");
            }
        }

        // ------------------------------------------------------------------
        //  Binary operators
        // ------------------------------------------------------------------

        private void CompileBinary(BinaryOperationNode bin) {
            // Short-circuit: && and ||
            if (bin.Operator == "&&") {
                CompileExpression(bin.Left);
                var jumpToFalse = EmitJump(Op.JumpIfFalse);
                CompileExpression(bin.Right);
                var jumpToEnd = EmitJump(Op.Jump);
                PatchJump(jumpToFalse);
                chunk.Emit(Op.Const, chunk.AddConstant(false));
                PatchJump(jumpToEnd);
                return;
            }

            if (bin.Operator == "||") {
                CompileExpression(bin.Left);
                var jumpToTrue = EmitJump(Op.JumpIfTrue);
                CompileExpression(bin.Right);
                var jumpToEnd = EmitJump(Op.Jump);
                PatchJump(jumpToTrue);
                chunk.Emit(Op.Const, chunk.AddConstant(true));
                PatchJump(jumpToEnd);
                return;
            }

            CompileExpression(bin.Left);
            CompileExpression(bin.Right);

            switch (bin.Operator) {
                case "+":  chunk.Emit(Op.Add); break;
                case "-":  chunk.Emit(Op.Sub); break;
                case "*":  chunk.Emit(Op.Mul); break;
                case "/":  chunk.Emit(Op.Div); break;
                case "%":  chunk.Emit(Op.Mod); break;
                case "==": chunk.Emit(Op.Eq); break;
                case "!=": chunk.Emit(Op.Neq); break;
                case "<":  chunk.Emit(Op.Lt); break;
                case ">":  chunk.Emit(Op.Gt); break;
                case "<=": chunk.Emit(Op.Lte); break;
                case ">=": chunk.Emit(Op.Gte); break;
                default: throw new Exception($"Unknown binary operator '{bin.Operator}'");
            }
        }

        // ------------------------------------------------------------------
        //  Increment / Decrement (++i, i++, --i, i--)
        // ------------------------------------------------------------------

        private void CompileIncDec(IncDecNode inc) {
            var slot = ResolveLocal(inc.Name);

            if (inc.IsPrefix) {
                // ++i: load, add 1, store, result is new value (left on stack)
                chunk.Emit(Op.LoadLocal, slot);
                chunk.Emit(Op.Const, chunk.AddConstant(1.0));
                chunk.Emit(inc.IsIncrement ? Op.Add : Op.Sub);
                chunk.Emit(Op.StoreLocal, slot);
                // StoreLocal peeks, so the new value is still on top — that's our result
            }
            else {
                // i++: load old value (our result), then load again, add 1, store back
                chunk.Emit(Op.LoadLocal, slot);           // push old value (this is the expression result)
                chunk.Emit(Op.LoadLocal, slot);           // push again for computation
                chunk.Emit(Op.Const, chunk.AddConstant(1.0));
                chunk.Emit(inc.IsIncrement ? Op.Add : Op.Sub);
                chunk.Emit(Op.StoreLocal, slot);          // store new value
                chunk.Emit(Op.Pop);                        // pop the peeked new value from StoreLocal
                // old value is now on top as the expression result
            }
        }

        // ------------------------------------------------------------------
        //  Function calls
        // ------------------------------------------------------------------

        private void CompileCall(FunctionCallNode call) {
            foreach (var arg in call.Arguments)
                CompileExpression(arg);

            var nameIdx = chunk.AddConstant(call.FunctionName);
            chunk.Emit(Op.Call, nameIdx, call.Arguments.Count);
        }

        // ------------------------------------------------------------------
        //  Control flow
        // ------------------------------------------------------------------

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
                    CompileStatement(ifn.FalseBranch); // handles else-if chains
                
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
            chunk.Emit(Op.Jump, loopStart);
            PatchJump(exitJump);

            PopLoop(ctx);
        }

        private void CompileFor(ForNode f) {
            // for has its own scope for the init variable
            BeginScope();

            CompileStatement(f.Init);

            var loopStart = chunk.code.Count;
            // continue target is right before the step, but we don't know that yet
            // so we'll set it after compiling the body
            var ctx = PushLoop(loopStart); // temporary, we'll fix continue target

            CompileExpression(f.Condition);
            var exitJump = EmitJump(Op.JumpIfFalse);

            CompileBlock(f.Body);

            // This is where 'continue' should jump to (right before step)
            var stepStart = chunk.code.Count;
            ctx.continueTarget = stepStart;

            CompileStatement(f.Step);

            chunk.Emit(Op.Jump, loopStart);
            PatchJump(exitJump);

            PopLoop(ctx);
            EndScope();
        }

        private void CompileDoWhile(DoWhileNode dw) {
            var loopStart = chunk.code.Count;
            var ctx = PushLoop(loopStart);

            CompileBlock(dw.Body);

            // continue jumps to the condition check
            ctx.continueTarget = chunk.code.Count;

            CompileExpression(dw.Condition);
            var jumpBack = EmitJump(Op.JumpIfTrue);
            PatchJump(jumpBack, loopStart); // if true, go back to loopStart

            PopLoop(ctx);
        }

        private void CompileBreak() {
            if (loopStack.Count == 0)
                throw new Exception("'break' outside of loop");

            var ctx = loopStack.Peek();
            // Emit pops for any locals declared inside the loop body
            // (we'll need to clean up the stack)
            var breakJump = EmitJump(Op.Jump);
            ctx.breakJumps.Add(breakJump);
        }

        private void CompileContinue() {
            if (loopStack.Count == 0)
                throw new Exception("'continue' outside of loop");

            var ctx = loopStack.Peek();
            chunk.Emit(Op.Jump, ctx.continueTarget);
        }

        // ------------------------------------------------------------------
        //  Blocks and scoping
        // ------------------------------------------------------------------

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

            // Pop locals that belonged to the scope we're leaving
            while (locals.Count > 0 && locals[^1].depth > scopeDepth) {
                chunk.Emit(Op.Pop);
                locals.RemoveAt(locals.Count - 1);
            }
        }

        // ------------------------------------------------------------------
        //  Local variable tracking
        // ------------------------------------------------------------------

        private void AddLocal(string name) {
            locals.Add(new Local { name = name, depth = scopeDepth });
            // The value is already on top of the stack from compiling the initializer.
            // Its stack position = locals.Count - 1 (the slot we just added).
        }

        private int ResolveLocal(string name) {
            for (var i = locals.Count - 1; i >= 0; i--) {
                if (locals[i].name == name)
                    return i;
            }
            throw new Exception($"Undefined variable: '{name}'");
        }

        // ------------------------------------------------------------------
        //  Jump helpers
        // ------------------------------------------------------------------

        private int EmitJump(Op op) {
            chunk.Emit(op, 0xFFFF); // placeholder operand
            return chunk.code.Count - 1; // index of the operand to patch
        }

        private void PatchJump(int operandIndex) {
            chunk.code[operandIndex] = chunk.code.Count;
        }

        private void PatchJump(int operandIndex, int target) {
            chunk.code[operandIndex] = target;
        }

        // ------------------------------------------------------------------
        //  Loop context helpers
        // ------------------------------------------------------------------

        private LoopContext PushLoop(int continueTarget) {
            var ctx = new LoopContext {
                continueTarget = continueTarget,
                breakJumps = new List<int>()
            };
            loopStack.Push(ctx);
            return ctx;
        }

        private void PopLoop(LoopContext ctx) {
            // Patch all break jumps to point here (after the loop)
            foreach (var jumpIdx in ctx.breakJumps)
                PatchJump(jumpIdx);

            loopStack.Pop();
        }
    }
}