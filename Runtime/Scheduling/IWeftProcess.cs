using Weft.Language.Runtime;
using Weft.Unity.Engine;

namespace Weft.Runtime.Scheduling {
    public interface IWeftProcess {
        int Pid { get; }
        ScriptContext Context { get; }
        double ResumeAt { get; set; }
        bool Completed { get; }
        int WaitingForPid { get; set; }
        ExecutionResult Step();
    }
}