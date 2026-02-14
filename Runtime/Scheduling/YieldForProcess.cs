namespace Weft.Runtime.Scheduling {
    public class YieldForProcess : IYieldRequest {
        public int TargetPid { get; }
        public YieldForProcess(int pid) => TargetPid = pid;
    }
}