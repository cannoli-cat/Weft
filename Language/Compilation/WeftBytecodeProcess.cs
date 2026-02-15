using Weft.Language.Runtime;
using Weft.Runtime.Scheduling;
using Weft.Unity.Engine;

namespace Weft.Language.Compilation {
    public sealed class WeftBytecodeProcess : IWeftProcess {
        public int Pid { get; }
        public ScriptContext Context { get; }
        public double ResumeAt { get; set; }
        public bool Completed => vm.Completed;
        public int WaitingForPid { get; set; } = -1;

        private readonly WeftVM vm = new();
        private readonly int gasPerStep;

        private static int _nextPid = 1;

        public WeftBytecodeProcess(WeftChunk chunk, ScriptContext context, int gasPerStep = 2000) {
            Pid = _nextPid++;
            Context = context;
            this.gasPerStep = gasPerStep;
            vm.Load(chunk, context);
        }

        public ExecutionResult Step() {
            if (Completed)
                return ExecutionResult.SuccessResult();

            if (ResumeAt > 0) ResumeAt = 0;

            var res = vm.Step(gasPerStep);

            if (!res.success)
                return res;

            if (res.isYield) {
                ResumeAt = res.resumeAt;
                WaitingForPid = res.waitForPid;
            }

            return res;
        }
    }
}