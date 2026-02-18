using System.Collections.Generic;
using System.Globalization;
using Weft.Language.AST;
using Weft.Language.Lexing;
using Weft.Runtime;

namespace Weft.Language.Parsing {
    public class Parser {
        private List<Token> tokens;
        private int position;
        private readonly LanguageFeatures features;

        public Parser(LanguageFeatures features = LanguageFeatures.Conditionals | LanguageFeatures.Loops |
                                                  LanguageFeatures.Functions | LanguageFeatures.Collections |
                                                  LanguageFeatures.AugAssign) {
            this.features = features;
        }

        private void Require(LanguageFeatures f, string msg, ParseResult res) {
            if ((features & f) == 0)
                SetError(res, msg);
        }

        public ParseResult Parse(List<Token> tokens) {
            this.tokens = tokens.FindAll(t => t.Type != TokenType.Comment);
            position = 0;

            var result = new ParseResult { Nodes = new List<AstNode>() };

            while (!IsAtEnd()) {
                var stmt = ParseStatement(result);
                if (stmt == null || result.HasError) break;
                result.Nodes.Add(stmt);
            }

            return result;
        }

        private AstNode ParseStatement(ParseResult result) {
            var token = Peek();
            if (IsAtEnd() || token == null) {
                SetError(result, "Unexpected end of input");
                return null;
            }

            // prefix ++ / --
            if (Match(TokenType.Operator, "++") || Match(TokenType.Operator, "--")) {
                var op = Previous().Value;

                var idTok = Consume(TokenType.Identifier, result, $"Expected identifier after '{op}'");
                if (result.HasError) return null;

                Consume(TokenType.Symbol, ";", result, "Expected ';' after increment/decrement");
                return new ExprStmtNode(new IncDecNode(idTok.Value, isIncrement: op == "++", isPrefix: true)
                    { Line = token.Line });
            }

            switch (token.Type) {
                case TokenType.Keyword when token.Value == "func":
                    Require(LanguageFeatures.Functions, "Functions not enabled", result);
                    return result.HasError ? null : ParseFuncDecl(result);

                case TokenType.Keyword when token.Value == "return":
                    Advance();
                    AstNode retVal = null;

                    if (!Check(TokenType.Symbol) || Peek().Value != ";")
                        retVal = ParseExpression(result);

                    if (result.HasError) return null;
                    Consume(TokenType.Symbol, ";", result, "Expected ';' after return value");

                    return result.HasError ? null : new ReturnNode(retVal) { Line = token.Line };

                case TokenType.Keyword when token.Value == "var":
                    return ParseVarDeclaration(result);

                case TokenType.Keyword when token.Value == "if":
                    Require(LanguageFeatures.Conditionals, "Conditionals not enabled", result);
                    return result.HasError ? null : ParseIfStatement(result);

                case TokenType.Keyword when token.Value == "while":
                    Require(LanguageFeatures.Loops, "Loops not enabled", result);
                    return result.HasError ? null : ParseWhile(result);

                case TokenType.Keyword when token.Value == "for":
                    Require(LanguageFeatures.Loops, "Loops not enabled", result);
                    return result.HasError ? null : ParseFor(result);

                case TokenType.Keyword when token.Value == "do":
                    Require(LanguageFeatures.Loops, "Loops not enabled", result);
                    return result.HasError ? null : ParseDoWhile(result);

                case TokenType.Keyword when token.Value == "break":
                    Require(LanguageFeatures.Loops, "Loops not enabled", result);
                    if (result.HasError) return null;

                    Advance();
                    Consume(TokenType.Symbol, ";", result, "Expected ';' after 'break'");

                    return result.HasError ? null : new BreakNode() { Line = token.Line };

                case TokenType.Keyword when token.Value == "continue":
                    Require(LanguageFeatures.Loops, "Loops not enabled", result);
                    if (result.HasError) return null;

                    Advance();
                    Consume(TokenType.Symbol, ";", result, "Expected ';' after 'continue'");

                    return result.HasError ? null : new ContinueNode() { Line = token.Line };

                case TokenType.Identifier:
                    return ParseIdentifierStatement(result);

                default:
                    SetError(result, $"Unexpected token: {token.Value}");
                    return null;
            }
        }
        
        private AstNode ParseIdentifierStatement(ParseResult result) {
            Advance();
            var name = Previous().Value;

            if (Match(TokenType.Symbol, "[")) {
                Require(LanguageFeatures.Collections, "Collections not enabled", result);
                if (result.HasError) return null;

                var index = ParseExpression(result);
                if (result.HasError) return null;

                Consume(TokenType.Symbol, "]", result, "Expected ']'");
                if (result.HasError) return null;

                Consume(TokenType.Operator, "=", result, "Expected '=' after index");
                if (result.HasError) return null;

                var value = ParseExpression(result);
                if (result.HasError) return null;

                Consume(TokenType.Symbol, ";", result, "Expected ';'");
                return new IndexAssignNode(new IdentifierNode(name), index, value) { Line = Previous().Line };
            }

            if (Match(TokenType.Symbol, ".")) {
                Require(LanguageFeatures.Collections, "Collections not enabled", result);
                if (result.HasError) return null;

                var member = Consume(TokenType.Identifier, result, "Expected member name after '.'");
                if (result.HasError) return null;

                if (Match(TokenType.Symbol, "(")) {
                    var args = new List<AstNode> { new IdentifierNode(name) };

                    if (!Match(TokenType.Symbol, ")")) {
                        do {
                            args.Add(ParseExpression(result));
                        } while (Match(TokenType.Symbol, ","));

                        Consume(TokenType.Symbol, ")", result, "Expected ')'");
                    }

                    if (result.HasError) return null;
                    Consume(TokenType.Symbol, ";", result, "Expected ';' after method call");

                    return new FunctionCallNode($"__{member.Value}", args) { Line = Previous().Line };
                }

                Consume(TokenType.Symbol, ";", result, "Expected ';'");
                return new ExprStmtNode(new MemberAccessNode(new IdentifierNode(name), member.Value))
                    { Line = Previous().Line };
            }

            // postfix ++ / --
            if (Match(TokenType.Operator, "++") || Match(TokenType.Operator, "--")) {
                var isInc = Previous().Value == "++";
                Consume(TokenType.Symbol, ";", result, "Expected ';' after increment/decrement");
                return new ExprStmtNode(new IncDecNode(name, isIncrement: isInc, isPrefix: false))
                    { Line = Previous().Line };
            }

            // augmented assignment
            if (Match(TokenType.Operator, "+=") || Match(TokenType.Operator, "-=") ||
                Match(TokenType.Operator, "*=") || Match(TokenType.Operator, "/=") ||
                Match(TokenType.Operator, "%=")) {
                Require(LanguageFeatures.AugAssign, "Augmented assignment disabled", result);
                if (result.HasError) return null;

                var op = Previous().Value[0].ToString();
                var rhs = ParseExpression(result);
                if (result.HasError) return null;
                Consume(TokenType.Symbol, ";", result, "Expected ';' after assignment");
                return new AssignmentNode(name, new BinaryOperationNode(new IdentifierNode(name), op, rhs))
                    { Line = Previous().Line };
            }

            // simple assignment
            if (Match(TokenType.Operator, "=")) {
                var valueExpr = ParseExpression(result);
                if (result.HasError) return null;
                Consume(TokenType.Symbol, ";", result, "Expected ';' after assignment");
                return new AssignmentNode(name, valueExpr) { Line = Previous().Line };
            }

            // function call
            if (Match(TokenType.Symbol, "(")) {
                var funcCall = ParseFunctionCall(name, result);
                Consume(TokenType.Symbol, ";", result, "Expected ';' after function call");
                return funcCall;
            }

            SetError(result, $"Unexpected identifier: {name}");
            return null;
        }

        private AstNode ParseFuncDecl(ParseResult result) {
            Advance(); // skip 'func'
            var nameToken = Consume(TokenType.Identifier, result, "Expected function name");
            if (result.HasError) return null;

            Consume(TokenType.Symbol, "(", result, "Expected '(' after function name");
            if (result.HasError) return null;

            var parameters = new List<string>();
            if (!Match(TokenType.Symbol, ")")) {
                do {
                    var paramTok = Consume(TokenType.Identifier, result, "Expected parameter name");
                    if (result.HasError) return null;
                    parameters.Add(paramTok.Value);
                } while (Match(TokenType.Symbol, ","));

                Consume(TokenType.Symbol, ")", result, "Expected ')' after parameters");
                if (result.HasError) return null;
            }

            var body = ParseBlockOrStatement(result);
            return result.HasError
                ? null
                : new FuncDeclNode(nameToken.Value, parameters, body) { Line = nameToken.Line };
        }

        private AstNode ParseVarDeclaration(ParseResult result) {
            Advance(); // skip 'var'
            var nameToken = Consume(TokenType.Identifier, result, "Expected variable name");
            if (result.HasError) return null;

            Consume(TokenType.Operator, "=", result, "Expected '=' after variable name");
            if (result.HasError) return null;

            var expr = ParseExpression(result);
            if (result.HasError) return null;

            Consume(TokenType.Symbol, ";", result, "Expected ';' after variable declaration");
            return result.HasError ? null : new VarNode(nameToken.Value, expr) { Line = nameToken.Line };
        }

        private AstNode ParseIfStatement(ParseResult res, bool alreadyConsumedIf = false) {
            var line = Peek().Line;
            if (!alreadyConsumedIf) Advance(); // 'if'

            Consume(TokenType.Symbol, "(", res, "Expected '(' after 'if'");
            if (res.HasError) return null;

            var condition = ParseExpression(res);
            if (res.HasError || condition == null) return null;

            Consume(TokenType.Symbol, ")", res, "Expected ')' after condition");
            if (res.HasError) return null;

            var trueBlock = ParseBlockOrStatement(res);
            if (res.HasError || trueBlock == null) return null;

            AstNode falseBlock = null;
            if (Match(TokenType.Keyword, "else")) {
                falseBlock = ParseBlockOrStatement(res);
                if (res.HasError || falseBlock == null) return null;
            }

            return new IfNode(condition, trueBlock, falseBlock) { Line = line };
        }

        private AstNode ParseWhile(ParseResult res) {
            var line = Peek().Line;
            Advance(); // 'while'

            Consume(TokenType.Symbol, "(", res, "Expected '(' after 'while'");
            if (res.HasError) return null;

            var cond = ParseExpression(res);
            if (res.HasError) return null;

            Consume(TokenType.Symbol, ")", res, "Expected ')' after while condition");
            if (res.HasError) return null;

            var body = ParseBlockOrStatement(res);
            return res.HasError ? null : new WhileNode(cond, body) { Line = line };
        }

        private AstNode ParseDoWhile(ParseResult res) {
            var line = Peek().Line;
            Advance(); // 'do'

            var body = ParseBlockOrStatement(res);
            if (res.HasError) return null;

            Consume(TokenType.Keyword, "while", res, "Expected 'while' after do-block");
            if (res.HasError) return null;

            Consume(TokenType.Symbol, "(", res, "Expected '(' after 'while'");
            if (res.HasError) return null;

            var cond = ParseExpression(res);
            if (res.HasError) return null;

            Consume(TokenType.Symbol, ")", res, "Expected ')' after do-while condition");
            if (res.HasError) return null;

            Consume(TokenType.Symbol, ";", res, "Expected ';' after do-while");
            return res.HasError ? null : new DoWhileNode(body, cond) { Line = line };
        }

        private AstNode ParseFor(ParseResult res) {
            var line = Peek().Line;
            Advance(); // 'for'

            Consume(TokenType.Symbol, "(", res, "Expected '(' after 'for'");
            if (res.HasError) return null;

            // init, ParseStatement handles var decl and assignments (each consumes its own ';')
            var init = ParseStatement(res);
            if (res.HasError) return null;

            // condition
            var cond = ParseExpression(res);
            if (res.HasError) return null;

            Consume(TokenType.Symbol, ";", res, "Expected ';' after for-condition");
            if (res.HasError) return null;

            // step, no trailing ';', so we use a special helper
            var step = ParseForStep(res);
            if (res.HasError) return null;

            Consume(TokenType.Symbol, ")", res, "Expected ')' after for-clauses");
            if (res.HasError) return null;

            var body = ParseBlockOrStatement(res);
            return res.HasError ? null : new ForNode(init, cond, step, body) { Line = line };
        }
        
        private AstNode ParseForStep(ParseResult res) {
            // prefix ++ / --
            if (Match(TokenType.Operator, "++") || Match(TokenType.Operator, "--")) {
                var op = Previous().Value;
                var idTok = Consume(TokenType.Identifier, res, $"Expected identifier after '{op}'");
                return res.HasError
                    ? null
                    : new ExprStmtNode(new IncDecNode(idTok.Value, isIncrement: op == "++", isPrefix: true))
                        { Line = idTok.Line };
            }

            var tok = Consume(TokenType.Identifier, res, "Expected identifier in for-step");
            if (res.HasError) return null;
            var name = tok.Value;

            // postfix ++ / --
            if (Match(TokenType.Operator, "++"))
                return new ExprStmtNode(new IncDecNode(name, true, false)) { Line = tok.Line };
            if (Match(TokenType.Operator, "--"))
                return new ExprStmtNode(new IncDecNode(name, false, false)) { Line = tok.Line };

            // augmented assignment
            if (Match(TokenType.Operator, "+=") || Match(TokenType.Operator, "-=") ||
                Match(TokenType.Operator, "*=") || Match(TokenType.Operator, "/=") ||
                Match(TokenType.Operator, "%=")) {
                var op = Previous().Value[0].ToString();
                var rhs = ParseExpression(res);
                if (res.HasError) return null;
                return new AssignmentNode(name, new BinaryOperationNode(new IdentifierNode(name), op, rhs))
                    { Line = tok.Line };
            }

            // simple assignment
            if (Match(TokenType.Operator, "=")) {
                var rhs = ParseExpression(res);
                if (res.HasError) return null;
                return new AssignmentNode(name, rhs) { Line = tok.Line };
            }

            SetError(res, $"Unexpected for-step token after '{name}'");
            return null;
        }

        private BlockNode ParseBlockOrStatement(ParseResult res) {
            if (Match(TokenType.Symbol, "{")) {
                var stmts = ParseBlock(res);
                if (res.HasError || stmts == null) return null;
                return new BlockNode(stmts) { Line = Peek()?.Line ?? Previous().Line };
            }

            var stmt = ParseStatement(res);
            if (res.HasError || stmt == null) return null;

            return new BlockNode(new List<AstNode> { stmt }) { Line = stmt.Line };
        }

        private List<AstNode> ParseBlock(ParseResult result) {
            var nodes = new List<AstNode>();
            while (true) {
                if (IsAtEnd()) {
                    SetError(result, "Unterminated block (expected '}')");
                    return null;
                }

                if (Match(TokenType.Symbol, "}")) break;

                var stmt = ParseStatement(result);
                if (stmt == null || result.HasError) return null;
                nodes.Add(stmt);
            }

            return nodes;
        }

        private AstNode ParseExpression(ParseResult result) => ParseLogicalOr(result);

        private FunctionCallNode ParseFunctionCall(string functionName, ParseResult result) {
            var arguments = new List<AstNode>();
            if (!Match(TokenType.Symbol, ")")) {
                do {
                    arguments.Add(ParseExpression(result));
                } while (Match(TokenType.Symbol, ","));

                Consume(TokenType.Symbol, ")", result, "Expected ')' after arguments");
            }

            return new FunctionCallNode(functionName, arguments) { Line = Previous().Line };
        }

        private AstNode ParseLogicalOr(ParseResult result) {
            var node = ParseLogicalAnd(result);
            while (!IsAtEnd() && Match(TokenType.Operator, "||")) {
                var right = ParseLogicalAnd(result);
                node = new BinaryOperationNode(node, "||", right) { Line = node.Line };
            }

            return node;
        }

        private AstNode ParseLogicalAnd(ParseResult result) {
            var node = ParseEquality(result);
            while (!IsAtEnd() && Match(TokenType.Operator, "&&")) {
                var right = ParseEquality(result);
                node = new BinaryOperationNode(node, "&&", right) { Line = node.Line };
            }

            return node;
        }

        private AstNode ParseEquality(ParseResult result) {
            var node = ParseComparison(result);
            while (!IsAtEnd() && (Match(TokenType.Operator, "==") || Match(TokenType.Operator, "!="))) {
                var op = Previous().Value;
                var right = ParseComparison(result);
                if (result.HasError || right == null) return null;
                node = new BinaryOperationNode(node, op, right) { Line = node.Line };
            }

            return node;
        }

        private AstNode ParseComparison(ParseResult result) {
            var node = ParseTerm(result);
            while (!IsAtEnd() && (Match(TokenType.Operator, "<") || Match(TokenType.Operator, ">") ||
                                  Match(TokenType.Operator, "<=") || Match(TokenType.Operator, ">="))) {
                var op = Previous().Value;
                var right = ParseTerm(result);
                if (result.HasError || right == null) return null;
                node = new BinaryOperationNode(node, op, right) { Line = node.Line };
            }

            return node;
        }

        private AstNode ParseTerm(ParseResult result) {
            var node = ParseFactor(result);
            while (!IsAtEnd() && (Match(TokenType.Operator, "+") || Match(TokenType.Operator, "-"))) {
                var op = Previous().Value;
                var right = ParseFactor(result);
                if (result.HasError || right == null) return null;
                node = new BinaryOperationNode(node, op, right) { Line = node.Line };
            }

            return node;
        }

        private AstNode ParseFactor(ParseResult result) {
            var node = ParseUnary(result);
            while (!IsAtEnd() && (Match(TokenType.Operator, "*") || Match(TokenType.Operator, "/") ||
                                  Match(TokenType.Operator, "%"))) {
                var op = Previous().Value;
                var right = ParseUnary(result);
                if (result.HasError || right == null) return null;
                node = new BinaryOperationNode(node, op, right) { Line = node.Line };
            }

            return node;
        }

        private AstNode ParseUnary(ParseResult res) {
            var line = Peek()?.Line ?? 0;

            if (Match(TokenType.Operator, "++") || Match(TokenType.Operator, "--")) {
                var op = Previous().Value;
                var idTok = Consume(TokenType.Identifier, res, $"Expected identifier after '{op}'");
                return res.HasError
                    ? null
                    : new IncDecNode(idTok.Value, isIncrement: op == "++", isPrefix: true) { Line = idTok.Line };
            }

            if (Match(TokenType.Operator, "!")) {
                var right = ParseUnary(res);
                if (res.HasError || right == null) return null;
                return new UnaryNode("!", right) { Line = line };
            }

            if (Match(TokenType.Operator, "-")) {
                var right = ParseUnary(res);
                if (res.HasError || right == null) return null;
                return new UnaryNode("-", right) { Line = line };
            }

            if (Match(TokenType.Operator, "+"))
                return ParseUnary(res);

            return ParsePostfix(res);
        }

        private AstNode ParsePostfix(ParseResult res) {
            var node = ParsePrimary(res);
            if (res.HasError || node == null) return null;

            while (true) {
                if (Match(TokenType.Symbol, "[")) {
                    Require(LanguageFeatures.Collections, "Collections not enabled", res);
                    if (res.HasError) return null;
                    var index = ParseExpression(res);
                    if (res.HasError) return null;
                    Consume(TokenType.Symbol, "]", res, "Expected ']'");
                    if (res.HasError) return null;
                    node = new IndexAccessNode(node, index) { Line = node.Line };
                }
                else if (Match(TokenType.Symbol, ".")) {
                    var member = Consume(TokenType.Identifier, res, "Expected member name after '.'");
                    if (res.HasError) return null;

                    if (Match(TokenType.Symbol, "(")) {
                        var args = new List<AstNode>();
                        if (!Match(TokenType.Symbol, ")")) {
                            do {
                                args.Add(ParseExpression(res));
                            } while (Match(TokenType.Symbol, ","));

                            Consume(TokenType.Symbol, ")", res, "Expected ')'");
                        }

                        args.Insert(0, node);
                        node = new FunctionCallNode($"__{member.Value}", args) { Line = node.Line };
                    }
                    else {
                        node = new MemberAccessNode(node, member.Value) { Line = node.Line };
                    }
                }
                else if (node is IdentifierNode id) {
                    if (Match(TokenType.Operator, "++"))
                        return new IncDecNode(id.Name, true, false) { Line = node.Line };
                    if (Match(TokenType.Operator, "--"))
                        return new IncDecNode(id.Name, false, false) { Line = node.Line };
                    break;
                }
                else break;
            }

            return node;
        }

        private AstNode ParsePrimary(ParseResult result) {
            if (Match(TokenType.Number))
                return new NumberNode(double.Parse(Previous().Value, CultureInfo.InvariantCulture))
                    { Line = Previous().Line };

            if (Match(TokenType.String))
                return new StringNode(Previous().Value) { Line = Previous().Line };

            if (Match(TokenType.Identifier)) {
                var name = Previous().Value;
                if (Match(TokenType.Symbol, "(")) {
                    var func = ParseFunctionCall(name, result);
                    return result.HasError ? null : func;
                }

                return new IdentifierNode(name) { Line = Previous().Line };
            }

            if (Match(TokenType.Keyword, "true")) return new BoolNode(true) { Line = Previous().Line };
            if (Match(TokenType.Keyword, "false")) return new BoolNode(false) { Line = Previous().Line };
            if (Match(TokenType.Keyword, "null")) return new NullNode { Line = Previous().Line };

            if (Match(TokenType.Symbol, "(")) {
                var expr = ParseExpression(result);
                if (result.HasError) return null;
                Consume(TokenType.Symbol, ")", result, "Expected ')' after expression");
                return result.HasError ? null : expr;
            }

            if (Match(TokenType.Symbol, "[")) {
                Require(LanguageFeatures.Collections, "Collections not enabled", result);
                if (result.HasError) return null;
                var elements = new List<AstNode>();

                if (!Match(TokenType.Symbol, "]")) {
                    do {
                        elements.Add(ParseExpression(result));
                        if (result.HasError) return null;
                    } while (Match(TokenType.Symbol, ","));

                    Consume(TokenType.Symbol, "]", result, "Expected ']'");
                }

                return result.HasError ? null : new ArrayLiteralNode(elements) { Line = Previous().Line };
            }

            if (Match(TokenType.Symbol, "{")) {
                Require(LanguageFeatures.Collections, "Collections not enabled", result);
                if (result.HasError) return null;

                var entries = new List<(AstNode Key, AstNode Value)>();

                if (!Match(TokenType.Symbol, "}")) {
                    do {
                        AstNode key;
                        if (Match(TokenType.Identifier))
                            key = new StringNode(Previous().Value) { Line = Previous().Line };
                        else if (Match(TokenType.String))
                            key = new StringNode(Previous().Value) { Line = Previous().Line };
                        else {
                            SetError(result, "Expected property name");
                            return null;
                        }

                        Consume(TokenType.Symbol, ":", result, "Expected ':' after property name");
                        if (result.HasError) return null;

                        var value = ParseExpression(result);
                        if (result.HasError) return null;

                        entries.Add((key, value));
                    } while (Match(TokenType.Symbol, ","));

                    Consume(TokenType.Symbol, "}", result, "Expected '}'");
                }

                return result.HasError ? null : new ObjectLiteralNode(entries) { Line = Previous().Line };
            }

            var t = Peek();
            SetError(result, $"Unexpected token in expression: {t?.Value ?? "end of input"}");

            return null;
        }

        private bool Match(TokenType type, string value) {
            if (!Check(type) || Peek().Value != value) return false;
            Advance();
            return true;
        }

        private bool Match(TokenType type) {
            if (!Check(type)) return false;
            Advance();
            return true;
        }

        private Token Peek() => IsAtEnd() ? null : tokens[position];

        private Token Advance() {
            if (!IsAtEnd()) position++;
            return Previous();
        }

        private Token Previous() => tokens[position - 1];
        private bool IsAtEnd() => position >= tokens.Count;

        private Token Consume(TokenType type, string value, ParseResult res, string errorMessage) {
            if (Check(type) && Peek().Value == value) return Advance();
            SetError(res, errorMessage);
            return null;
        }

        private Token Consume(TokenType type, ParseResult res, string errorMessage) {
            if (Check(type)) return Advance();
            SetError(res, errorMessage);
            return null;
        }

        private bool Check(TokenType type) => !IsAtEnd() && Peek().Type == type;

        private void SetError(ParseResult res, string msg) {
            var line = Peek()?.Line ?? Previous().Line;
            res.Error = new WeftError(ErrorPhase.Parse, msg, line);
        }
    }
}