using System.Collections.Generic;
using Weft.Language.AST;
using Weft.Language.Runtime;
using Weft.Unity.Engine;

namespace Weft.Runtime.Scheduling {
    public sealed class WeftProcess {
        public int Pid { get; }
        public ScriptContext Context { get; }
        private Interpreter Vm { get; }
        private List<AstNode> Program { get; }
        public int PC { get; private set; }
        public double ResumeAt { get; set; }
        public bool Completed { get; private set; }
        public int WaitingForPid { get; set; } = -1;

        private readonly int gasPerStep;

        private static int _nextPid = 1;

        public WeftProcess(List<AstNode> program, ScriptContext context, int gasPerStep = 2000) {
            Pid = _nextPid++;
            Program = program;
            Context = context;
            Vm = new Interpreter();
            Vm.SetContext(context);
            this.gasPerStep = gasPerStep;
        }

        public ExecutionResult Step() {
            if (Completed) return ExecutionResult.SuccessResult();

            if (ResumeAt > 0) ResumeAt = 0;

            while (PC < Program.Count) {
                // direct call, no reflection
                var res = Vm.ExecuteOne(Program[PC], gasPerStep);

                if (!res.success) {
                    Completed = true;
                    return res;
                }

                if (res.isYield) {
                    PC++;
                    ResumeAt = res.resumeAt;
                    WaitingForPid = res.waitForPid;
                    return ExecutionResult.SuccessResult();
                }

                PC++;
            }

            if (PC >= Program.Count) Completed = true;
            return ExecutionResult.SuccessResult();
        }
    }
}