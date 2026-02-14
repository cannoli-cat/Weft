using System.Collections.Generic;
using Weft.Language.AST;
using Weft.Runtime.Binding;
using Weft.Runtime.Scheduling;
using Weft.Runtime.Services;
using Weft.Unity.Engine;

namespace Weft.Language.Runtime {
    public class Interpreter {
        private readonly List<Dictionary<string, object>> scopes = new() { new Dictionary<string, object>() };
        private ScriptContext context;
        private int gas;

        public void SetContext(ScriptContext ctx) =>
            this.context = ctx;

        /// <summary>
        /// Called by WeftProcess each tick. Runs one top-level statement
        /// with a gas budget. No reflection needed.
        /// </summary>
        public ExecutionResult ExecuteOne(AstNode node, int gasBudget) {
            gas = gasBudget;
            return Execute(node);
        }

        private bool Burn(int cost = 1) {
            gas -= cost;
            return gas > 0;
        }

        private ExecutionResult Execute(AstNode node) {
            if (!Burn())
                return ExecutionResult.ErrorResult("Gas limit exceeded.");

            try {
                switch (node) {
                    case VarNode varNode: {
                        var val = Evaluate(varNode.Value);
                        if (!val.success) return val;
                        
                        if (!DeclareVar(varNode.Name, val.value))
                            return ExecutionResult.ErrorResult($"Variable '{varNode.Name}' is already defined.");
                        break;
                    }

                    case AssignmentNode assign: {
                        var val = Evaluate(assign.Value);
                        if (!val.success) return val;
                        
                        if (!SetVar(assign.Name, val.value))
                            return ExecutionResult.ErrorResult($"Variable '{assign.Name}' is not defined.");
                        break;
                    }

                    case FunctionCallNode funcNode:
                        return ExecuteFunction(funcNode);

                    case IfNode ifNode:
                        return ExecuteIf(ifNode);

                    case WhileNode whileNode:
                        return ExecuteWhile(whileNode);

                    case ForNode forNode:
                        return ExecuteFor(forNode);

                    case DoWhileNode doNode:
                        return ExecuteDoWhile(doNode);

                    case BreakNode:
                        return ExecutionResult.Break();

                    case ContinueNode:
                        return ExecutionResult.Continue();

                    case ExprStmtNode es: {
                        var r = Evaluate(es.Expr);
                        return r.success ? ExecutionResult.SuccessResult() : r;
                    }
                    
                    case IndexAssignNode idxAssign: {
                        var target = Evaluate(idxAssign.Target);
                        if (!target.success) return target;
                        
                        var index = Evaluate(idxAssign.Index);
                        if (!index.success) return index;
                        
                        var val = Evaluate(idxAssign.Value);
                        if (!val.success) return val;

                        if (target.value is List<object> list && index.value is double d) {
                            var i = (int)d;
                            if (i < 0 || i >= list.Count)
                                return ExecutionResult.ErrorResult($"Index {i} out of range (length {list.Count}).");
                            list[i] = val.value;
                            return ExecutionResult.SuccessResult();
                        }
                        
                        return ExecutionResult.ErrorResult("Index assignment requires an array and numeric index.");
                    }

                    default:
                        return ExecutionResult.ErrorResult($"Unknown AST node type: {node.GetType()}");
                }

                return ExecutionResult.SuccessResult();
            }
            catch (System.Exception ex) {
                return ExecutionResult.ErrorResult(ex.Message);
            }
        }

        private ExecutionResult Evaluate(AstNode node) {
            if (!Burn())
                return ExecutionResult.ErrorResult("Gas limit exceeded.");

            try {
                switch (node) {
                    case ArrayLiteralNode arr: {
                        var list = new List<object>(arr.Elements.Count);
                        foreach (var elem in arr.Elements) {
                            var val = Evaluate(elem);
                            
                            if (!val.success) return val;
                            
                            list.Add(val.value);
                        }
                        return ExecutionResult.SuccessResult(list);
                    }

                    case IndexAccessNode idx: {
                        var target = Evaluate(idx.Target);
                        if (!target.success) return target;
                        
                        var index = Evaluate(idx.Index);
                        if (!index.success) return index;

                        if (target.value is List<object> list && index.value is double d) {
                            var i = (int)d;
                            if (i < 0 || i >= list.Count)
                                return ExecutionResult.ErrorResult($"Index {i} out of range (length {list.Count}).");
                            return ExecutionResult.SuccessResult(list[i]);
                        }
                        return ExecutionResult.ErrorResult("Index access requires an array and numeric index.");
                    }

                    case IndexAssignNode idxAssign: {
                        var target = Evaluate(idxAssign.Target);
                        if (!target.success) return target;
                        
                        var index = Evaluate(idxAssign.Index);
                        if (!index.success) return index;
                        
                        var val = Evaluate(idxAssign.Value);
                        if (!val.success) return val;

                        if (target.value is List<object> list && index.value is double d) {
                            var i = (int)d;
                            if (i < 0 || i >= list.Count)
                                return ExecutionResult.ErrorResult($"Index {i} out of range (length {list.Count}).");
                            list[i] = val.value;
                            return ExecutionResult.SuccessResult();
                        }
                        return ExecutionResult.ErrorResult("Index assignment requires an array and numeric index.");
                    }

                    case MemberAccessNode mem: {
                        var target = Evaluate(mem.Target);
                        if (!target.success) return target;

                        if (target.value is List<object> list && mem.Member == "length")
                            return ExecutionResult.SuccessResult((double)list.Count);

                        return ExecutionResult.ErrorResult($"Unknown member '{mem.Member}'.");
                    }
                    
                    case NumberNode n:
                        return ExecutionResult.SuccessResult(n.Value);

                    case StringNode s:
                        return ExecutionResult.SuccessResult(s.Value);

                    case BoolNode b:
                        return ExecutionResult.SuccessResult(b.Value);

                    case IdentifierNode id:
                        return TryGetVar(id.Name, out var value)
                            ? ExecutionResult.SuccessResult(value)
                            : ExecutionResult.ErrorResult($"Undefined variable: {id.Name}");

                    case UnaryNode u: {
                        var val = Evaluate(u.Operand);
                        if (!val.success) return val;

                        return u.Operator switch {
                            "!" when val.value is bool bv => ExecutionResult.SuccessResult(!bv),
                            "!" => ExecutionResult.ErrorResult("Operand of '!' must be boolean."),
                            "-" when val.value is double dv => ExecutionResult.SuccessResult(-dv),
                            "-" => ExecutionResult.ErrorResult("Operand of unary '-' must be a number."),
                            "+" => ExecutionResult.SuccessResult(val.value),
                            _ => ExecutionResult.ErrorResult($"Unknown unary operator '{u.Operator}'.")
                        };
                    }

                    case IncDecNode inc: {
                        if (!TryGetVar(inc.Name, out var cur))
                            return ExecutionResult.ErrorResult($"Undefined variable: {inc.Name}");
                        if (cur is not double dv)
                            return ExecutionResult.ErrorResult(
                                $"Variable '{inc.Name}' must be a number for {(inc.IsIncrement ? "++" : "--")}.");

                        var after = inc.IsIncrement ? dv + 1 : dv - 1;
                        SetVar(inc.Name, after);
                        return ExecutionResult.SuccessResult(inc.IsPrefix ? after : dv);
                    }

                    case BinaryOperationNode binOp:
                        return EvaluateBinary(binOp);

                    case FunctionCallNode funcCall:
                        return ExecuteFunction(funcCall);

                    default:
                        return ExecutionResult.ErrorResult($"Cannot evaluate node type: {node.GetType()}");
                }
            }
            catch (System.Exception ex) {
                return ExecutionResult.ErrorResult(ex.Message);
            }
        }

        private ExecutionResult EvaluateBinary(BinaryOperationNode binOp) {
            // short-circuit logical operators
            if (binOp.Operator == "&&") {
                var lhs = Evaluate(binOp.Left);
                if (!lhs.success) return lhs;
                if (lhs.value is not bool lb)
                    return ExecutionResult.ErrorResult("Left operand of '&&' must be boolean.");
                if (!lb) return ExecutionResult.SuccessResult(false);

                var rhs = Evaluate(binOp.Right);
                if (!rhs.success) return rhs;
                if (rhs.value is bool rb) return ExecutionResult.SuccessResult(rb);
                return ExecutionResult.ErrorResult("Right operand of '&&' must be boolean.");
            }

            if (binOp.Operator == "||") {
                var lhs = Evaluate(binOp.Left);
                if (!lhs.success) return lhs;
                if (lhs.value is not bool lb)
                    return ExecutionResult.ErrorResult("Left operand of '||' must be boolean.");
                if (lb) return ExecutionResult.SuccessResult(true);

                var rhs = Evaluate(binOp.Right);
                if (!rhs.success) return rhs;
                if (rhs.value is bool rb) return ExecutionResult.SuccessResult(rb);
                return ExecutionResult.ErrorResult("Right operand of '||' must be boolean.");
            }

            var leftRes = Evaluate(binOp.Left);
            if (!leftRes.success) return leftRes;
            var rightRes = Evaluate(binOp.Right);
            if (!rightRes.success) return rightRes;

            var leftVal = leftRes.value;
            var rightVal = rightRes.value;

            if (leftVal is double l && rightVal is double r) {
                return binOp.Operator switch {
                    "+" => ExecutionResult.SuccessResult(l + r),
                    "-" => ExecutionResult.SuccessResult(l - r),
                    "*" => ExecutionResult.SuccessResult(l * r),
                    "/" => r == 0
                        ? ExecutionResult.ErrorResult("Division by zero.")
                        : ExecutionResult.SuccessResult(l / r),
                    "%" => ExecutionResult.SuccessResult(l % r),
                    ">" => ExecutionResult.SuccessResult(l > r),
                    "<" => ExecutionResult.SuccessResult(l < r),
                    ">=" => ExecutionResult.SuccessResult(l >= r),
                    "<=" => ExecutionResult.SuccessResult(l <= r),
                    "==" => ExecutionResult.SuccessResult(l == r),
                    "!=" => ExecutionResult.SuccessResult(l != r),
                    _ => ExecutionResult.ErrorResult($"Unsupported numeric operator '{binOp.Operator}'.")
                };
            }

            // string concat
            if (binOp.Operator == "+") {
                if (leftVal is string ls && rightVal is string rs)
                    return ExecutionResult.SuccessResult(ls + rs);
                if (leftVal is string ls2)
                    return ExecutionResult.SuccessResult(ls2 + (rightVal?.ToString() ?? "null"));
                if (rightVal is string rs2)
                    return ExecutionResult.SuccessResult((leftVal?.ToString() ?? "null") + rs2);
            }

            if (binOp.Operator == "==") return ExecutionResult.SuccessResult(Equals(leftVal, rightVal));
            if (binOp.Operator == "!=") return ExecutionResult.SuccessResult(!Equals(leftVal, rightVal));

            return ExecutionResult.ErrorResult(
                $"Unsupported operation '{binOp.Operator}' for operands {leftVal} and {rightVal}");
        }

        private ExecutionResult ExecuteWhile(WhileNode node) {
            while (true) {
                if (!Burn()) return ExecutionResult.ErrorResult("Gas limit exceeded.");

                var cond = Evaluate(node.Condition);
                if (!cond.success) return cond;
                if (cond.value is not bool b)
                    return ExecutionResult.ErrorResult("While condition must evaluate to boolean.");
                if (!b) break;

                var res = ExecuteBlock(node.Body);
                if (res.isBreak) break;
                if (res.isContinue) continue;
                if (!res.success || res.isYield) return res;
            }

            return ExecutionResult.SuccessResult();
        }

        private ExecutionResult ExecuteFor(ForNode node) {
            PushScope();
            try {
                var init = Execute(node.Init);
                if (!init.success) return init;

                while (true) {
                    if (!Burn()) return ExecutionResult.ErrorResult("Gas limit exceeded.");

                    var cond = Evaluate(node.Condition);
                    if (!cond.success) return cond;
                    if (cond.value is not bool b)
                        return ExecutionResult.ErrorResult("For condition must evaluate to boolean.");
                    if (!b) break;

                    var body = ExecuteBlock(node.Body);
                    if (body.isBreak) break;
                    if (body.isContinue) { /* fall through to step */ }
                    else if (!body.success || body.isYield) return body;

                    var step = Execute(node.Step);
                    if (!step.success) return step;
                }

                return ExecutionResult.SuccessResult();
            }
            finally {
                PopScope();
            }
        }

        private ExecutionResult ExecuteDoWhile(DoWhileNode node) {
            do {
                if (!Burn()) return ExecutionResult.ErrorResult("Gas limit exceeded.");

                var body = ExecuteBlock(node.Body);
                if (body.isBreak) break;
                if (body.isContinue) { /* fall through to condition */ }
                else if (!body.success || body.isYield) return body;

                var cond = Evaluate(node.Condition);
                if (!cond.success) return cond;
                if (cond.value is not bool b)
                    return ExecutionResult.ErrorResult("Do-while condition must evaluate to boolean.");
                if (!b) break;
            } while (true);

            return ExecutionResult.SuccessResult();
        }

        private ExecutionResult ExecuteBlock(BlockNode block) {
            PushScope();
            
            try {
                foreach (var stmt in block.Statements) {
                    var res = Execute(stmt);
                    if (!res.success || res.isBreak || res.isContinue || res.isYield)
                        return res;
                }
                return ExecutionResult.SuccessResult();
            }
            finally {
                PopScope();
            }
        }
        
        private ExecutionResult ExecuteIf(IfNode ifNode) {
            var condResult = Evaluate(ifNode.Condition);
            if (!condResult.success) return condResult;

            if (condResult.value is not bool cond)
                return ExecutionResult.ErrorResult("If condition must evaluate to boolean.");

            if (cond) {
                if (ifNode.TrueBranch is BlockNode tb)
                    return ExecuteBlock(tb);
            }
            else if (ifNode.FalseBranch != null) {
                if (ifNode.FalseBranch is BlockNode fb)
                    return ExecuteBlock(fb);
                return Execute(ifNode.FalseBranch);
            }

            return ExecutionResult.SuccessResult();
        }

        private ExecutionResult ExecuteFunction(FunctionCallNode call) {
            var argVals = new object[call.Arguments.Count];
            for (var i = 0; i < call.Arguments.Count; i++) {
                var v = Evaluate(call.Arguments[i]);
                if (!v.success) return v;
                argVals[i] = v.value;
            }

            if (!WeftRegistry.TryGet(call.FunctionName, out var host))
                return ExecutionResult.ErrorResult($"Unknown function: {call.FunctionName}");

            try {
                var ret = host(context, argVals);

                if (ret is IYieldRequest y) {
                    switch (y) {
                        case YieldForSeconds ys:
                            var time = context.Resolve<ITimeService>();
                            return ExecutionResult.YieldUntil(time.Now + ys.Seconds);
                        case YieldForProcess yp:
                            return ExecutionResult.YieldUntilPid(yp.TargetPid);
                    }
                }

                return ExecutionResult.SuccessResult(ret);
            }
            catch (System.Exception ex) {
                return ExecutionResult.ErrorResult($"Function '{call.FunctionName}' failed: {ex.Message}");
            }
        }
        
        private void PushScope() => scopes.Add(new Dictionary<string, object>());

        private void PopScope() {
            if (scopes.Count > 1) scopes.RemoveAt(scopes.Count - 1);
        }

        private bool TryGetVar(string name, out object value) {
            // walk scopes from innermost to outermost
            for (var i = scopes.Count - 1; i >= 0; i--) {
                if (scopes[i].TryGetValue(name, out value)) return true;
            }
            value = null;
            return false;
        }

        private bool SetVar(string name, object value) {
            // find the scope that owns this variable and update it
            for (var i = scopes.Count - 1; i >= 0; i--) {
                if (scopes[i].ContainsKey(name)) {
                    scopes[i][name] = value;
                    return true;
                }
            }
            return false; // not defined
        }

        private bool DeclareVar(string name, object value) {
            var current = scopes[^1];
            if (!current.TryAdd(name, value)) return false; // already defined in THIS scope
            return true;
        }
    }
}
